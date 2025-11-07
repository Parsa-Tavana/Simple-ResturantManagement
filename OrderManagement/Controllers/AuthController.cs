using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OrderManagement.DTOs;
using OrderManagement.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer; // <-- for scheme name
using OrderManagement.Configuration;
using OrderManagement.Services;

namespace OrderManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly IJwtTokenService _tokenService;
        private readonly IOptionsSnapshot<JwtOptions> _jwtOptions;

        public AuthController(
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn,
            IJwtTokenService tokenService,
            IOptionsSnapshot<JwtOptions> jwtOptions)
        {
            _users = users;
            _signIn = signIn;
            _tokenService = tokenService;
            _jwtOptions = jwtOptions;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray()
                });

            if (string.Equals(dto.Role, "Customer", StringComparison.OrdinalIgnoreCase) && dto.CustomerId is null)
                return BadRequest(new { message = "Registration failed", errors = new[] { "CustomerId is required for role Customer." } });

            if (string.Equals(dto.Role, "RestaurantAdmin", StringComparison.OrdinalIgnoreCase) && dto.RestaurantId is null)
                return BadRequest(new { message = "Registration failed", errors = new[] { "RestaurantId is required for role RestaurantAdmin." } });

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true,
                CustomerId = dto.CustomerId,
                RestaurantId = dto.RestaurantId
            };

            var result = await _users.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var reasons = result.Errors?.Select(e => e.Description).ToArray() ?? Array.Empty<string>();
                return BadRequest(new { message = "Registration failed", errors = reasons });
            }

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var addRole = await _users.AddToRoleAsync(user, dto.Role);
                if (!addRole.Succeeded)
                {
                    var reasons = addRole.Errors?.Select(e => e.Description).ToArray() ?? Array.Empty<string>();
                    return BadRequest(new { message = "Could not add role", errors = reasons });
                }
            }

            return Ok(new { message = "Registered successfully" });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var input = (dto.Email ?? "").Trim();
            var user = await _users.FindByEmailAsync(input) ?? await _users.FindByNameAsync(input);
            if (user == null) return Unauthorized("Invalid credentials");

            var ok = await _signIn.CheckPasswordSignInAsync(user, dto.Password ?? "", false);
            if (!ok.Succeeded) return Unauthorized("Invalid credentials");

            var roles = await _users.GetRolesAsync(user);
            var tokenResult = _tokenService.CreateToken(user, roles);

            return Ok(new
            {
                token = tokenResult.Token,
                expiresAt = tokenResult.ExpiresAtUtc,
                roles,
                email = user.Email,
                customerId = user.CustomerId,
                restaurantId = user.RestaurantId
            });
        }

        // Force JWT bearer scheme explicitly to avoid any cookie scheme interference
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("me")]
        public async Task<IActionResult> Me([FromServices] UserManager<ApplicationUser> users)
        {
            var user = await users.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var roles = await users.GetRolesAsync(user);
            return Ok(new
            {
                email = user.Email,
                roles,
                customerId = user.CustomerId,
                restaurantId = user.RestaurantId
            });
        }

        // Quick probe: what does the pipeline think your identity is?
        [Authorize]
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            var claims = User?.Claims?.Select(c => new { c.Type, c.Value }).ToArray() ?? [];
            var sub = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(new
            {
                ok = true,
                sub,
                email = User?.FindFirstValue(JwtRegisteredClaimNames.Email)
                     ?? User?.FindFirstValue(ClaimTypes.Email),
                roles = User?.Claims?.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [],
                claimCount = claims.Length
            });
        }


        // ===== Debug (no validation) =====
        [AllowAnonymous]
        [HttpGet("me-debug")]
        public IActionResult MeDebug()
        {
            var tokenStr = ReadBearerOrBadRequest();
            if (tokenStr is null) return BadRequest(new { message = "Missing or malformed Authorization header. Expected: Bearer <token>" });

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(tokenStr);
                var exp = jwt.Payload.Exp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(jwt.Payload.Exp.Value) : (DateTimeOffset?)null;
                var options = _jwtOptions.Value;
                var expectedIssuer = options.Issuer ?? "(null)";
                var expectedAudience = string.IsNullOrWhiteSpace(options.Audience) ? "(not configured)" : options.Audience;

                return Ok(new
                {
                    header = jwt.Header,
                    payload = jwt.Payload,
                    claims = jwt.Claims.Select(c => new { c.Type, c.Value }),
                    summary = new
                    {
                        tokenIssuer = jwt.Issuer,
                        expectedIssuer,
                        expectedAudience,
                        subject = jwt.Subject,
                        nameId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                        email = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value
                                ?? jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                        roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray(),
                        expUtc = exp?.UtcDateTime.ToString("O"),
                        nowUtc = DateTimeOffset.UtcNow.UtcDateTime.ToString("O"),
                        isExpired = exp.HasValue && DateTimeOffset.UtcNow > exp.Value,
                        configuredLifetimeMinutes = options.AccessTokenMinutes
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Token could not be parsed", error = ex.Message });
            }
        }

        // ===== Validate with the same rules as middleware =====
        [AllowAnonymous]
        [HttpGet("me-validate")]
        public IActionResult MeValidate()
        {
            var tokenStr = ReadBearerOrBadRequest();
            if (tokenStr is null)
                return BadRequest(new { message = "Missing or malformed Authorization header. Expected: Bearer <token>" });

            var options = _jwtOptions.Value;
            var issuer = (options.Issuer ?? "").Trim();
            var keyStr = (options.Key ?? "").Trim();
            var hasAudience = !string.IsNullOrWhiteSpace(options.Audience);

            var parms = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = hasAudience,
                ValidAudience = hasAudience ? options.Audience : null,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(Math.Max(0, options.ClockSkewMinutes))
            };

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(tokenStr, parms, out var validatedToken);

                var sub = principal.FindFirst("sub")?.Value;
                var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                         ?? principal.FindFirst(ClaimTypes.Email)?.Value;
                var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

                return Ok(new
                {
                    ok = true,
                    subject = sub,
                    email,
                    roles,
                    tokenType = validatedToken?.GetType().FullName
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new
                {
                    ok = false,
                    message = ex.Message,
                    exception = ex.GetType().FullName
                });
            }
        }

        [AllowAnonymous]
        [HttpGet("config-snapshot")]
        public IActionResult ConfigSnapshot()
        {
            var options = _jwtOptions.Value;
            var issuer = (options.Issuer ?? "(null)").Trim();
            var audience = string.IsNullOrWhiteSpace(options.Audience) ? "(not configured)" : options.Audience.Trim();
            var key = (options.Key ?? "(null)").Trim();
            var key6 = key == "(null)" ? "(null)" : (key.Length >= 6 ? key[..6] : "(short)");
            return Ok(new
            {
                issuer,
                audience,
                key6,
                accessTokenMinutes = options.AccessTokenMinutes,
                clockSkewMinutes = options.ClockSkewMinutes
            });
        }

        // ----------------------------------------------------------------

        private string? ReadBearerOrBadRequest()
        {
            var auth = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;
            return auth.Substring("Bearer ".Length).Trim();
        }

    }
}

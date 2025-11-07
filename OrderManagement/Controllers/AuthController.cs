using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OrderManagement.DTOs;
using OrderManagement.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer; // <-- for scheme name

namespace OrderManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly IConfiguration _cfg;

        public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IConfiguration cfg)
        {
            _users = users;
            _signIn = signIn;
            _cfg = cfg;
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
            var token = GenerateJwt(user, roles, out var expiresAt);

            return Ok(new
            {
                token,
                expiresAt,
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
        public IActionResult MeDebug([FromServices] IConfiguration cfg)
        {
            var tokenStr = ReadBearerOrBadRequest();
            if (tokenStr is null) return BadRequest(new { message = "Missing or malformed Authorization header. Expected: Bearer <token>" });

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(tokenStr);
                var exp = jwt.Payload.Exp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(jwt.Payload.Exp.Value) : (DateTimeOffset?)null;
                var expectedIssuer = cfg["Jwt:Issuer"] ?? "(null)";

                return Ok(new
                {
                    header = jwt.Header,
                    payload = jwt.Payload,
                    claims = jwt.Claims.Select(c => new { c.Type, c.Value }),
                    summary = new
                    {
                        tokenIssuer = jwt.Issuer,
                        expectedIssuer,
                        subject = jwt.Subject,
                        nameId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value,
                        email = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value
                                ?? jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                        roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray(),
                        expUtc = exp?.UtcDateTime.ToString("O"),
                        nowUtc = DateTimeOffset.UtcNow.UtcDateTime.ToString("O"),
                        isExpired = exp.HasValue && DateTimeOffset.UtcNow > exp.Value
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

            var issuer = (_cfg["Jwt:Issuer"] ?? "").Trim();
            var keyStr = (_cfg["Jwt:Key"] ?? "").Trim();

            var parms = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = false,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
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
        public IActionResult ConfigSnapshot([FromServices] IConfiguration cfg)
        {
            var issuer = (cfg["Jwt:Issuer"] ?? "(null)").Trim();
            var key = (cfg["Jwt:Key"] ?? "(null)").Trim();
            var key6 = key == "(null)" ? "(null)" : (key.Length >= 6 ? key[..6] : "(short)");
            return Ok(new { issuer, key6 });
        }

        // ----------------------------------------------------------------

        private string? ReadBearerOrBadRequest()
        {
            var auth = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;
            return auth.Substring("Bearer ".Length).Trim();
        }

        private string GenerateJwt(ApplicationUser user, IList<string> roles, out DateTime expiresAtUtc)
        {
            var issuer = (_cfg["Jwt:Issuer"]
                ?? throw new InvalidOperationException("Missing Jwt:Issuer in configuration.")).Trim();

            var keyStr = (_cfg["Jwt:Key"]
                ?? throw new InvalidOperationException("Missing Jwt:Key in configuration.")).Trim();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),        // required for UserManager
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? "")
            };
            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

            expiresAtUtc = DateTime.UtcNow.AddHours(8);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: null,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OrderManagement.Configuration;
using OrderManagement.Models;

namespace OrderManagement.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IOptionsMonitor<JwtOptions> _optionsMonitor;
        private readonly JwtSecurityTokenHandler _handler = new();

        public JwtTokenService(IOptionsMonitor<JwtOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }

        public JwtTokenResult CreateToken(ApplicationUser user, IEnumerable<string> roles)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var options = _optionsMonitor.CurrentValue;
            if (string.IsNullOrWhiteSpace(options.Issuer))
            {
                throw new InvalidOperationException("Jwt:Issuer is not configured.");
            }

            if (string.IsNullOrWhiteSpace(options.Key))
            {
                throw new InvalidOperationException("Jwt:Key is not configured.");
            }

            var lifetimeMinutes = options.AccessTokenMinutes <= 0 ? 60 : options.AccessTokenMinutes;
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(lifetimeMinutes);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var audience = string.IsNullOrWhiteSpace(options.Audience) ? null : options.Audience;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            foreach (var role in roles?.Where(r => !string.IsNullOrWhiteSpace(r))
                                         .Distinct(StringComparer.OrdinalIgnoreCase)
                                         ?? Enumerable.Empty<string>())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: options.Issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials);

            var tokenString = _handler.WriteToken(token);
            return new JwtTokenResult(tokenString, expires);
        }
    }
}

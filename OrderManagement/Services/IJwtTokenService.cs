using System;
using System.Collections.Generic;
using OrderManagement.Models;

namespace OrderManagement.Services
{
    public interface IJwtTokenService
    {
        JwtTokenResult CreateToken(ApplicationUser user, IEnumerable<string> roles);
    }

    public record JwtTokenResult(string Token, DateTime ExpiresAtUtc);
}

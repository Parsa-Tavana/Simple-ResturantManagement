using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OrderManagement.Tests
{
    public class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthFlowTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Login_And_Me_Roundtrip_Succeeds()
        {
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                email = "integration@test.com",
                password = "Pass123!",
                role = "Customer",
                customerId = 1
            });
            registerResponse.EnsureSuccessStatusCode();

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "integration@test.com",
                password = "Pass123!"
            });
            loginResponse.EnsureSuccessStatusCode();

            var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(login);
            Assert.False(string.IsNullOrWhiteSpace(login!.Token));
            Assert.True(login.ExpiresAt > DateTime.UtcNow);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
            var meResponse = await _client.GetAsync("/api/auth/me");
            meResponse.EnsureSuccessStatusCode();

            var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
            Assert.NotNull(me);
            Assert.Equal("integration@test.com", me!.Email);
            Assert.Contains("Customer", me.Roles);
        }

        private sealed class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
        }

        private sealed class MeResponse
        {
            public string Email { get; set; } = string.Empty;
            public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
            public int? CustomerId { get; set; }
            public int? RestaurantId { get; set; }
        }
    }
}

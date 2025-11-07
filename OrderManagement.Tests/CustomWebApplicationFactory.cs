using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OrderManagement.Data;

namespace OrderManagement.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                var overrides = new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:Key"] = "test-secret-key-which-is-long-enough-1234567890",
                    ["Jwt:AccessTokenMinutes"] = "60",
                    ["Jwt:ClockSkewMinutes"] = "0",
                    ["Stripe:SecretKey"] = "sk_test_placeholder",
                    ["Stripe:WebhookSecret"] = "whsec_placeholder",
                    ["Stripe:Currency"] = "usd"
                };

                config.AddInMemoryCollection(overrides!);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.RemoveAll<AppDbContext>();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("OrderManagementTests");
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            return host;
        }
    }
}

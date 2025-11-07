using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using OrderManagement.Data;
using Microsoft.IdentityModel.Logging; // top of file
using OrderManagement.Models;
using OrderManagement.Repositories;
using OrderManagement.Services;
using System.Text;
using Stripe;
using System.Security.Claims;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

// OPTIONAL: show PII in token errors while debugging (remove in prod)
IdentityModelEventSource.ShowPII = true;

// ---------------------- Config ----------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=OrderMgmtDb;Trusted_Connection=True;TrustServerCertificate=True";

string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Missing Jwt:Key in configuration.");
string jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Missing Jwt:Issuer in configuration.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// ---------------------- EF Core ----------------------
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connectionString));

// ---------------------- Identity + JWT ----------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opts =>
    {
        opts.Password.RequiredLength = 6;
        opts.Password.RequireDigit = false;
        opts.Password.RequireLowercase = false;
        opts.Password.RequireUppercase = false;
        opts.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
IdentityModelEventSource.ShowPII = true; // dev only: show details for token errors
builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = true;
        o.SaveToken = true;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        // --- HARDENED extraction & diagnostics ---
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var raw = ctx.Request.Headers.Authorization.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return Task.CompletedTask;
                }

                // Must start with Bearer (case-insensitive)
                const string bearer = "Bearer ";
                var i = raw.IndexOf(bearer, StringComparison.OrdinalIgnoreCase);
                if (i < 0) return Task.CompletedTask;

                var tok = raw.Substring(i + bearer.Length);

                // Normalize common glitches
                tok = tok.Trim();                           // whitespace
                tok = tok.Trim('"');                        // stray quotes
                if (tok.Contains('%'))                      // percent-encoded (e.g., %2E instead of .)
                {
                    try { tok = Uri.UnescapeDataString(tok); } catch { /* ignore */ }
                }
                tok = tok.Replace(" ", "");                 // internal spaces should not exist

                // Optional: very defensive - if someone encoded Base64Url padding
                tok = tok.Replace("%3D", "=").Replace("%2F", "/").Replace("%2b", "+");

                // Log short preview to the console (dev)
                var head = tok.Length >= 10 ? tok[..10] : tok;
                var tail = tok.Length >= 10 ? tok[^10..] : tok;
                Console.WriteLine($"[JWT OnMessageReceived] Authorization header present={raw.Length > 0}, token={tok.Length} chars, head='{head}', tail='{tail}'");

                ctx.Token = tok;
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[JWT] auth failed: {ctx.Exception.GetType().Name} - {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                // bubble details to client headers (dev)
                ctx.Response.Headers.Append("x-jwt-error", ctx.Error ?? "invalid_token");
                ctx.Response.Headers.Append("x-jwt-desc", ctx.ErrorDescription ?? "none");
                return Task.CompletedTask;
            }
        };
    });


// ---------------------- CORS ----------------------
const string SpaCors = "spa";
builder.Services.AddCors(o =>
{
    o.AddPolicy(SpaCors, p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(_ => true)
        .AllowCredentials());
});

// ---------------------- DI ----------------------
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<ICustomerService, OrderManagement.Services.CustomerService>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<RoleSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(SpaCors);
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// quick config echo
app.MapGet("/_diag/jwt-config", (IConfiguration cfg) =>
{
    var issuer = cfg["Jwt:Issuer"] ?? "(null)";
    var key = cfg["Jwt:Key"] ?? "(null)";
    var key6 = key == "(null)" ? "(null)" : (key.Length >= 6 ? key[..6] : "(short)");
    return Results.Json(new { issuer, key6 });
}).AllowAnonymous();

app.MapFallbackToFile("/index.html");

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public class RoleSeeder
{
    private readonly RoleManager<IdentityRole> _roles;
    private readonly UserManager<ApplicationUser> _users;

    public RoleSeeder(RoleManager<IdentityRole> roles, UserManager<ApplicationUser> users)
    {
        _roles = roles; _users = users;
    }

    public async Task SeedAsync()
    {
        foreach (var r in new[] { "Customer", "RestaurantAdmin", "SuperAdmin" })
            if (!await _roles.RoleExistsAsync(r))
                await _roles.CreateAsync(new IdentityRole(r));

        var adminEmail = "admin@example.com";
        var admin = await _users.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await _users.CreateAsync(admin, "Pass123!");
            await _users.AddToRoleAsync(admin, "SuperAdmin");
        }
    }
}

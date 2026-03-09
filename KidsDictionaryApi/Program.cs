using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using KidsDictionaryApi.Data;
using KidsDictionaryApi.Endpoints;
using KidsDictionaryApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=kidsdictionary_api.db"));

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ─── JWT Authentication ───────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

// Reject the default placeholder to prevent accidental insecure deployments
const string placeholderKey = "CHANGE_THIS_SECRET_KEY_IN_PRODUCTION_MINIMUM_32_CHARS";
if (!builder.Environment.IsDevelopment() && jwtKey == placeholderKey)
    throw new InvalidOperationException(
        "Jwt:Key must be changed from the default placeholder before running in production. " +
        "Use an environment variable or Azure Key Vault to supply the secret.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "KidsDictionaryApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "KidsDictionaryApp";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── OpenAPI / Swagger ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// ─── Database Migration (auto-create schema on startup) ───────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    db.Database.EnsureCreated();
}

// ─── Middleware ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// ─── Endpoints ────────────────────────────────────────────────────────────────
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapUsageEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithTags("Health")
   .AllowAnonymous();

app.Run();

// Expose the type for WebApplicationFactory in tests
public partial class Program { }

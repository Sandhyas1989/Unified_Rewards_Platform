using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.PayrollIntegration.Integration;
using UnifiedRewards.PayrollIntegration.Persistence;
using UnifiedRewards.PayrollIntegration.Processing;

// Payroll Integration Service — real logic ported from the monolith's Payroll module:
// Polly-resilient external push + asynchronous settlement worker. Owns its own SQLite database.
var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(AppContext.BaseDirectory, "payroll-integration.db");
builder.Services.AddDbContext<PayrollDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddControllers();

// Polly resilience pipeline (built once) + resilient gateway over the mock external system.
builder.Services.AddSingleton(sp =>
    PayrollResilience.BuildPipeline(sp.GetRequiredService<ILoggerFactory>().CreateLogger("Payroll.Resilience")));
builder.Services.AddScoped<MockPayrollGateway>();
builder.Services.AddScoped<IPayrollGateway, ResilientPayrollGateway>();

// Asynchronous settlement (in-process queue + worker; Azure Service Bus is the prod swap).
builder.Services.AddSingleton<SettlementQueue>();
builder.Services.AddSingleton<SettlementProcessor>();
builder.Services.AddHostedService<SettlementBackgroundService>();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
        };
    });
builder.Services.AddAuthorization();

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5106");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PayrollDbContext>().Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "payroll-integration" }));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

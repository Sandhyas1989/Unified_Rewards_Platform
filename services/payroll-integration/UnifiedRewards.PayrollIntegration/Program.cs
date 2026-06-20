using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UnifiedRewards.Messaging;
using UnifiedRewards.PayrollIntegration;
using UnifiedRewards.PayrollIntegration.Handlers;
using UnifiedRewards.PayrollIntegration.Integration;
using UnifiedRewards.PayrollIntegration.Persistence;
using UnifiedRewards.PayrollIntegration.Processing;

// Payroll Integration Service — real logic ported from the monolith's Payroll module:
// Polly-resilient external push + asynchronous settlement worker. Owns its own SQLite database.
var builder = WebApplication.CreateBuilder(args);

var dbDir  = Environment.GetEnvironmentVariable("DB_DIR") ?? AppContext.BaseDirectory;
var dbPath = Path.Combine(dbDir, "payroll-integration.db");
builder.Services.AddDbContext<PayrollDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddControllers();
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpContextAccessor();

// Polly resilience pipeline (built once) + resilient gateway over the mock external system.
builder.Services.AddSingleton(sp =>
    PayrollResilience.BuildPipeline(sp.GetRequiredService<ILoggerFactory>().CreateLogger("Payroll.Resilience")));
builder.Services.AddScoped<MockPayrollGateway>();
builder.Services.AddScoped<IPayrollGateway, ResilientPayrollGateway>();

// Asynchronous settlement (in-process queue + worker; Azure Service Bus is the prod swap).
builder.Services.AddSingleton<SettlementQueue>();
builder.Services.AddSingleton<SettlementProcessor>();
builder.Services.AddHostedService<SettlementBackgroundService>();

// Event publishing (SettlementCompleted) and consuming (SettlementRequested → enqueue).
builder.Services.AddEventPublishing<PayrollDbContext>(builder.Configuration);
builder.Services.AddEventSubscribing(builder.Configuration, "payroll-integration");
builder.Services.AddEventHandler<SettlementRequestedHandler>();
builder.Services.AddEventHandler<BonusAwardedHandler>();
builder.Services.AddHostedService<DataRetentionService>();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var authority = jwt["Authority"];
    if (!string.IsNullOrEmpty(authority))
    {
        options.Authority = authority;
        options.Audience = jwt["Audience"];
    }
    else
    {
        using var rsa = RSA.Create();
        rsa.FromXmlString(jwt["RsaPublicKey"]!);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"], ValidAudience = jwt["Audience"],
            IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false)),
        };
    }
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payroll Integration API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "JWT from POST /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5106");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PayrollDbContext>().Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payroll Integration API v1"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "payroll-integration" }));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

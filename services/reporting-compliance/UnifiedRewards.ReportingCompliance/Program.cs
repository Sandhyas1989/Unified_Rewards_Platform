using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UnifiedRewards.Messaging;
using UnifiedRewards.ReportingCompliance;
using UnifiedRewards.ReportingCompliance.Handlers;
using UnifiedRewards.ReportingCompliance.Persistence;

// Reporting & Compliance Service — event-sourced audit store + cross-service reports + Excel export.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<UnifiedRewards.ReportingCompliance.Handlers.AuditStreamPublisher>();
builder.Services.AddSingleton<UnifiedRewards.ReportingCompliance.Handlers.EmailNotifier>();

// Own audit DB (database-per-service). No outbox needed — this service only consumes, never publishes.
var dbDir  = Environment.GetEnvironmentVariable("DB_DIR") ?? AppContext.BaseDirectory;
var dbPath = Path.Combine(dbDir, "reporting-compliance.db");
var sqlConn = builder.Configuration.GetConnectionString("Sql");
builder.Services.AddDbContext<ReportingDbContext>(o =>
{
    if (string.IsNullOrWhiteSpace(sqlConn)) o.UseSqlite($"Data Source={dbPath}");
    else o.UseSqlServer(sqlConn);
});

builder.Services.AddControllers();
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("reimbursement", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Reimbursement"] ?? "http://localhost:5104"));
builder.Services.AddHttpClient("payroll", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Payroll"] ?? "http://localhost:5106"));

// Subscribe to the event bus — ClaimEventLogHandler persists each claim lifecycle event as an AuditEntry.
builder.Services.AddEventSubscribing(builder.Configuration, "reporting-compliance");
builder.Services.AddEventHandler<ClaimEventLogHandler>();
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Reporting & Compliance API", Version = "v1" });
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
    builder.WebHost.UseUrls("http://localhost:5107");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var __db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    var __c = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>(__db);
    if (!__c.Exists()) __c.Create();
    if (!__c.HasTables()) __c.CreateTables();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reporting & Compliance API v1"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "reporting-compliance" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UnifiedRewards.Messaging;
using UnifiedRewards.ReimbursementWorkflow;
using UnifiedRewards.ReimbursementWorkflow.Integration;
using UnifiedRewards.ReimbursementWorkflow.Persistence;

// Reimbursement Workflow Service — ported from the monolith's Claims state machine, PLUS it
// orchestrates the Payroll Integration service (the reimbursement saga). Owns its own SQLite DB.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

var dbDir  = Environment.GetEnvironmentVariable("DB_DIR") ?? AppContext.BaseDirectory;
var dbPath = Path.Combine(dbDir, "reimbursement-workflow.db");
var sqlConn = builder.Configuration.GetConnectionString("Sql");
builder.Services.AddDbContext<ReimbursementDbContext>(o =>
{
    if (string.IsNullOrWhiteSpace(sqlConn)) o.UseSqlite($"Data Source={dbPath}");
    else o.UseSqlServer(sqlConn);
});
builder.Services.AddControllers();
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpContextAccessor();

// Typed HTTP client to the Payroll service (direct call locally; Azure Service Bus message in prod).
builder.Services.AddHttpClient<PayrollClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Payroll"] ?? "http://localhost:5106"));

// Event publishing via the transactional outbox (local SQLite bus; Azure Service Bus on deploy).
builder.Services.AddEventPublishing<ReimbursementDbContext>(builder.Configuration);

// Saga consumers: DocumentProcessed → "In Review"; SettlementCompleted → "Settled".
builder.Services.AddEventSubscribing(builder.Configuration, "reimbursement-workflow");
builder.Services.AddEventHandler<UnifiedRewards.ReimbursementWorkflow.Handlers.DocumentProcessedHandler>();
builder.Services.AddEventHandler<UnifiedRewards.ReimbursementWorkflow.Handlers.SettlementCompletedHandler>();
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Reimbursement Workflow API", Version = "v1" });
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
    builder.WebHost.UseUrls("http://localhost:5104");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var __db = scope.ServiceProvider.GetRequiredService<ReimbursementDbContext>();
    var __c = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>(__db);
    if (!__c.Exists()) __c.Create();
    if (!__c.HasTables()) __c.CreateTables();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reimbursement Workflow API v1"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "reimbursement-workflow" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

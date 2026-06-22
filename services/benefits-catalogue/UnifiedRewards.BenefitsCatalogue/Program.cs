using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UnifiedRewards.BenefitsCatalogue.Domain;
using UnifiedRewards.BenefitsCatalogue.Persistence;

// Benefits Catalogue Service — ported from the monolith's Benefits module. Owns its own SQLite DB.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

var dbDir  = Environment.GetEnvironmentVariable("DB_DIR") ?? AppContext.BaseDirectory;
var dbPath = Path.Combine(dbDir, "benefits-catalogue.db");
var cosmosConn = builder.Configuration.GetConnectionString("Cosmos");
var sqlConn = builder.Configuration.GetConnectionString("Sql");
builder.Services.AddDbContext<BenefitsDbContext>(o =>
{
    if (!string.IsNullOrWhiteSpace(cosmosConn)) o.UseCosmos(cosmosConn, "urp");
    else if (!string.IsNullOrWhiteSpace(sqlConn)) o.UseSqlServer(sqlConn);
    else o.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddControllers();
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpContextAccessor();

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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Benefits Catalogue API", Version = "v1" });
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
    builder.WebHost.UseUrls("http://localhost:5102");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BenefitsDbContext>();
    // DB init + demo seed is best-effort: never let a transient store issue crash startup, so the
    // container always reaches a healthy/Running state and the service stays available.
    try
    {
        if (db.Database.IsCosmos())
        {
            db.Database.EnsureCreated();
        }
        else
        {
            var __c = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>(db);
            if (!__c.Exists()) __c.Create();
            if (!__c.HasTables()) __c.CreateTables();
        }
        if (!db.Plans.AsEnumerable().Any())   // Cosmos can't translate the Any() aggregate; enumerate client-side
        {
            var tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
            db.Plans.AddRange(
                new BenefitPlan { TenantId = tenant, Name = "Comprehensive Health Insurance", Description = "Family floater up to 5 lakh.", Category = BenefitCategory.Insurance, MonthlyCost = 1200m },
                new BenefitPlan { TenantId = tenant, Name = "Gym & Wellness Membership", Description = "Gym, yoga and wellness reimbursement.", Category = BenefitCategory.Wellness, MonthlyCost = 500m },
                new BenefitPlan { TenantId = tenant, Name = "Meal Card", Description = "Tax-exempt meal allowance.", Category = BenefitCategory.Food, MonthlyCost = 2200m });
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Benefits DB init/seed failed (non-fatal); continuing startup.");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Benefits Catalogue API v1"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "benefits-catalogue" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

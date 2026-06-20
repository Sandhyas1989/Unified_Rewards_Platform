using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UnifiedRewards.EmployeeProfile.Auth;
using UnifiedRewards.EmployeeProfile.Domain;
using UnifiedRewards.EmployeeProfile.Persistence;
using UnifiedRewards.Messaging;

// Employee Profile Service — real logic ported from the monolith's User Management module.
// Owns its own SQLite database (database-per-service). Runs as a plain process (zero-install).
var builder = WebApplication.CreateBuilder(args);

// Deterministic DB location (under the app base dir) so it doesn't depend on the launch working directory.
var dbDir  = Environment.GetEnvironmentVariable("DB_DIR") ?? AppContext.BaseDirectory;
var dbPath = Path.Combine(dbDir, "employee-profile.db");
builder.Services.AddDbContext<EmployeeProfileDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddControllers();
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpContextAccessor();

// Event publishing via transactional outbox (BonusAwarded → PayrollIntegration).
builder.Services.AddEventPublishing<EmployeeProfileDbContext>(builder.Configuration);

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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Employee Profile API", Version = "v1" });
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

// Local dev runs on a fixed port; in a container ASPNETCORE_URLS takes over.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5101");
}

var app = builder.Build();

// Dev convenience: create this service's own store + seed one user per role (password: Password123!).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmployeeProfileDbContext>();
    db.Database.EnsureCreated();
    if (!db.Users.Any())
    {
        var tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var hash = PasswordHasher.Hash("Password123!");
        db.Users.AddRange(
            new HrAdmin { TenantId = tenant, FullName = "Hannah HR", Email = "hr@urp.local", PasswordHash = hash },
            new FinanceUser { TenantId = tenant, FullName = "Frank Finance", Email = "finance@urp.local", PasswordHash = hash },
            new Manager { TenantId = tenant, FullName = "Mary Manager", Email = "manager@urp.local", PasswordHash = hash, Grade = "M3", DateOfJoining = new DateOnly(2020, 1, 15) },
            new Employee { TenantId = tenant, FullName = "Ed Employee", Email = "employee@urp.local", PasswordHash = hash, Grade = "E2", DateOfJoining = new DateOnly(2022, 6, 1) });
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Profile API v1"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "employee-profile" }));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

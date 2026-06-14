using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.EmployeeProfile.Auth;
using UnifiedRewards.EmployeeProfile.Domain;
using UnifiedRewards.EmployeeProfile.Persistence;

// Employee Profile Service — real logic ported from the monolith's User Management module.
// Owns its own SQLite database (database-per-service). Runs as a plain process (zero-install).
var builder = WebApplication.CreateBuilder(args);

// Deterministic DB location (under the app base dir) so it doesn't depend on the launch working directory.
var dbPath = Path.Combine(AppContext.BaseDirectory, "employee-profile.db");
builder.Services.AddDbContext<EmployeeProfileDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddControllers();

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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "employee-profile" }));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

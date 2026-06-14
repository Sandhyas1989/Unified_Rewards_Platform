using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.CompensationRules.Persistence;
using UnifiedRewards.CompensationRules.Rules;

// Compensation Rules Engine Service — ported from the monolith's Compensation module (NRules).
// Owns its own SQLite database.
var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(AppContext.BaseDirectory, "compensation-rules.db");
builder.Services.AddDbContext<CompensationDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton<ICompensationCalculator, NRulesCompensationCalculator>();  // rules compiled once
builder.Services.AddControllers();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"], ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
    };
});
builder.Services.AddAuthorization();

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
    builder.WebHost.UseUrls("http://localhost:5103");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CompensationDbContext>().Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "compensation-rules" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

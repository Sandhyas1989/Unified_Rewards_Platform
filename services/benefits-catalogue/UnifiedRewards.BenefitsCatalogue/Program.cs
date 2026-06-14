using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.BenefitsCatalogue.Domain;
using UnifiedRewards.BenefitsCatalogue.Persistence;

// Benefits Catalogue Service — ported from the monolith's Benefits module. Owns its own SQLite DB.
var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(AppContext.BaseDirectory, "benefits-catalogue.db");
builder.Services.AddDbContext<BenefitsDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
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
    builder.WebHost.UseUrls("http://localhost:5102");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BenefitsDbContext>();
    db.Database.EnsureCreated();
    if (!db.Plans.Any())
    {
        var tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
        db.Plans.AddRange(
            new BenefitPlan { TenantId = tenant, Name = "Comprehensive Health Insurance", Description = "Family floater up to 5 lakh.", Category = BenefitCategory.Insurance, MonthlyCost = 1200m },
            new BenefitPlan { TenantId = tenant, Name = "Gym & Wellness Membership", Description = "Gym, yoga and wellness reimbursement.", Category = BenefitCategory.Wellness, MonthlyCost = 500m },
            new BenefitPlan { TenantId = tenant, Name = "Meal Card", Description = "Tax-exempt meal allowance.", Category = BenefitCategory.Food, MonthlyCost = 2200m });
        db.SaveChanges();
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "benefits-catalogue" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

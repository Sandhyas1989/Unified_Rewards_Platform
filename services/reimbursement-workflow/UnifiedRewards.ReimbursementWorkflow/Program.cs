using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.ReimbursementWorkflow.Integration;
using UnifiedRewards.ReimbursementWorkflow.Persistence;

// Reimbursement Workflow Service — ported from the monolith's Claims state machine, PLUS it
// orchestrates the Payroll Integration service (the reimbursement saga). Owns its own SQLite DB.
var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(AppContext.BaseDirectory, "reimbursement-workflow.db");
builder.Services.AddDbContext<ReimbursementDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddControllers();

// Typed HTTP client to the Payroll service (direct call locally; Azure Service Bus message in prod).
builder.Services.AddHttpClient<PayrollClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Payroll"] ?? "http://localhost:5106"));

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
    builder.WebHost.UseUrls("http://localhost:5104");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ReimbursementDbContext>().Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "reimbursement-workflow" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

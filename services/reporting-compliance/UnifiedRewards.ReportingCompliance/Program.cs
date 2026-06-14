using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Reporting & Compliance Service — cross-service read/aggregation + Excel export (ClosedXML).
// Locally aggregates over HTTP; in production it is an event-sourced read model (Service Bus). No own
// transactional DB in this skeleton (its data is derived).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("reimbursement", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Reimbursement"] ?? "http://localhost:5104"));
builder.Services.AddHttpClient("payroll", c =>
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
    builder.WebHost.UseUrls("http://localhost:5107");

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "reporting-compliance" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

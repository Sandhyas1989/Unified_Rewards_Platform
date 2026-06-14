using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.DocumentProcessing.Ocr;
using UnifiedRewards.DocumentProcessing.Persistence;
using UnifiedRewards.DocumentProcessing.Storage;

// Document & Receipt Processing Service — real logic ported from the monolith's Claims & Documents
// (file storage + OCR). Owns its own SQLite database (database-per-service). Zero-install: dotnet run.
var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(AppContext.BaseDirectory, "document-processing.db");
builder.Services.AddDbContext<DocumentDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();   // Azure Blob is the prod swap
builder.Services.AddScoped<IOcrEngine, StubOcrEngine>();           // Tesseract/Doc Intelligence in prod
builder.Services.AddControllers();

// Validates the JWT issued by the Employee Profile service (Entra ID in production). Same key/issuer.
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

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5105");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DocumentDbContext>().Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "document-processing" }));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

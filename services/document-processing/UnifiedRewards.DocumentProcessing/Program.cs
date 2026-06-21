using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UnifiedRewards.DocumentProcessing;
using UnifiedRewards.DocumentProcessing.Ocr;
using UnifiedRewards.DocumentProcessing.Persistence;
using UnifiedRewards.DocumentProcessing.Storage;
using UnifiedRewards.Messaging;

// Document & Receipt Processing Service — real logic ported from the monolith's Claims & Documents
// (file storage + OCR). Owns its own SQLite database (database-per-service). Zero-install: dotnet run.
var builder = WebApplication.CreateBuilder(args);

var dbDir  = Environment.GetEnvironmentVariable("DB_DIR") ?? AppContext.BaseDirectory;
var dbPath = Path.Combine(dbDir, "document-processing.db");
var sqlConn = builder.Configuration.GetConnectionString("Sql");
builder.Services.AddDbContext<DocumentDbContext>(o =>
{
    if (string.IsNullOrWhiteSpace(sqlConn)) o.UseSqlite($"Data Source={dbPath}");
    else o.UseSqlServer(sqlConn);
});
// Switch to Azure Blob Storage when Storage:Provider = AzureBlob (set via env var in Azure deployment).
if (string.Equals(builder.Configuration["Storage:Provider"], "AzureBlob", StringComparison.OrdinalIgnoreCase))
{
    var connStr = builder.Configuration["Storage:AzureBlob:ConnectionString"]
        ?? throw new InvalidOperationException("Storage:AzureBlob:ConnectionString required when Storage:Provider=AzureBlob.");
    var container = builder.Configuration["Storage:AzureBlob:Container"] ?? "receipts";
    builder.Services.AddSingleton<IFileStorage>(new AzureBlobFileStorage(connStr, container));
}
else
{
    builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
}
builder.Services.AddScoped<IOcrEngine, StubOcrEngine>();           // Tesseract/Doc Intelligence in prod
builder.Services.AddControllers();
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpContextAccessor();

// Publishes DocumentProcessed after a receipt is stored + OCR'd (transactional outbox → bus).
builder.Services.AddEventPublishing<DocumentDbContext>(builder.Configuration);
builder.Services.AddHostedService<DataRetentionService>();

// Validates the JWT issued by the Employee Profile service (Entra ID in production). Same key/issuer.
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Document Processing API", Version = "v1" });
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
{
    builder.WebHost.UseUrls("http://localhost:5105");
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var __db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    var __c = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>(__db);
    if (!__c.Exists()) __c.Create();
    if (!__c.HasTables()) __c.CreateTables();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Document Processing API v1"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "document-processing" }));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

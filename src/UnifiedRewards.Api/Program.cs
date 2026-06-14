using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using UnifiedRewards.Api.Middleware;
using UnifiedRewards.Api.Services;
using UnifiedRewards.Application;
using UnifiedRewards.Application.Common.Interfaces;
using UnifiedRewards.Infrastructure;
using UnifiedRewards.Infrastructure.Identity;
using UnifiedRewards.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.MinimumLevel.Information()
          .Enrich.FromLogContext()
          .WriteTo.Console());

// Application + Infrastructure composition
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Current-user accessor (used by the audit pipeline) over HttpContext.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORS for the Module Federation frontend dev servers (shell + portal remotes).
const string FrontendCorsPolicy = "FrontendDev";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(
            "http://localhost:3000", "http://localhost:3001", "http://localhost:3002",
            "http://localhost:3003", "http://localhost:3004")
        .AllowAnyHeader()
        .AllowAnyMethod()));

// JWT bearer authentication
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwtSettings);
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
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey))
        };
    });
builder.Services.AddAuthorization();

// Swagger with bearer auth support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Unified Rewards Platform API", Version = "v1" });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT bearer token.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = Array.Empty<string>() });
});

var app = builder.Build();

// Migrations + demo seed run only in Development. In production, schema migrations are applied
// as a separate, gated deployment step (not on app startup, where multiple instances would race).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await ApplicationDbContextSeeder.SeedAsync(db, passwordHasher);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Convenience: send the bare root URL to the Swagger UI in dev.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseSerilogRequestLogging();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// API Gateway for the Unified Rewards microservices — YARP reverse proxy.
// Stands in for Azure API Management locally (zero-install: just `dotnet run`).
// In production, APIM fronts the same services; routes here mirror that contract.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS for the Module Federation frontends (shell + portal remotes).
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002",
                 "http://localhost:3003", "http://localhost:3004")
    .AllowAnyHeader().AllowAnyMethod()));

// Local dev runs on a fixed port; in a container ASPNETCORE_URLS takes over.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5080");
}

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }));

// All /api/** traffic is routed to the owning microservice per appsettings ReverseProxy config.
app.MapReverseProxy();

app.Run();

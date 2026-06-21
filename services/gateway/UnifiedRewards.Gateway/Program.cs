using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

// API Gateway for the Unified Rewards microservices — YARP reverse proxy.
// Stands in for Azure API Management locally (zero-install: just `dotnet run`).
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS — origins driven by Cors:AllowedOrigins config (override via env var in Azure:
//   Cors__AllowedOrigins__0=https://urp-shell.azurecontainerapps.io)
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["http://localhost:3000", "http://localhost:3001", "http://localhost:3002",
        "http://localhost:3003", "http://localhost:3004"];

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Rate limiting — fixed-window, partitioned by tenant_id claim (falls back to remote IP).
// Override via RateLimit:PermitLimit / RateLimit:WindowSeconds in config or env vars.
var permitLimit  = builder.Configuration.GetValue("RateLimit:PermitLimit", 100);
var windowSecs   = builder.Configuration.GetValue("RateLimit:WindowSeconds", 60);

builder.Services.AddRateLimiter(o =>
{
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var key = ctx.User.FindFirst("tenant_id")?.Value
               ?? ctx.Connection.RemoteIpAddress?.ToString()
               ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit           = permitLimit,
            Window                = TimeSpan.FromSeconds(windowSecs),
            QueueProcessingOrder  = QueueProcessingOrder.OldestFirst,
            QueueLimit            = 0,
        });
    });
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = windowSecs.ToString();
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new { title = "Too many requests. Please retry after the window expires.", status = 429 }, ct);
    };
});

builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
    builder.WebHost.UseUrls("http://localhost:5080");

var app = builder.Build();

app.UseCors();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }));

app.MapReverseProxy();

app.Run();

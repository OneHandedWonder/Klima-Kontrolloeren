using KlimaKontrolloerenBackend.Data;
using KlimaKontrolloerenBackend.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<ICsvService, CsvService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-signin", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientIp(httpContext),
            _ => CreateFixedWindowOptions(4, TimeSpan.FromMinutes(1))));

    options.AddPolicy("auth-token", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientIp(httpContext),
            _ => CreateFixedWindowOptions(10, TimeSpan.FromMinutes(1))));

    options.AddPolicy("data", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientIp(httpContext),
            _ => CreateFixedWindowOptions(6, TimeSpan.FromMinutes(1))));

    options.AddPolicy("sensor-read", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientIp(httpContext),
            _ => CreateFixedWindowOptions(12, TimeSpan.FromMinutes(1))));

    options.AddPolicy("sensor-ingest", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientIp(httpContext),
            _ => CreateFixedWindowOptions(120, TimeSpan.FromMinutes(1))));
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseCors();
app.UseRateLimiter();
app.MapGet("/", () => Results.Ok("Klima-Kontrolloeren backend is running."));
app.MapControllers();
app.Run();

static string GetClientIp(HttpContext httpContext)
{
    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
        return forwardedFor.Split(',')[0].Trim();

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

static FixedWindowRateLimiterOptions CreateFixedWindowOptions(int permitLimit, TimeSpan window)
{
    return new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueLimit = 0
    };
}

public partial class Program { }

using System.Threading.RateLimiting;
using GuardProxy.Configuration;
using GuardProxy.Proxy;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

builder.Configuration
    .AddJsonFile("config.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var guardProxyConfig = ConfigurationLoader.Load(builder.Configuration);
var validationResult = GuardProxyConfigValidator.Validate(guardProxyConfig);

if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
    {
        Console.Error.WriteLine($"Configuration error: {error}");
    }

    Environment.Exit(1);
    return;
}

builder.Services.AddSingleton(guardProxyConfig);

builder.WebHost.UseUrls(guardProxyConfig.Listen.Url);

var (routes, clusters) = ProxyRouteConfigFactory.Create(guardProxyConfig.Upstream);
builder.Services.AddReverseProxy().LoadFromMemory(routes, clusters);

const string ConcurrencyLimiterPolicy = "upstream-concurrency";

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status503ServiceUnavailable;

    options.AddConcurrencyLimiter(ConcurrencyLimiterPolicy, limiterOptions =>
    {
        limiterOptions.PermitLimit = guardProxyConfig.Proxy.MaxConcurrentRequests;
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

app.UseRateLimiter();

app.MapReverseProxy().RequireRateLimiting(ConcurrencyLimiterPolicy);

app.Run();

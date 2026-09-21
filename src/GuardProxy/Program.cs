using GuardProxy.Configuration;
using GuardProxy.Proxy;

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

var app = builder.Build();

app.MapReverseProxy();

app.Run();

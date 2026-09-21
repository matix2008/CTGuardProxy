using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GuardProxy.IntegrationTests;

public sealed class FakeUpstreamServer : IAsyncDisposable
{
    private readonly IHost _host;

    private FakeUpstreamServer(IHost host, string baseUrl)
    {
        _host = host;
        BaseUrl = baseUrl;
    }

    public string BaseUrl { get; }

    public static async Task<FakeUpstreamServer> StartAsync(Action<WebApplication> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        configure(app);

        await app.StartAsync();

        var address = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!
            .Addresses.First();

        return new FakeUpstreamServer(app, address);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}

public sealed record RecordedRequest(
    string Method,
    string Path,
    string Query,
    string Body,
    string? Authorization,
    string? SoapAction);

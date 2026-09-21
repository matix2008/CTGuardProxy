using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace GuardProxy.IntegrationTests;

public sealed class GuardProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    private const int DefaultMaxConcurrentRequests = 8;

    private readonly string _upstreamUrl;
    private readonly int _maxConcurrentRequests;

    public GuardProxyWebApplicationFactory(string upstreamUrl, int maxConcurrentRequests = DefaultMaxConcurrentRequests)
    {
        _upstreamUrl = upstreamUrl;
        _maxConcurrentRequests = maxConcurrentRequests;
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        Environment.SetEnvironmentVariable("upstream__url", _upstreamUrl);
        Environment.SetEnvironmentVariable("listen__url", "http://127.0.0.1:0");
        Environment.SetEnvironmentVariable(
            "proxy__maxConcurrentRequests",
            _maxConcurrentRequests.ToString());

        return base.CreateHostBuilder()!;
    }
}

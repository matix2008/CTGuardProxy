using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace GuardProxy.IntegrationTests;

public sealed class GuardProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _upstreamUrl;

    public GuardProxyWebApplicationFactory(string upstreamUrl)
    {
        _upstreamUrl = upstreamUrl;
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        Environment.SetEnvironmentVariable("upstream__url", _upstreamUrl);
        Environment.SetEnvironmentVariable("listen__url", "http://127.0.0.1:0");
        return base.CreateHostBuilder()!;
    }
}

using Microsoft.AspNetCore.Mvc.Testing;

namespace GuardProxy.IntegrationTests;

public class HostStartupTests
{
    [Fact]
    public async Task Host_starts_and_accepts_http_requests()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.NotNull(response);
    }
}

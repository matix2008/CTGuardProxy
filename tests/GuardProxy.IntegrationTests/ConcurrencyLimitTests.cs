using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GuardProxy.IntegrationTests;

public class ConcurrencyLimitTests
{
    [Fact(Timeout = 15000)]
    public async Task Request_beyond_limit_gets_503_and_does_not_reach_upstream()
    {
        const int MaxConcurrentRequests = 3;

        var upstreamHitCount = 0;
        var arrivedCount = 0;
        var allArrived = new TaskCompletionSource();
        var releaseRequests = new TaskCompletionSource();

        await using var upstream = await FakeUpstreamServer.StartAsync(app =>
        {
            app.MapGet("/service/slow", async (HttpContext ctx) =>
            {
                Interlocked.Increment(ref upstreamHitCount);
                if (Interlocked.Increment(ref arrivedCount) == MaxConcurrentRequests)
                {
                    allArrived.TrySetResult();
                }

                await releaseRequests.Task;
                await ctx.Response.WriteAsync("done");
            });
        });

        using var factory = new GuardProxyWebApplicationFactory(
            upstream.BaseUrl + "/service",
            maxConcurrentRequests: MaxConcurrentRequests);
        using var client = factory.CreateClient();

        var inFlightRequests = Enumerable.Range(0, MaxConcurrentRequests)
            .Select(_ => client.GetAsync("/slow"))
            .ToArray();
        await allArrived.Task;

        var extraResponse = await client.GetAsync("/slow");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, extraResponse.StatusCode);
        Assert.Equal(MaxConcurrentRequests, Interlocked.CompareExchange(ref upstreamHitCount, 0, 0));

        releaseRequests.TrySetResult();
        var completedResponses = await Task.WhenAll(inFlightRequests);
        Assert.All(completedResponses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task Requests_within_limit_are_all_proxied()
    {
        await using var upstream = await FakeUpstreamServer.StartAsync(app =>
        {
            app.MapGet("/service/fast", () => "ok");
        });

        using var factory = new GuardProxyWebApplicationFactory(upstream.BaseUrl + "/service", maxConcurrentRequests: 4);
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => client.GetAsync("/fast")));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }
}

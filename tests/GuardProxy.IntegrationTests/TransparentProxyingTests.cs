using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GuardProxy.IntegrationTests;

public class TransparentProxyingTests
{
    [Fact]
    public async Task Successful_http_request_is_proxied_transparently()
    {
        RecordedRequest? recorded = null;

        await using var upstream = await FakeUpstreamServer.StartAsync(app =>
        {
            app.MapGet("/service/orders/123", async (HttpContext ctx) =>
            {
                recorded = await Capture(ctx);
                await ctx.Response.WriteAsync("order-payload");
            });
        });

        using var factory = new GuardProxyWebApplicationFactory(upstream.BaseUrl + "/service");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/orders/123?debug=true");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("order-payload", body);
        Assert.NotNull(recorded);
        Assert.Equal("GET", recorded!.Method);
        Assert.Equal("/service/orders/123", recorded.Path);
        Assert.Equal("?debug=true", recorded.Query);
    }

    [Fact]
    public async Task Basic_auth_header_is_passed_through_unmodified()
    {
        RecordedRequest? recorded = null;

        await using var upstream = await FakeUpstreamServer.StartAsync(app =>
        {
            app.MapGet("/service/secure", async (HttpContext ctx) =>
            {
                recorded = await Capture(ctx);
                ctx.Response.StatusCode = 200;
            });
        });

        using var factory = new GuardProxyWebApplicationFactory(upstream.BaseUrl + "/service");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Basic dXNlcjpwYXNz");

        var response = await client.GetAsync("/secure");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Basic dXNlcjpwYXNz", recorded!.Authorization);
    }

    [Fact]
    public async Task Soap_request_and_soap_action_header_are_passed_through()
    {
        const string soapBody = "<soap:Envelope><soap:Body><DoWork/></soap:Body></soap:Envelope>";
        RecordedRequest? recorded = null;

        await using var upstream = await FakeUpstreamServer.StartAsync(app =>
        {
            app.MapPost("/service", async (HttpContext ctx) =>
            {
                recorded = await Capture(ctx);
                ctx.Response.ContentType = "text/xml";
                await ctx.Response.WriteAsync("<soap:Envelope><soap:Body><DoWorkResponse/></soap:Body></soap:Envelope>");
            });
        });

        using var factory = new GuardProxyWebApplicationFactory(upstream.BaseUrl + "/service");
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = new StringContent(soapBody, Encoding.UTF8, "text/xml"),
        };
        request.Headers.Add("SOAPAction", "\"http://example.com/DoWork\"");

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("DoWorkResponse", responseBody);
        Assert.Equal(soapBody, recorded!.Body);
        Assert.Equal("\"http://example.com/DoWork\"", recorded.SoapAction);
    }

    [Fact]
    public async Task Soap_fault_response_is_passed_through_without_modification()
    {
        const string soapFault = "<soap:Envelope><soap:Body><soap:Fault><faultstring>boom</faultstring></soap:Fault></soap:Body></soap:Envelope>";

        await using var upstream = await FakeUpstreamServer.StartAsync(app =>
        {
            app.MapPost("/service", async (HttpContext ctx) =>
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "text/xml";
                await ctx.Response.WriteAsync(soapFault);
            });
        });

        using var factory = new GuardProxyWebApplicationFactory(upstream.BaseUrl + "/service");
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = new StringContent("<soap:Envelope/>", Encoding.UTF8, "text/xml"),
        };

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(soapFault, responseBody);
    }

    private static async Task<RecordedRequest> Capture(HttpContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();

        return new RecordedRequest(
            ctx.Request.Method,
            ctx.Request.Path.Value ?? string.Empty,
            ctx.Request.QueryString.Value ?? string.Empty,
            body,
            ctx.Request.Headers.Authorization.ToString() is { Length: > 0 } auth ? auth : null,
            ctx.Request.Headers["SOAPAction"].ToString() is { Length: > 0 } soapAction ? soapAction : null);
    }
}

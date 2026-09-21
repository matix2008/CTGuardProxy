using GuardProxy.Configuration;
using Yarp.ReverseProxy.Configuration;

namespace GuardProxy.Proxy;

public static class ProxyRouteConfigFactory
{
    private const string ClusterId = "upstream-cluster";
    private const string RouteId = "upstream-route";

    public static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Create(UpstreamConfig upstream)
    {
        var upstreamUri = new Uri(upstream.Url, UriKind.Absolute);
        var destinationAddress = new UriBuilder(upstreamUri) { Path = "/", Query = string.Empty }.Uri.ToString();
        var pathPrefix = upstreamUri.AbsolutePath.TrimEnd('/');

        var route = new RouteConfig
        {
            RouteId = RouteId,
            ClusterId = ClusterId,
            Match = new RouteMatch { Path = "{**catch-all}" },
        };

        if (!string.IsNullOrEmpty(pathPrefix))
        {
            route = route with
            {
                Transforms = new[]
                {
                    new Dictionary<string, string> { ["PathPrefix"] = pathPrefix },
                },
            };
        }

        var cluster = new ClusterConfig
        {
            ClusterId = ClusterId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["upstream"] = new DestinationConfig { Address = destinationAddress },
            },
        };

        return (new[] { route }, new[] { cluster });
    }
}

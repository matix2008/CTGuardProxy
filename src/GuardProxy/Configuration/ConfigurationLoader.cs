using Microsoft.Extensions.Configuration;

namespace GuardProxy.Configuration;

public static class ConfigurationLoader
{
    public static GuardProxyConfig Load(IConfiguration configuration)
    {
        var config = new GuardProxyConfig();
        configuration.Bind(config);
        return config;
    }
}

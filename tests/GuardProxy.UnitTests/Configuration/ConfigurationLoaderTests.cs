using GuardProxy.Configuration;
using Microsoft.Extensions.Configuration;

namespace GuardProxy.UnitTests.Configuration;

public class ConfigurationLoaderTests
{
    [Fact]
    public void Loads_and_validates_the_shipped_config_example()
    {
        var configPath = FindConfigExamplePath();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false)
            .Build();

        var config = ConfigurationLoader.Load(configuration);
        var result = GuardProxyConfigValidator.Validate(config);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    private static string FindConfigExamplePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "config-example.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("config-example.json could not be located relative to the test output directory.");
    }
}

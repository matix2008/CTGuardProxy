using GuardProxy.Configuration;

namespace GuardProxy.UnitTests.Configuration;

public class GuardProxyConfigValidatorTests
{
    private static GuardProxyConfig ValidConfig() => new()
    {
        Listen = new ListenConfig { Url = "http://127.0.0.1:8081" },
        Upstream = new UpstreamConfig { Url = "http://service.example.local:8080/service" },
        Proxy = new ProxyConfig { MaxConcurrentRequests = 8, RequestTimeoutMs = 15000, RetryCount = 0 },
        HealthCheck = new HealthCheckConfig
        {
            Enabled = true,
            Method = "POST",
            Url = "http://service.example.local:8080/service/healthcheck",
            IntervalMs = 5000,
            TimeoutMs = 2000,
            FailureThreshold = 2,
            ResponseValidation = new ResponseValidationConfig
            {
                Type = "jsonFieldEquals",
                Field = "reply",
                ExpectedValue = "I'm alive",
                Comparison = "ordinal",
            },
        },
        Logging = new LoggingConfig { Directory = "logs", Level = "Information", MaxFileSizeMb = 10, MaxFiles = 10 },
    };

    [Fact]
    public void Valid_config_produces_no_errors()
    {
        var result = GuardProxyConfigValidator.Validate(ValidConfig());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://127.0.0.1:8081")]
    public void Invalid_listen_url_is_rejected(string url)
    {
        var config = ValidConfig();
        config.Listen.Url = url;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("listen.url"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    public void Invalid_upstream_url_is_rejected(string url)
    {
        var config = ValidConfig();
        config.Upstream.Url = url;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("upstream.url"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxConcurrentRequests_must_be_positive(int value)
    {
        var config = ValidConfig();
        config.Proxy.MaxConcurrentRequests = value;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("maxConcurrentRequests"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RequestTimeoutMs_must_be_positive(int value)
    {
        var config = ValidConfig();
        config.Proxy.RequestTimeoutMs = value;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("requestTimeoutMs"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void RetryCount_must_be_zero(int value)
    {
        var config = ValidConfig();
        config.Proxy.RetryCount = value;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("retryCount"));
    }

    [Fact]
    public void Disabled_health_check_skips_further_validation()
    {
        var config = ValidConfig();
        config.HealthCheck.Enabled = false;
        config.HealthCheck.Url = "not-a-url";
        config.HealthCheck.IntervalMs = -1;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Enabled_health_check_with_invalid_url_is_rejected()
    {
        var config = ValidConfig();
        config.HealthCheck.Url = "not-a-url";

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("healthCheck.url"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void HealthCheck_intervalMs_must_be_positive(int value)
    {
        var config = ValidConfig();
        config.HealthCheck.IntervalMs = value;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("healthCheck.intervalMs"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void HealthCheck_timeoutMs_must_be_positive(int value)
    {
        var config = ValidConfig();
        config.HealthCheck.TimeoutMs = value;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("healthCheck.timeoutMs"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void HealthCheck_failureThreshold_must_be_positive(int value)
    {
        var config = ValidConfig();
        config.HealthCheck.FailureThreshold = value;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("healthCheck.failureThreshold"));
    }

    [Fact]
    public void Unsupported_response_validation_type_is_rejected()
    {
        var config = ValidConfig();
        config.HealthCheck.ResponseValidation!.Type = "regex";

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("responseValidation.type"));
    }

    [Fact]
    public void JsonFieldEquals_without_field_is_rejected()
    {
        var config = ValidConfig();
        config.HealthCheck.ResponseValidation!.Field = null;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("responseValidation.field"));
    }

    [Fact]
    public void JsonFieldEquals_without_expectedValue_is_rejected()
    {
        var config = ValidConfig();
        config.HealthCheck.ResponseValidation!.ExpectedValue = null;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("responseValidation.expectedValue"));
    }

    [Fact]
    public void None_response_validation_does_not_require_field()
    {
        var config = ValidConfig();
        config.HealthCheck.ResponseValidation = new ResponseValidationConfig { Type = "none" };

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_logging_directory_is_rejected()
    {
        var config = ValidConfig();
        config.Logging.Directory = "  ";

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("logging.directory"));
    }

    [Fact]
    public void Invalid_logging_level_is_rejected()
    {
        var config = ValidConfig();
        config.Logging.Level = "Verbose";

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("logging.level"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxFileSizeMb_must_be_positive(int value)
    {
        var config = ValidConfig();
        config.Logging.MaxFileSizeMb = value;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("logging.maxFileSizeMb"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxFiles_must_be_positive(int value)
    {
        var config = ValidConfig();
        config.Logging.MaxFiles = value;

        var result = GuardProxyConfigValidator.Validate(config);

        Assert.Contains(result.Errors, e => e.Contains("logging.maxFiles"));
    }
}

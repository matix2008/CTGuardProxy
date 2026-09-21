using Microsoft.Extensions.Logging;

namespace GuardProxy.Configuration;

public static class GuardProxyConfigValidator
{
    private static readonly string[] ValidResponseValidationTypes = ["none", "jsonFieldEquals"];

    public static ConfigValidationResult Validate(GuardProxyConfig config)
    {
        var result = new ConfigValidationResult();

        ValidateListen(config.Listen, result);
        ValidateUpstream(config.Upstream, result);
        ValidateProxy(config.Proxy, result);
        ValidateHealthCheck(config.HealthCheck, result);
        ValidateLogging(config.Logging, result);

        return result;
    }

    private static void ValidateListen(ListenConfig listen, ConfigValidationResult result)
    {
        if (!IsValidAbsoluteHttpUrl(listen.Url))
        {
            result.AddError($"listen.url '{listen.Url}' is not a valid absolute http(s) URL.");
        }
    }

    private static void ValidateUpstream(UpstreamConfig upstream, ConfigValidationResult result)
    {
        if (!IsValidAbsoluteHttpUrl(upstream.Url))
        {
            result.AddError($"upstream.url '{upstream.Url}' is not a valid absolute http(s) URL.");
        }
    }

    private static void ValidateProxy(ProxyConfig proxy, ConfigValidationResult result)
    {
        if (proxy.MaxConcurrentRequests <= 0)
        {
            result.AddError("proxy.maxConcurrentRequests must be greater than 0.");
        }

        if (proxy.RequestTimeoutMs <= 0)
        {
            result.AddError("proxy.requestTimeoutMs must be greater than 0.");
        }

        if (proxy.RetryCount != 0)
        {
            result.AddError("proxy.retryCount must be 0.");
        }
    }

    private static void ValidateHealthCheck(HealthCheckConfig healthCheck, ConfigValidationResult result)
    {
        if (!healthCheck.Enabled)
        {
            return;
        }

        if (!IsValidHttpMethod(healthCheck.Method))
        {
            result.AddError($"healthCheck.method '{healthCheck.Method}' is not a valid HTTP method.");
        }

        if (!IsValidAbsoluteHttpUrl(healthCheck.Url))
        {
            result.AddError($"healthCheck.url '{healthCheck.Url}' is not a valid absolute http(s) URL.");
        }

        if (healthCheck.IntervalMs <= 0)
        {
            result.AddError("healthCheck.intervalMs must be greater than 0.");
        }

        if (healthCheck.TimeoutMs <= 0)
        {
            result.AddError("healthCheck.timeoutMs must be greater than 0.");
        }

        if (healthCheck.FailureThreshold <= 0)
        {
            result.AddError("healthCheck.failureThreshold must be greater than 0.");
        }

        ValidateResponseValidation(healthCheck.ResponseValidation, result);
    }

    private static void ValidateResponseValidation(ResponseValidationConfig? validation, ConfigValidationResult result)
    {
        if (validation is null)
        {
            return;
        }

        if (!ValidResponseValidationTypes.Contains(validation.Type))
        {
            result.AddError($"healthCheck.responseValidation.type '{validation.Type}' is not supported.");
            return;
        }

        if (validation.Type == "jsonFieldEquals")
        {
            if (string.IsNullOrWhiteSpace(validation.Field))
            {
                result.AddError("healthCheck.responseValidation.field is required when type is 'jsonFieldEquals'.");
            }

            if (validation.ExpectedValue is null)
            {
                result.AddError("healthCheck.responseValidation.expectedValue is required when type is 'jsonFieldEquals'.");
            }
        }
    }

    private static void ValidateLogging(LoggingConfig logging, ConfigValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(logging.Directory))
        {
            result.AddError("logging.directory must not be empty.");
        }

        if (!Enum.TryParse<LogLevel>(logging.Level, ignoreCase: true, out _))
        {
            result.AddError($"logging.level '{logging.Level}' is not a valid log level.");
        }

        if (logging.MaxFileSizeMb <= 0)
        {
            result.AddError("logging.maxFileSizeMb must be greater than 0.");
        }

        if (logging.MaxFiles <= 0)
        {
            result.AddError("logging.maxFiles must be greater than 0.");
        }
    }

    private static bool IsValidAbsoluteHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool IsValidHttpMethod(string method) =>
        !string.IsNullOrWhiteSpace(method) &&
        method.All(c => !char.IsWhiteSpace(c));
}

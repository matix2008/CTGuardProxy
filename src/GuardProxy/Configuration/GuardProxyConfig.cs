namespace GuardProxy.Configuration;

public sealed class GuardProxyConfig
{
    public ListenConfig Listen { get; set; } = new();
    public UpstreamConfig Upstream { get; set; } = new();
    public ProxyConfig Proxy { get; set; } = new();
    public HealthCheckConfig HealthCheck { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
}

public sealed class ListenConfig
{
    public string Url { get; set; } = string.Empty;
}

public sealed class UpstreamConfig
{
    public string Url { get; set; } = string.Empty;
}

public sealed class ProxyConfig
{
    public int MaxConcurrentRequests { get; set; }
    public int RequestTimeoutMs { get; set; }
    public int RetryCount { get; set; }
}

public sealed class HealthCheckConfig
{
    public bool Enabled { get; set; }
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = string.Empty;
    public int IntervalMs { get; set; }
    public int TimeoutMs { get; set; }
    public int FailureThreshold { get; set; }
    public ResponseValidationConfig? ResponseValidation { get; set; }
}

public sealed class ResponseValidationConfig
{
    public string Type { get; set; } = "none";
    public string? Field { get; set; }
    public string? ExpectedValue { get; set; }
    public string? Comparison { get; set; }
}

public sealed class LoggingConfig
{
    public string Directory { get; set; } = "logs";
    public string Level { get; set; } = "Information";
    public int MaxFileSizeMb { get; set; }
    public int MaxFiles { get; set; }
}

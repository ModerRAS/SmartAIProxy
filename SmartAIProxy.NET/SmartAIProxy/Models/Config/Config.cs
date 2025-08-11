namespace SmartAIProxy.Models.Config;

public class AppConfig
{
    public ServerConfig Server { get; set; } = new();
    public List<ChannelConfig> Channels { get; set; } = new();
    public List<RuleConfig> Rules { get; set; } = new();
    public MonitorConfig Monitor { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
}

public class ServerConfig
{
    public string Listen { get; set; } = "0.0.0.0:8080";
    public int Timeout { get; set; } = 30;
    public int MaxConnections { get; set; } = 1000;
}

public class ChannelConfig
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "openai";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public double PricePerToken { get; set; }
    public int DailyLimit { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = "active";
    public Dictionary<string, string> ModelMapping { get; set; } = new();
}

public class RuleConfig
{
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class MonitorConfig
{
    public bool Enable { get; set; } = true;
    public string PrometheusListen { get; set; } = "0.0.0.0:9100";
}

public class SecurityConfig
{
    public AuthConfig Auth { get; set; } = new();
    public RateLimitConfig RateLimit { get; set; } = new();
}

public class AuthConfig
{
    public JwtConfig Jwt { get; set; } = new();
    public Dictionary<string, string> ApiKeys { get; set; } = new();
}

public class JwtConfig
{
    public string Secret { get; set; } = "your-secret-key-here";
    public string Issuer { get; set; } = "SmartAIProxy";
    public string Audience { get; set; } = "SmartAIProxy-Client";
    public int ExpiryMinutes { get; set; } = 60;
}

public class RateLimitConfig
{
    public int RequestsPerMinute { get; set; } = 60;
    public int Burst { get; set; } = 10;
}
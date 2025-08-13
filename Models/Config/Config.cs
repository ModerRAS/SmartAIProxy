namespace SmartAIProxy.Models.Config;

/// <summary>
/// 应用程序配置类，包含所有系统配置信息
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 服务器配置
    /// </summary>
    public ServerConfig Server { get; set; } = new();
    
    /// <summary>
    /// AI服务通道配置列表
    /// </summary>
    public List<ChannelConfig> Channels { get; set; } = new();
    
    /// <summary>
    /// 路由规则配置列表
    /// </summary>
    public List<RuleConfig> Rules { get; set; } = new();
    
    /// <summary>
    /// 监控配置
    /// </summary>
    public MonitorConfig Monitor { get; set; } = new();
    
    /// <summary>
    /// 安全配置
    /// </summary>
    public SecurityConfig Security { get; set; } = new();
}

/// <summary>
/// 服务器配置类
/// </summary>
public class ServerConfig
{
    /// <summary>
    /// 服务器监听地址和端口
    /// </summary>
    public string Listen { get; set; } = "0.0.0.0:8080";
    
    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int Timeout { get; set; } = 30;
    
    /// <summary>
    /// 最大连接数
    /// </summary>
    public int MaxConnections { get; set; } = 1000;
}

/// <summary>
/// AI服务通道配置类
/// </summary>
public class ChannelConfig
{
    /// <summary>
    /// 通道名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 通道类型（如openai、anthropic等）
    /// </summary>
    public string Type { get; set; } = "openai";
    
    /// <summary>
    /// AI服务API端点
    /// </summary>
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
    
    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 每个令牌的价格
    /// </summary>
    public double PricePerToken { get; set; }
    
    /// <summary>
    /// 每日令牌限制
    /// </summary>
    public int DailyLimit { get; set; }
    
    /// <summary>
    /// 通道优先级（数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// 通道状态（active、inactive、maintenance等）
    /// </summary>
    public string Status { get; set; } = "active";
    
    /// <summary>
    /// 模型映射字典，用于将请求中的模型名映射为通道支持的模型名
    /// </summary>
    public Dictionary<string, string> ModelMapping { get; set; } = new();
}

/// <summary>
/// 路由规则配置类
/// </summary>
public class RuleConfig
{
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 规则匹配时使用的通道名称
    /// </summary>
    public string Channel { get; set; } = string.Empty;
    
    /// <summary>
    /// 规则表达式，使用NCalc语法
    /// </summary>
    public string Expression { get; set; } = string.Empty;
    
    /// <summary>
    /// 规则优先级（数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// 监控配置类
/// </summary>
public class MonitorConfig
{
    /// <summary>
    /// 是否启用监控
    /// </summary>
    public bool Enable { get; set; } = true;
    
    /// <summary>
    /// Prometheus监控服务监听地址和端口
    /// </summary>
    public string PrometheusListen { get; set; } = "0.0.0.0:9100";
}

/// <summary>
/// 安全配置类
/// </summary>
public class SecurityConfig
{
    /// <summary>
    /// 身份验证配置
    /// </summary>
    public AuthConfig Auth { get; set; } = new();
    
    /// <summary>
    /// 速率限制配置
    /// </summary>
    public RateLimitConfig RateLimit { get; set; } = new();
}

/// <summary>
/// 身份验证配置类
/// </summary>
public class AuthConfig
{
    /// <summary>
    /// JWT配置
    /// </summary>
    public JwtConfig Jwt { get; set; } = new();
    
    /// <summary>
    /// API密钥字典，键为密钥名称，值为密钥值
    /// </summary>
    public Dictionary<string, string> ApiKeys { get; set; } = new();
}

/// <summary>
    /// JWT配置类
    /// </summary>
public class JwtConfig
{
    /// <summary>
    /// JWT签名密钥
    /// </summary>
    public string Secret { get; set; } = "your-secret-key-here";
    
    /// <summary>
    /// JWT签发者
    /// </summary>
    public string Issuer { get; set; } = "SmartAIProxy";
    
    /// <summary>
    /// JWT接收者
    /// </summary>
    public string Audience { get; set; } = "SmartAIProxy-Client";
    
    /// <summary>
    /// JWT过期时间（分钟）
    /// </summary>
    public int ExpiryMinutes { get; set; } = 60;
}

/// <summary>
/// 速率限制配置类
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// 每分钟允许的请求数
    /// </summary>
    public int RequestsPerMinute { get; set; } = 60;
    
    /// <summary>
    /// 突发请求数
    /// </summary>
    public int Burst { get; set; } = 10;
}
using SmartAIProxy.Models.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SmartAIProxy.Core.Config;

/// <summary>
/// 配置服务接口，提供应用程序配置的管理功能
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 获取当前应用程序配置
    /// </summary>
    /// <returns>应用程序配置对象</returns>
    AppConfig GetConfig();

    /// <summary>
    /// 从配置文件重新加载配置
    /// </summary>
    void ReloadConfig();

    /// <summary>
    /// 更新应用程序配置并保存到文件
    /// </summary>
    /// <param name="config">新的应用程序配置</param>
    void UpdateConfig(AppConfig config);
}

/// <summary>
/// 配置服务实现类，负责从YAML文件加载和保存应用程序配置
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configPath;
    private AppConfig _config;
    private readonly object _lock = new();

    /// <summary>
    /// 配置服务构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="env">Web主机环境</param>
    public ConfigurationService(ILogger<ConfigurationService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _configPath = Path.Combine(env.ContentRootPath, "config", "smartaiproxy.yaml");
        _config = LoadConfig();
    }

    /// <summary>
    /// 获取当前应用程序配置
    /// </summary>
    /// <returns>应用程序配置对象</returns>
    public AppConfig GetConfig()
    {
        lock (_lock)
        {
            return _config;
        }
    }

    /// <summary>
    /// 从配置文件重新加载配置
    /// </summary>
    public void ReloadConfig()
    {
        lock (_lock)
        {
            var newConfig = LoadConfig();
            _config = newConfig;
            _logger.LogInformation("Configuration reloaded successfully");
        }
    }

    /// <summary>
    /// 更新应用程序配置并保存到文件
    /// </summary>
    /// <param name="config">新的应用程序配置</param>
    public void UpdateConfig(AppConfig config)
    {
        lock (_lock)
        {
            _config = config;
            SaveConfig(config);
            _logger.LogInformation("Configuration updated successfully");
        }
    }

    /// <summary>
    /// 从YAML文件加载配置
    /// </summary>
    /// <returns>加载的应用程序配置</returns>
    private AppConfig LoadConfig()
    {
        try
        {
            // 如果配置文件不存在，创建默认配置
            if (!File.Exists(_configPath))
            {
                _logger.LogWarning("Config file not found at {ConfigPath}, creating default config", _configPath);
                var defaultConfig = CreateDefaultConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }

            // 读取并解析YAML配置文件
            var yaml = File.ReadAllText(_configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<AppConfig>(yaml);
            _logger.LogInformation("Configuration loaded successfully from {ConfigPath}", _configPath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {ConfigPath}", _configPath);
            return CreateDefaultConfig();
        }
    }

    /// <summary>
    /// 将配置保存到YAML文件
    /// </summary>
    /// <param name="config">要保存的应用程序配置</param>
    private void SaveConfig(AppConfig config)
    {
        try
        {
            // 确保配置目录存在
            var directory = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 序列化配置为YAML并保存
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(config);
            File.WriteAllText(_configPath, yaml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to {ConfigPath}", _configPath);
        }
    }

    /// <summary>
    /// 创建默认的应用程序配置
    /// </summary>
    /// <returns>默认的应用程序配置</returns>
    private AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            Server = new ServerConfig
            {
                Listen = "0.0.0.0:8080",
                Timeout = 30,
                MaxConnections = 1000
            },
            Channels = new List<ChannelConfig>
            {
                new ChannelConfig
                {
                    Name = "Free Channel A",
                    Type = "openai",
                    Endpoint = "https://api.openai.com/v1",
                    ApiKey = "your-openai-api-key",
                    PricePerToken = 0,
                    DailyLimit = 10000,
                    Priority = 1,
                    Status = "active"
                },
                new ChannelConfig
                {
                    Name = "Paid Channel B",
                    Type = "openai",
                    Endpoint = "https://api.openai.com/v1",
                    ApiKey = "your-openai-api-key",
                    PricePerToken = 0.01,
                    DailyLimit = 50000,
                    Priority = 2,
                    Status = "active"
                }
            },
            Rules = new List<RuleConfig>
            {
                new RuleConfig
                {
                    Name = "Free Priority",
                    Channel = "Free Channel A",
                    Expression = "day_tokens_used < daily_limit",
                    Priority = 1
                },
                new RuleConfig
                {
                    Name = "Discount Hours",
                    Channel = "Free Channel A",
                    Expression = "time_of_day >= '00:00' AND time_of_day <= '06:00'",
                    Priority = 2
                }
            },
            Monitor = new MonitorConfig
            {
                Enable = true,
                PrometheusListen = "0.0.0.0:9100"
            },
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    Jwt = new JwtConfig
                    {
                        Secret = "your-secret-key-here",
                        Issuer = "SmartAIProxy",
                        Audience = "SmartAIProxy-Client",
                        ExpiryMinutes = 60
                    },
                    ApiKeys = new Dictionary<string, string>
                    {
                        { "default", "your-api-key-here" }
                    }
                },
                RateLimit = new RateLimitConfig
                {
                    RequestsPerMinute = 60,
                    Burst = 10
                }
            }
        };
    }
}
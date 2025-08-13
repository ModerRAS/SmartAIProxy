using SmartAIProxy.Models.Config;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SmartAIProxy.Core.Config;

public interface IConfigurationService
{
    AppConfig GetConfig();
    void ReloadConfig();
    void UpdateConfig(AppConfig config);
}

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configPath;
    private AppConfig _config;
    private readonly object _lock = new();

    public ConfigurationService(ILogger<ConfigurationService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _configPath = Path.Combine(env.ContentRootPath, "config", "smartaiproxy.yaml");
        _config = LoadConfig();
    }

    public AppConfig GetConfig()
    {
        lock (_lock)
        {
            return _config;
        }
    }

    public void ReloadConfig()
    {
        lock (_lock)
        {
            var newConfig = LoadConfig();
            _config = newConfig;
            _logger.LogInformation("Configuration reloaded successfully");
        }
    }

    public void UpdateConfig(AppConfig config)
    {
        lock (_lock)
        {
            _config = config;
            SaveConfig(config);
            _logger.LogInformation("Configuration updated successfully");
        }
    }

    private AppConfig LoadConfig()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogWarning("Config file not found at {ConfigPath}, creating default config", _configPath);
                var defaultConfig = CreateDefaultConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }

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

    private void SaveConfig(AppConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

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
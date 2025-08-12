using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Moq;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Models.Config;
using Xunit;

namespace SmartAIProxy.Tests.Core;

public class ConfigurationServiceTests
{
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly string _testConfigPath;
    private readonly string _testConfigContent;

    public ConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        
        // Setup test directory
        var testDir = Path.Combine(Path.GetTempPath(), "SmartAIProxyTests");
        if (!Directory.Exists(testDir))
        {
            Directory.CreateDirectory(testDir);
        }
        
        _testConfigPath = Path.Combine(testDir, "config", "smartaiproxy.yaml");
        var configDir = Path.GetDirectoryName(_testConfigPath);
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }
        
        _testConfigContent = @"
server:
  listen: ""0.0.0.0:8080""
  timeout: 30
  max_connections: 1000
channels:
  - name: ""Test Channel""
    type: ""openai""
    endpoint: ""https://api.openai.com/v1""
    api_key: ""test-key""
    price_per_token: 0.01
    daily_limit: 10000
    priority: 1
    status: ""active""
rules:
  - name: ""Test Rule""
    channel: ""Test Channel""
    expression: ""true""
    priority: 1
monitor:
  enable: true
  prometheus_listen: ""0.0.0.0:9100""
security:
  auth:
    jwt:
      secret: ""test-secret-key""
      issuer: ""SmartAIProxy""
      audience: ""SmartAIProxy-Client""
      expiry_minutes: 60
    api_keys:
      default: ""test-api-key""
  rate_limit:
    requests_per_minute: 60
    burst: 10
";
        
        _mockEnv.Setup(env => env.ContentRootPath).Returns(testDir);
    }

    [Fact]
    public void Constructor_LoadsConfigFromFile()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);

        // Act
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // Assert
        var config = configService.GetConfig();
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
        Assert.Single(config.Channels);
        Assert.Equal("Test Channel", config.Channels[0].Name);
        Assert.Single(config.Rules);
        Assert.Equal("Test Rule", config.Rules[0].Name);
    }

    [Fact]
    public void Constructor_CreatesDefaultConfigWhenFileNotExists()
    {
        // Arrange
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }

        // Act
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // Assert
        var config = configService.GetConfig();
        Assert.NotNull(config);
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
        Assert.NotEmpty(config.Channels);
    }

    [Fact]
    public void GetConfig_ReturnsCurrentConfig()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // Act
        var config = configService.GetConfig();

        // Assert
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
        Assert.Single(config.Channels);
    }

    [Fact]
    public void ReloadConfig_ReloadsConfigFromFile()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);
        
        // Modify the config file
        var modifiedContent = _testConfigContent.Replace("0.0.0.0:8080", "0.0.0.0:9090");
        File.WriteAllText(_testConfigPath, modifiedContent);

        // Act
        configService.ReloadConfig();
        var config = configService.GetConfig();

        // Assert
        Assert.Equal("0.0.0.0:9090", config.Server.Listen);
    }

    [Fact]
    public void UpdateConfig_UpdatesAndSavesConfig()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);
        
        var newConfig = new AppConfig
        {
            Server = new ServerConfig { Listen = "0.0.0.0:9090", Timeout = 60, MaxConnections = 2000 },
            Channels = new List<ChannelConfig>(),
            Rules = new List<RuleConfig>(),
            Monitor = new MonitorConfig { Enable = false },
            Security = new SecurityConfig()
        };

        // Act
        configService.UpdateConfig(newConfig);
        var config = configService.GetConfig();

        // Assert
        Assert.Equal("0.0.0.0:9090", config.Server.Listen);
        Assert.Equal(60, config.Server.Timeout);
        Assert.Equal(2000, config.Server.MaxConnections);
        Assert.False(config.Monitor.Enable);
    }

    [Fact]
    public void GetConfig_IsThreadSafe()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // Act
        var results = new List<AppConfig>();
        var threads = new List<Thread>();
        
        for (int i = 0; i < 10; i++)
        {
            var thread = new Thread(() =>
            {
                results.Add(configService.GetConfig());
            });
            threads.Add(thread);
            thread.Start();
        }
        
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        Assert.Equal(10, results.Count);
        foreach (var result in results)
        {
            Assert.Equal("0.0.0.0:8080", result.Server.Listen);
        }
    }

    [Fact]
    public void ReloadConfig_IsThreadSafe()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);
        
        // Modify the config file
        var modifiedContent = _testConfigContent.Replace("0.0.0.0:8080", "0.0.0.0:9090");
        File.WriteAllText(_testConfigPath, modifiedContent);

        // Act
        var results = new List<AppConfig>();
        var threads = new List<Thread>();
        
        for (int i = 0; i < 10; i++)
        {
            var thread = new Thread(() =>
            {
                configService.ReloadConfig();
                results.Add(configService.GetConfig());
            });
            threads.Add(thread);
            thread.Start();
        }
        
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        Assert.Equal(10, results.Count);
        foreach (var result in results)
        {
            Assert.Equal("0.0.0.0:9090", result.Server.Listen);
        }
    }

    [Fact]
    public void UpdateConfig_IsThreadSafe()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // Act
        var results = new List<AppConfig>();
        var threads = new List<Thread>();
        
        for (int i = 0; i < 10; i++)
        {
            var threadNum = i;
            var thread = new Thread(() =>
            {
                var newConfig = new AppConfig
                {
                    Server = new ServerConfig { Listen = $"0.0.0.0:{8080 + threadNum}", Timeout = 30, MaxConnections = 1000 },
                    Channels = new List<ChannelConfig>(),
                    Rules = new List<RuleConfig>(),
                    Monitor = new MonitorConfig { Enable = true },
                    Security = new SecurityConfig()
                };
                
                configService.UpdateConfig(newConfig);
                results.Add(configService.GetConfig());
            });
            threads.Add(thread);
            thread.Start();
        }
        
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert
        Assert.Equal(10, results.Count);
        // All configs should have the same final value (the last one updated)
        var finalConfig = results[0];
        foreach (var result in results)
        {
            Assert.Equal(finalConfig.Server.Listen, result.Server.Listen);
        }
    }

    [Fact]
    public void Constructor_HandlesInvalidConfigFile()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, "invalid: yaml: content: [");

        // Act
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // Assert
        var config = configService.GetConfig();
        Assert.NotNull(config);
        // Should fall back to default config
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
    }

    [Fact]
    public void ReloadConfig_HandlesInvalidConfigFile()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);
        
        // Replace with invalid content
        File.WriteAllText(_testConfigPath, "invalid: yaml: content: [");

        // Act
        configService.ReloadConfig();
        var config = configService.GetConfig();

        // Assert
        // Should keep the previous valid config
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
    }

    [Fact]
    public void Constructor_HandlesEmptyConfigFile()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, "");

        // Act
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // Assert
        var config = configService.GetConfig();
        Assert.NotNull(config);
        // Should fall back to default config
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
    }

    [Fact]
    public void UpdateConfig_HandlesNullConfig()
    {
        // Arrange
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => configService.UpdateConfig(null));
    }
}
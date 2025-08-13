using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Moq;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Models.Config;
using Xunit;

namespace SmartAIProxy.Tests.Core;

/// <summary>
/// 配置服务测试类
/// 用于验证配置服务的加载、保存、重载和线程安全功能
/// </summary>
public class ConfigurationServiceTests
{
    private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly string _testConfigPath;
    private readonly string _testConfigContent;

    /// <summary>
    /// 配置服务测试构造函数
    /// 设置测试环境和测试配置内容
    /// </summary>
    public ConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationService>>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        
        // 设置测试目录
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
        
        // 测试配置内容
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

    /// <summary>
    /// 测试构造函数是否从文件加载配置
    /// </summary>
    [Fact]
    public void Constructor_LoadsConfigFromFile()
    {
        // 准备
        File.WriteAllText(_testConfigPath, _testConfigContent);

        // 执行
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // 断言
        var config = configService.GetConfig();
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
        Assert.Single(config.Channels);
        Assert.Equal("Test Channel", config.Channels[0].Name);
        Assert.Single(config.Rules);
        Assert.Equal("Test Rule", config.Rules[0].Name);
    }

    /// <summary>
    /// 测试构造函数在配置文件不存在时是否创建默认配置
    /// </summary>
    [Fact]
    public void Constructor_CreatesDefaultConfigWhenFileNotExists()
    {
        // 准备
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }

        // 执行
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // 断言
        var config = configService.GetConfig();
        Assert.NotNull(config);
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
        Assert.NotEmpty(config.Channels);
    }

    /// <summary>
    /// 测试GetConfig方法是否返回当前配置
    /// </summary>
    [Fact]
    public void GetConfig_ReturnsCurrentConfig()
    {
        // 准备
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // 执行
        var config = configService.GetConfig();

        // 断言
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
        Assert.Single(config.Channels);
    }

    /// <summary>
    /// 测试ReloadConfig方法是否从文件重新加载配置
    /// </summary>
    [Fact]
    public void ReloadConfig_ReloadsConfigFromFile()
    {
        // 准备
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);
        
        // 修改配置文件
        var modifiedContent = _testConfigContent.Replace("0.0.0.0:8080", "0.0.0.0:9090");
        File.WriteAllText(_testConfigPath, modifiedContent);

        // 执行
        configService.ReloadConfig();
        var config = configService.GetConfig();

        // 断言
        Assert.Equal("0.0.0.0:9090", config.Server.Listen);
    }

    /// <summary>
    /// 测试UpdateConfig方法是否更新并保存配置
    /// </summary>
    [Fact]
    public void UpdateConfig_UpdatesAndSavesConfig()
    {
        // 准备
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

        // 执行
        configService.UpdateConfig(newConfig);
        var config = configService.GetConfig();

        // 断言
        Assert.Equal("0.0.0.0:9090", config.Server.Listen);
        Assert.Equal(60, config.Server.Timeout);
        Assert.Equal(2000, config.Server.MaxConnections);
        Assert.False(config.Monitor.Enable);
    }

    /// <summary>
    /// 测试GetConfig方法是否是线程安全的
    /// </summary>
    [Fact]
    public void GetConfig_IsThreadSafe()
    {
        // 准备
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // 执行
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

        // 断言
        Assert.Equal(10, results.Count);
        foreach (var result in results)
        {
            Assert.Equal("0.0.0.0:8080", result.Server.Listen);
        }
    }

    /// <summary>
    /// 测试ReloadConfig方法是否是线程安全的
    /// </summary>
    [Fact]
    public void ReloadConfig_IsThreadSafe()
    {
        // 准备
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);
        
        // 修改配置文件
        var modifiedContent = _testConfigContent.Replace("0.0.0.0:8080", "0.0.0.0:9090");
        File.WriteAllText(_testConfigPath, modifiedContent);

        // 执行
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

        // 断言
        Assert.Equal(10, results.Count);
        foreach (var result in results)
        {
            Assert.Equal("0.0.0.0:9090", result.Server.Listen);
        }
    }

    /// <summary>
    /// 测试UpdateConfig方法是否是线程安全的
    /// </summary>
    [Fact]
    public void UpdateConfig_IsThreadSafe()
    {
        // 准备
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // 执行
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

        // 断言
        Assert.Equal(10, results.Count);
        // 所有配置应该具有相同的最终值（最后更新的值）
        var finalConfig = results[0];
        foreach (var result in results)
        {
            Assert.Equal(finalConfig.Server.Listen, result.Server.Listen);
        }
    }

    /// <summary>
    /// 测试构造函数是否处理无效的配置文件
    /// </summary>
    [Fact]
    public void Constructor_HandlesInvalidConfigFile()
    {
        // 准备
        File.WriteAllText(_testConfigPath, "invalid: yaml: content: [");

        // 执行
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // 断言
        var config = configService.GetConfig();
        Assert.NotNull(config);
        // 应该回退到默认配置
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
    }

    /// <summary>
    /// 测试ReloadConfig方法是否处理无效的配置文件
    /// </summary>
    [Fact]
    public void ReloadConfig_HandlesInvalidConfigFile()
    {
        // 准备
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);
        
        // 替换为无效内容
        File.WriteAllText(_testConfigPath, "invalid: yaml: content: [");

        // 执行
        configService.ReloadConfig();
        var config = configService.GetConfig();

        // 断言
        // 应该保持之前的有效配置
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
    }

    /// <summary>
    /// 测试构造函数是否处理空的配置文件
    /// </summary>
    [Fact]
    public void Constructor_HandlesEmptyConfigFile()
    {
        // 准备
        File.WriteAllText(_testConfigPath, "");

        // 执行
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // 断言
        var config = configService.GetConfig();
        Assert.NotNull(config);
        // 应该回退到默认配置
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
    }

    /// <summary>
    /// 测试UpdateConfig方法是否处理null配置
    /// </summary>
    [Fact]
    public void UpdateConfig_HandlesNullConfig()
    {
        // 准备
        File.WriteAllText(_testConfigPath, _testConfigContent);
        var configService = new ConfigurationService(_mockLogger.Object, _mockEnv.Object);

        // 执行和断言
        Assert.Throws<ArgumentNullException>(() => configService.UpdateConfig(null));
    }
}
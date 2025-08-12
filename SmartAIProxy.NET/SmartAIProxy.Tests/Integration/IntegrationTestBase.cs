using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SmartAIProxy.Core.Channels;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Core.Rules;
using SmartAIProxy.Models.Config;
using System.Text.Json;

namespace SmartAIProxy.Tests.Integration;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _httpClient;
    protected readonly JsonSerializerOptions _jsonOptions;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace services with test implementations
                services.AddSingleton<IConfigurationService>(sp => 
                    new TestConfigurationService(GetTestConfig()));
                
                services.AddSingleton<IChannelService, TestChannelService>();
                services.AddSingleton<IRuleEngine, TestRuleEngine>();
            });
        });

        _httpClient = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    protected AppConfig GetTestConfig()
    {
        return new AppConfig
        {
            Server = new ServerConfig
            {
                Listen = "localhost:8080",
                Timeout = 30,
                MaxConnections = 100
            },
            Channels = new List<ChannelConfig>
            {
                new ChannelConfig
                {
                    Name = "test-channel",
                    Type = "openai",
                    Endpoint = "https://api.openai.com/v1",
                    ApiKey = "test-api-key",
                    PricePerToken = 0.01,
                    DailyLimit = 10000,
                    Priority = 1,
                    Status = "active"
                }
            },
            Rules = new List<RuleConfig>
            {
                new RuleConfig
                {
                    Name = "test-rule",
                    Channel = "test-channel",
                    Expression = "true",
                    Priority = 1
                }
            },
            Monitor = new MonitorConfig
            {
                Enable = false,
                PrometheusListen = "localhost:9100"
            },
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string>
                    {
                        { "test-key", "valid-api-key" }
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

    protected StringContent GetJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, _jsonOptions);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }
}

// Test implementations for services
public class TestConfigurationService : IConfigurationService
{
    private readonly AppConfig _config;

    public TestConfigurationService(AppConfig config)
    {
        _config = config;
    }

    public AppConfig GetConfig() => _config;

    public void ReloadConfig() { }

    public void UpdateConfig(AppConfig config) { }
}

public class TestChannelService : IChannelService
{
    private readonly Dictionary<string, int> _channelUsage = new();

    public List<ChannelConfig> GetChannels() => new List<ChannelConfig>();

    public ChannelConfig? GetChannelByName(string name) => null;

    public void AddOrUpdateChannel(ChannelConfig channel) { }

    public void RemoveChannel(string name) { }

    public void UpdateChannelStatus(string name, string status) { }

    public Dictionary<string, int> GetChannelUsage() => _channelUsage;

    public void UpdateChannelUsage(string channelName, int tokens)
    {
        _channelUsage[channelName] = tokens;
    }
}

public class TestRuleEngine : IRuleEngine
{
    public ChannelConfig? EvaluateRules(List<RuleConfig> rules, List<ChannelConfig> channels, Dictionary<string, object> context)
    {
        return channels.FirstOrDefault();
    }
}
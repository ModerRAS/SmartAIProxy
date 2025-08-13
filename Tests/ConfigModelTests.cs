using System;
using System.Collections.Generic;
using SmartAIProxy.Models.Config;
using Xunit;

namespace SmartAIProxy.Tests.Models;

public class ConfigModelTests
{
    [Fact]
    public void AppConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new AppConfig();

        // Assert
        Assert.NotNull(config.Server);
        Assert.Equal("0.0.0.0:8080", config.Server.Listen);
        Assert.Equal(30, config.Server.Timeout);
        Assert.Equal(1000, config.Server.MaxConnections);
        
        Assert.NotNull(config.Channels);
        Assert.Empty(config.Channels);
        
        Assert.NotNull(config.Rules);
        Assert.Empty(config.Rules);
        
        Assert.NotNull(config.Monitor);
        Assert.True(config.Monitor.Enable);
        Assert.Equal("0.0.0.0:9100", config.Monitor.PrometheusListen);
        
        Assert.NotNull(config.Security);
        Assert.NotNull(config.Security.Auth);
        Assert.NotNull(config.Security.Auth.Jwt);
        Assert.Equal("your-secret-key-here", config.Security.Auth.Jwt.Secret);
        Assert.Equal("SmartAIProxy", config.Security.Auth.Jwt.Issuer);
        Assert.Equal("SmartAIProxy-Client", config.Security.Auth.Jwt.Audience);
        Assert.Equal(60, config.Security.Auth.Jwt.ExpiryMinutes);
        
        Assert.NotNull(config.Security.Auth.ApiKeys);
        Assert.Empty(config.Security.Auth.ApiKeys);
        
        Assert.NotNull(config.Security.RateLimit);
        Assert.Equal(60, config.Security.RateLimit.RequestsPerMinute);
        Assert.Equal(10, config.Security.RateLimit.Burst);
    }

    [Fact]
    public void ServerConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var serverConfig = new ServerConfig();

        // Assert
        Assert.Equal("0.0.0.0:8080", serverConfig.Listen);
        Assert.Equal(30, serverConfig.Timeout);
        Assert.Equal(1000, serverConfig.MaxConnections);
    }

    [Fact]
    public void ChannelConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var channelConfig = new ChannelConfig();

        // Assert
        Assert.Equal(string.Empty, channelConfig.Name);
        Assert.Equal("openai", channelConfig.Type);
        Assert.Equal("https://api.openai.com/v1", channelConfig.Endpoint);
        Assert.Equal(string.Empty, channelConfig.ApiKey);
        Assert.Equal(0, channelConfig.PricePerToken);
        Assert.Equal(0, channelConfig.DailyLimit);
        Assert.Equal(0, channelConfig.Priority);
        Assert.Equal("active", channelConfig.Status);
        Assert.NotNull(channelConfig.ModelMapping);
        Assert.Empty(channelConfig.ModelMapping);
    }

    [Fact]
    public void RuleConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var ruleConfig = new RuleConfig();

        // Assert
        Assert.Equal(string.Empty, ruleConfig.Name);
        Assert.Equal(string.Empty, ruleConfig.Channel);
        Assert.Equal(string.Empty, ruleConfig.Expression);
        Assert.Equal(0, ruleConfig.Priority);
    }

    [Fact]
    public void MonitorConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var monitorConfig = new MonitorConfig();

        // Assert
        Assert.True(monitorConfig.Enable);
        Assert.Equal("0.0.0.0:9100", monitorConfig.PrometheusListen);
    }

    [Fact]
    public void SecurityConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var securityConfig = new SecurityConfig();

        // Assert
        Assert.NotNull(securityConfig.Auth);
        Assert.NotNull(securityConfig.RateLimit);
    }

    [Fact]
    public void AuthConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var authConfig = new AuthConfig();

        // Assert
        Assert.NotNull(authConfig.Jwt);
        Assert.NotNull(authConfig.ApiKeys);
        Assert.Empty(authConfig.ApiKeys);
    }

    [Fact]
    public void JwtConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var jwtConfig = new JwtConfig();

        // Assert
        Assert.Equal("your-secret-key-here", jwtConfig.Secret);
        Assert.Equal("SmartAIProxy", jwtConfig.Issuer);
        Assert.Equal("SmartAIProxy-Client", jwtConfig.Audience);
        Assert.Equal(60, jwtConfig.ExpiryMinutes);
    }

    [Fact]
    public void RateLimitConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var rateLimitConfig = new RateLimitConfig();

        // Assert
        Assert.Equal(60, rateLimitConfig.RequestsPerMinute);
        Assert.Equal(10, rateLimitConfig.Burst);
    }

    [Fact]
    public void AppConfig_CanBeInitializedWithCustomValues()
    {
        // Arrange & Act
        var config = new AppConfig
        {
            Server = new ServerConfig { Listen = "0.0.0.0:9090", Timeout = 60, MaxConnections = 2000 },
            Channels = new List<ChannelConfig>
            {
                new ChannelConfig
                {
                    Name = "Test Channel",
                    Type = "openai",
                    Endpoint = "https://api.openai.com/v1",
                    ApiKey = "test-key",
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
                    Name = "Test Rule",
                    Channel = "Test Channel",
                    Expression = "true",
                    Priority = 1
                }
            },
            Monitor = new MonitorConfig { Enable = false, PrometheusListen = "0.0.0.0:9200" },
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    Jwt = new JwtConfig
                    {
                        Secret = "custom-secret",
                        Issuer = "Custom-Issuer",
                        Audience = "Custom-Audience",
                        ExpiryMinutes = 120
                    },
                    ApiKeys = new Dictionary<string, string>
                    {
                        { "default", "custom-api-key" }
                    }
                },
                RateLimit = new RateLimitConfig
                {
                    RequestsPerMinute = 120,
                    Burst = 20
                }
            }
        };

        // Assert
        Assert.Equal("0.0.0.0:9090", config.Server.Listen);
        Assert.Equal(60, config.Server.Timeout);
        Assert.Equal(2000, config.Server.MaxConnections);
        
        Assert.Single(config.Channels);
        Assert.Equal("Test Channel", config.Channels[0].Name);
        
        Assert.Single(config.Rules);
        Assert.Equal("Test Rule", config.Rules[0].Name);
        
        Assert.False(config.Monitor.Enable);
        Assert.Equal("0.0.0.0:9200", config.Monitor.PrometheusListen);
        
        Assert.Equal("custom-secret", config.Security.Auth.Jwt.Secret);
        Assert.Equal("Custom-Issuer", config.Security.Auth.Jwt.Issuer);
        Assert.Equal("Custom-Audience", config.Security.Auth.Jwt.Audience);
        Assert.Equal(120, config.Security.Auth.Jwt.ExpiryMinutes);
        
        Assert.Single(config.Security.Auth.ApiKeys);
        Assert.Equal("custom-api-key", config.Security.Auth.ApiKeys["default"]);
        
        Assert.Equal(120, config.Security.RateLimit.RequestsPerMinute);
        Assert.Equal(20, config.Security.RateLimit.Burst);
    }

    [Fact]
    public void ChannelConfig_CanHaveModelMapping()
    {
        // Arrange & Act
        var channelConfig = new ChannelConfig
        {
            Name = "Test Channel",
            ModelMapping = new Dictionary<string, string>
            {
                { "gpt-3.5-turbo", "gpt-4" },
                { "gpt-4", "gpt-4-turbo" }
            }
        };

        // Assert
        Assert.Equal(2, channelConfig.ModelMapping.Count);
        Assert.Equal("gpt-4", channelConfig.ModelMapping["gpt-3.5-turbo"]);
        Assert.Equal("gpt-4-turbo", channelConfig.ModelMapping["gpt-4"]);
    }

    [Fact]
    public void AuthConfig_CanHaveMultipleApiKeys()
    {
        // Arrange & Act
        var authConfig = new AuthConfig
        {
            ApiKeys = new Dictionary<string, string>
            {
                { "default", "default-api-key" },
                { "admin", "admin-api-key" },
                { "user", "user-api-key" }
            }
        };

        // Assert
        Assert.Equal(3, authConfig.ApiKeys.Count);
        Assert.Equal("default-api-key", authConfig.ApiKeys["default"]);
        Assert.Equal("admin-api-key", authConfig.ApiKeys["admin"]);
        Assert.Equal("user-api-key", authConfig.ApiKeys["user"]);
    }

    [Fact]
    public void ChannelConfig_StatusProperty_AcceptsDifferentValues()
    {
        // Arrange & Act
        var channelConfig = new ChannelConfig { Status = "active" };
        
        // Assert
        Assert.Equal("active", channelConfig.Status);
        
        // Act
        channelConfig.Status = "inactive";
        
        // Assert
        Assert.Equal("inactive", channelConfig.Status);
        
        // Act
        channelConfig.Status = "maintenance";
        
        // Assert
        Assert.Equal("maintenance", channelConfig.Status);
    }

    [Fact]
    public void RuleConfig_PriorityProperty_AcceptsDifferentValues()
    {
        // Arrange & Act
        var ruleConfig = new RuleConfig { Priority = 1 };
        
        // Assert
        Assert.Equal(1, ruleConfig.Priority);
        
        // Act
        ruleConfig.Priority = 10;
        
        // Assert
        Assert.Equal(10, ruleConfig.Priority);
        
        // Act
        ruleConfig.Priority = 0;
        
        // Assert
        Assert.Equal(0, ruleConfig.Priority);
    }

    [Fact]
    public void JwtConfig_ExpiryMinutesProperty_AcceptsDifferentValues()
    {
        // Arrange & Act
        var jwtConfig = new JwtConfig { ExpiryMinutes = 30 };
        
        // Assert
        Assert.Equal(30, jwtConfig.ExpiryMinutes);
        
        // Act
        jwtConfig.ExpiryMinutes = 120;
        
        // Assert
        Assert.Equal(120, jwtConfig.ExpiryMinutes);
        
        // Act
        jwtConfig.ExpiryMinutes = 0;
        
        // Assert
        Assert.Equal(0, jwtConfig.ExpiryMinutes);
    }

    [Fact]
    public void RateLimitConfig_Properties_AcceptDifferentValues()
    {
        // Arrange & Act
        var rateLimitConfig = new RateLimitConfig { RequestsPerMinute = 30, Burst = 5 };
        
        // Assert
        Assert.Equal(30, rateLimitConfig.RequestsPerMinute);
        Assert.Equal(5, rateLimitConfig.Burst);
        
        // Act
        rateLimitConfig.RequestsPerMinute = 120;
        rateLimitConfig.Burst = 20;
        
        // Assert
        Assert.Equal(120, rateLimitConfig.RequestsPerMinute);
        Assert.Equal(20, rateLimitConfig.Burst);
        
        // Act
        rateLimitConfig.RequestsPerMinute = 0;
        rateLimitConfig.Burst = 0;
        
        // Assert
        Assert.Equal(0, rateLimitConfig.RequestsPerMinute);
        Assert.Equal(0, rateLimitConfig.Burst);
    }
}
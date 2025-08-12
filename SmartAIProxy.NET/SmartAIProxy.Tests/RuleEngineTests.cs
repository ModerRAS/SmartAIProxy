using SmartAIProxy.Core.Rules;
using SmartAIProxy.Models.Config;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace SmartAIProxy.Tests;

public class RuleEngineTests
{
    private readonly Mock<ILogger<RuleEngine>> _mockLogger;
    private readonly RuleEngine _ruleEngine;

    public RuleEngineTests()
    {
        _mockLogger = new Mock<ILogger<RuleEngine>>();
        _ruleEngine = new RuleEngine(_mockLogger.Object);
    }

    [Fact]
    public void EvaluateRules_WithMatchingRule_ReturnsCorrectChannel()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Test Rule",
                Channel = "Test Channel",
                Expression = "day_tokens_used < 1000",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Test Channel",
                Status = "active"
            }
        };

        var context = new Dictionary<string, object>
        {
            ["day_tokens_used"] = 500
        };

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Channel", result.Name);
    }

    [Fact]
    public void EvaluateRules_WithNonMatchingRule_ReturnsDefaultChannel()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Test Rule",
                Channel = "Test Channel",
                Expression = "day_tokens_used < 1000",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Test Channel",
                Status = "active",
                Priority = 2
            },
            new ChannelConfig
            {
                Name = "Default Channel",
                Status = "active",
                Priority = 1
            }
        };

        var context = new Dictionary<string, object>
        {
            ["day_tokens_used"] = 1500
        };

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default Channel", result.Name);
    }

    [Fact]
    public void EvaluateRules_WithInvalidExpression_ReturnsDefaultChannel()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Invalid Rule",
                Channel = "Test Channel",
                Expression = "invalid_expression",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Default Channel",
                Status = "active"
            }
        };

        var context = new Dictionary<string, object>();

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default Channel", result.Name);
    }

    [Fact]
    public void EvaluateRules_WithInactiveChannel_ReturnsDefaultActiveChannel()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Test Rule",
                Channel = "Inactive Channel",
                Expression = "true",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Inactive Channel",
                Status = "inactive"
            },
            new ChannelConfig
            {
                Name = "Default Active Channel",
                Status = "active",
                Priority = 2
            }
        };

        var context = new Dictionary<string, object>
        {
            ["day_tokens_used"] = 500
        };

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default Active Channel", result.Name);
        Assert.Equal("active", result.Status);
    }
    [Fact]
    public void EvaluateRules_WithNoActiveChannels_ReturnsNull()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Test Rule",
                Channel = "Test Channel",
                Expression = "true",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Inactive Channel",
                Status = "inactive"
            }
        };

        var context = new Dictionary<string, object>();

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EvaluateRules_WithMultipleMatchingRules_ReturnsHighestPriorityChannel()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Low Priority Rule",
                Channel = "Low Priority Channel",
                Expression = "true",
                Priority = 2
            },
            new RuleConfig
            {
                Name = "High Priority Rule",
                Channel = "High Priority Channel",
                Expression = "true",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "High Priority Channel",
                Status = "active"
            },
            new ChannelConfig
            {
                Name = "Low Priority Channel",
                Status = "active"
            }
        };

        var context = new Dictionary<string, object>();

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("High Priority Channel", result.Name);
    }

    [Fact]
    public void EvaluateRules_WithComplexExpression_EvaluatesCorrectly()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Complex Rule",
                Channel = "Complex Channel",
                Expression = "day_tokens_used < daily_limit AND time_of_day >= '00:00' AND time_of_day <= '06:00'",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Complex Channel",
                Status = "active"
            }
        };

        var context = new Dictionary<string, object>
        {
            ["day_tokens_used"] = 500,
            ["daily_limit"] = 1000,
            ["time_of_day"] = "02:00"
        };

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Complex Channel", result.Name);
    }

    [Fact]
    public void EvaluateRules_WithNoRules_ReturnsDefaultChannel()
    {
        // Arrange
        var rules = new List<RuleConfig>();

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Default Channel",
                Status = "active",
                Priority = 1
            },
            new ChannelConfig
            {
                Name = "Other Channel",
                Status = "active",
                Priority = 2
            }
        };

        var context = new Dictionary<string, object>();

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default Channel", result.Name);
    }

    [Fact]
    public void EvaluateRules_WithNoMatchingRulesAndNoActiveChannels_ReturnsNull()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Non-matching Rule",
                Channel = "Non-matching Channel",
                Expression = "false",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Inactive Channel",
                Status = "inactive"
            }
        };

        var context = new Dictionary<string, object>();

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EvaluateRules_WithNullContext_HandlesGracefully()
    {
        // Arrange
        var rules = new List<RuleConfig>
        {
            new RuleConfig
            {
                Name = "Test Rule",
                Channel = "Test Channel",
                Expression = "true",
                Priority = 1
            }
        };

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Test Channel",
                Status = "active"
            }
        };

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Channel", result.Name);
    }

    [Fact]
    public void EvaluateRules_WithEmptyRulesList_ReturnsDefaultChannel()
    {
        // Arrange
        var rules = new List<RuleConfig>();

        var channels = new List<ChannelConfig>
        {
            new ChannelConfig
            {
                Name = "Default Channel",
                Status = "active",
                Priority = 1
            }
        };

        var context = new Dictionary<string, object>();

        // Act
        var result = _ruleEngine.EvaluateRules(rules, channels, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default Channel", result.Name);
    }
}
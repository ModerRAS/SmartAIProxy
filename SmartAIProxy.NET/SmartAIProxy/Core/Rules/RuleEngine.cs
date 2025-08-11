using NCalc;
using SmartAIProxy.Models.Config;

namespace SmartAIProxy.Core.Rules;

public interface IRuleEngine
{
    ChannelConfig? EvaluateRules(List<RuleConfig> rules, List<ChannelConfig> channels, Dictionary<string, object> context);
}

public class RuleEngine : IRuleEngine
{
    private readonly ILogger<RuleEngine> _logger;

    public RuleEngine(ILogger<RuleEngine> logger)
    {
        _logger = logger;
    }

    public ChannelConfig? EvaluateRules(List<RuleConfig> rules, List<ChannelConfig> channels, Dictionary<string, object> context)
    {
        // Sort rules by priority (lower number = higher priority)
        var sortedRules = rules.OrderBy(r => r.Priority).ToList();

        foreach (var rule in sortedRules)
        {
            try
            {
                var expression = new Expression(rule.Expression);

                // Register parameters from context
                foreach (var param in context)
                {
                    expression.Parameters[param.Key] = param.Value;
                }

                // Evaluate the expression
                var result = expression.Evaluate();

                if (result is bool boolResult && boolResult)
                {
                    // Find the channel for this rule
                    var channel = channels.FirstOrDefault(c => c.Name == rule.Channel && c.Status == "active");
                    if (channel != null)
                    {
                        _logger.LogInformation("Rule '{RuleName}' matched, selecting channel '{ChannelName}'", rule.Name, channel.Name);
                        return channel;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rule '{RuleName}': {Expression}", rule.Name, rule.Expression);
            }
        }

        // If no rules match, return the highest priority active channel
        var activeChannels = channels.Where(c => c.Status == "active").OrderBy(c => c.Priority).ToList();
        if (activeChannels.Any())
        {
            _logger.LogInformation("No rules matched, selecting default channel '{ChannelName}'", activeChannels.First().Name);
            return activeChannels.First();
        }

        _logger.LogWarning("No active channels available for routing");
        return null;
    }
}
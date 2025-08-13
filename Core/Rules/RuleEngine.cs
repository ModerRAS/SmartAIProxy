using NCalc;
using SmartAIProxy.Models.Config;

namespace SmartAIProxy.Core.Rules;

/// <summary>
/// 规则引擎接口，提供基于表达式的路由规则评估功能
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// 评估路由规则并选择合适的AI服务通道
    /// </summary>
    /// <param name="rules">路由规则列表</param>
    /// <param name="channels">AI服务通道列表</param>
    /// <param name="context">评估上下文，包含变量和参数</param>
    /// <returns>选中的通道配置，如果没有合适的通道则返回null</returns>
    ChannelConfig? EvaluateRules(List<RuleConfig> rules, List<ChannelConfig> channels, Dictionary<string, object> context);
}

/// <summary>
/// 规则引擎实现类，负责评估路由规则并选择合适的AI服务通道
/// 使用NCalc表达式引擎进行规则评估
/// </summary>
public class RuleEngine : IRuleEngine
{
    private readonly ILogger<RuleEngine> _logger;

    /// <summary>
    /// 规则引擎构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public RuleEngine(ILogger<RuleEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 评估路由规则并选择合适的AI服务通道
    /// </summary>
    /// <param name="rules">路由规则列表</param>
    /// <param name="channels">AI服务通道列表</param>
    /// <param name="context">评估上下文，包含变量和参数</param>
    /// <returns>选中的通道配置，如果没有合适的通道则返回null</returns>
    public ChannelConfig? EvaluateRules(List<RuleConfig> rules, List<ChannelConfig> channels, Dictionary<string, object> context)
    {
        // 按优先级排序规则（数字越小优先级越高）
        var sortedRules = rules.OrderBy(r => r.Priority).ToList();

        foreach (var rule in sortedRules)
        {
            try
            {
                // 创建表达式对象
                var expression = new Expression(rule.Expression);

                // 从上下文注册参数
                foreach (var param in context)
                {
                    expression.Parameters[param.Key] = param.Value;
                }

                // 评估表达式
                var result = expression.Evaluate();

                // 如果表达式结果为true，则选择对应的通道
                if (result is bool boolResult && boolResult)
                {
                    // 查找此规则对应的活跃通道
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

        // 如果没有规则匹配，返回优先级最高的活跃通道
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
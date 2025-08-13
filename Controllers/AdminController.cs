using Microsoft.AspNetCore.Mvc;
using SmartAIProxy.Core.Channels;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Models.Config;
using SmartAIProxy.Models.DTO;
using System.Diagnostics;

namespace SmartAIProxy.Controllers;

/// <summary>
/// 管理员控制器，提供系统管理API端点
/// 用于管理AI服务通道、路由规则和系统配置
/// </summary>
[ApiController]
[Route("api")]
public class AdminController : ControllerBase
{
    private readonly IConfigurationService _configService;
    private readonly IChannelService _channelService;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// 管理员控制器构造函数
    /// </summary>
    /// <param name="configService">配置服务</param>
    /// <param name="channelService">通道服务</param>
    /// <param name="logger">日志记录器</param>
    public AdminController(IConfigurationService configService, IChannelService channelService, ILogger<AdminController> logger)
    {
        _configService = configService;
        _channelService = channelService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有配置的AI服务通道
    /// </summary>
    /// <returns>包含所有通道信息的API响应</returns>
    [HttpGet("channels")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public IActionResult GetChannels()
    {
        try
        {
            // 获取所有通道
            var channels = _channelService.GetChannels();
            var responseChannels = channels.Select(c => new ChannelResponse
            {
                Name = c.Name,
                Type = c.Type,
                Endpoint = c.Endpoint,
                PricePerToken = c.PricePerToken,
                DailyLimit = c.DailyLimit,
                Priority = c.Priority,
                Status = c.Status
            }).ToList();

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Channels retrieved successfully",
                Data = responseChannels
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving channels");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error retrieving channels"
            });
        }
    }

    /// <summary>
    /// 添加或更新AI服务通道配置
    /// </summary>
    /// <param name="channel">通道配置信息</param>
    /// <returns>操作结果的API响应</returns>
    [HttpPost("channels")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public IActionResult AddOrUpdateChannel([FromBody] ChannelConfig channel)
    {
        try
        {
            // 验证通道名称
            if (string.IsNullOrEmpty(channel.Name))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Channel name is required"
                });
            }

            // 添加或更新通道
            _channelService.AddOrUpdateChannel(channel);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Channel updated successfully",
                Data = new ChannelResponse
                {
                    Name = channel.Name,
                    Type = channel.Type,
                    Endpoint = channel.Endpoint,
                    PricePerToken = channel.PricePerToken,
                    DailyLimit = channel.DailyLimit,
                    Priority = channel.Priority,
                    Status = channel.Status
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating channel");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error updating channel"
            });
        }
    }

    /// <summary>
    /// 获取所有配置的路由规则
    /// </summary>
    /// <returns>包含所有规则信息的API响应</returns>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public IActionResult GetRules()
    {
        try
        {
            // 获取配置和规则
            var config = _configService.GetConfig();
            var rules = config.Rules.Select(r => new RuleResponse
            {
                Name = r.Name,
                Channel = r.Channel,
                Expression = r.Expression,
                Priority = r.Priority
            }).ToList();

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Rules retrieved successfully",
                Data = rules
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rules");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error retrieving rules"
            });
        }
    }

    /// <summary>
    /// 添加或更新路由规则
    /// </summary>
    /// <param name="rule">规则配置信息</param>
    /// <returns>操作结果的API响应</returns>
    [HttpPost("rules")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public IActionResult AddOrUpdateRule([FromBody] RuleConfig rule)
    {
        try
        {
            // 验证规则名称
            if (string.IsNullOrEmpty(rule.Name))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Rule name is required"
                });
            }

            // 获取当前配置
            var config = _configService.GetConfig();
            var existingRule = config.Rules.FirstOrDefault(r => r.Name == rule.Name);

            if (existingRule != null)
            {
                // 更新现有规则
                var index = config.Rules.IndexOf(existingRule);
                config.Rules[index] = rule;
            }
            else
            {
                // 添加新规则
                config.Rules.Add(rule);
            }

            // 保存更新后的配置
            _configService.UpdateConfig(config);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Rule updated successfully",
                Data = new RuleResponse
                {
                    Name = rule.Name,
                    Channel = rule.Channel,
                    Expression = rule.Expression,
                    Priority = rule.Priority
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error updating rule"
            });
        }
    }

    /// <summary>
    /// 获取当前系统配置
    /// </summary>
    /// <returns>包含完整配置信息的API响应</returns>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public IActionResult GetConfig()
    {
        try
        {
            // 获取当前配置
            var config = _configService.GetConfig();
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Configuration retrieved successfully",
                Data = config
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error retrieving configuration"
            });
        }
    }

    /// <summary>
    /// 获取系统健康状态信息
    /// </summary>
    /// <returns>包含健康状态信息的API响应</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public IActionResult GetHealth()
    {
        try
        {
            // 计算系统运行时间
            var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Admin API is running",
                Data = new HealthResponse
                {
                    Status = "healthy",
                    Uptime = $"{uptime.Hours}h{uptime.Minutes}m",
                    Version = "1.0.0"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error checking health"
            });
        }
    }
}
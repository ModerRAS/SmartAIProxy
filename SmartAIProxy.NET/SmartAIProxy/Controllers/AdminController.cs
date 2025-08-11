using Microsoft.AspNetCore.Mvc;
using SmartAIProxy.Core.Channels;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Models.Config;
using SmartAIProxy.Models.DTO;
using System.Diagnostics;

namespace SmartAIProxy.Controllers;

[ApiController]
[Route("api")]
public class AdminController : ControllerBase
{
    private readonly IConfigurationService _configService;
    private readonly IChannelService _channelService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IConfigurationService configService, IChannelService channelService, ILogger<AdminController> logger)
    {
        _configService = configService;
        _channelService = channelService;
        _logger = logger;
    }

    [HttpGet("channels")]
    public IActionResult GetChannels()
    {
        try
        {
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

    [HttpPost("channels")]
    public IActionResult AddOrUpdateChannel([FromBody] ChannelConfig channel)
    {
        try
        {
            if (string.IsNullOrEmpty(channel.Name))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Channel name is required"
                });
            }

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

    [HttpGet("rules")]
    public IActionResult GetRules()
    {
        try
        {
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

    [HttpPost("rules")]
    public IActionResult AddOrUpdateRule([FromBody] RuleConfig rule)
    {
        try
        {
            if (string.IsNullOrEmpty(rule.Name))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Rule name is required"
                });
            }

            var config = _configService.GetConfig();
            var existingRule = config.Rules.FirstOrDefault(r => r.Name == rule.Name);

            if (existingRule != null)
            {
                // Update existing rule
                var index = config.Rules.IndexOf(existingRule);
                config.Rules[index] = rule;
            }
            else
            {
                // Add new rule
                config.Rules.Add(rule);
            }

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

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        try
        {
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

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        try
        {
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
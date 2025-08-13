using Microsoft.AspNetCore.Mvc;
using SmartAIProxy.Models.DTO;

namespace SmartAIProxy.Controllers;

/// <summary>
/// 健康检查控制器，提供系统健康状态检查端点
/// 用于监控系统运行状态
/// </summary>
[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 健康检查端点，用于验证系统是否正常运行
    /// </summary>
    /// <returns>包含健康状态的对象</returns>
    [HttpGet("healthz")]
    [ProducesResponseType(200)]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "ok" });
    }
}
using Microsoft.AspNetCore.Mvc;
using SmartAIProxy.Models.DTO;

namespace SmartAIProxy.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet("healthz")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "ok" });
    }
}
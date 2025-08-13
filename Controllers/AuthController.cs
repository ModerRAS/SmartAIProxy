using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Models.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartAIProxy.Controllers;

/// <summary>
/// 身份验证控制器，提供用户登录和JWT令牌生成功能
/// 用于管理员API的身份验证
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// 身份验证控制器构造函数
    /// </summary>
    /// <param name="configService">配置服务</param>
    /// <param name="logger">日志记录器</param>
    public AuthController(IConfigurationService configService, ILogger<AuthController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录接口，验证用户凭据并生成JWT令牌
    /// </summary>
    /// <param name="loginRequest">登录请求信息</param>
    /// <returns>包含访问令牌的登录响应</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            // 在实际实现中，您应该根据用户存储验证凭据
            // 在此示例中，我们使用简单的检查
            if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Username and password are required"
                });
            }

            // 简单验证 - 在生产环境中，请使用适当的身份验证机制
            if (loginRequest.Username != "admin" || loginRequest.Password != "admin123")
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "Invalid credentials"
                });
            }

            // 生成JWT令牌
            var token = GenerateJwtToken(loginRequest.Username);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Login successful",
                Data = new LoginResponse
                {
                    AccessToken = token,
                    TokenType = "Bearer",
                    ExpiresIn = _configService.GetConfig().Security.Auth.Jwt.ExpiryMinutes * 60
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error during login"
            });
        }
    }

    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>JWT令牌字符串</returns>
    private string GenerateJwtToken(string username)
    {
        // 获取JWT配置
        var config = _configService.GetConfig().Security.Auth.Jwt;
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(config.Secret);
        
        // 创建令牌描述符
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "admin")
            }),
            Expires = DateTime.UtcNow.AddMinutes(config.ExpiryMinutes),
            Issuer = config.Issuer,
            Audience = config.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        // 创建令牌
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
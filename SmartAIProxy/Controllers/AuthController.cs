using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Models.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartAIProxy.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfigurationService _configService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfigurationService configService, ILogger<AuthController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            // In a real implementation, you would validate credentials against a user store
            // For this example, we'll use a simple check
            if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Username and password are required"
                });
            }

            // Simple validation - in production, use proper authentication
            if (loginRequest.Username != "admin" || loginRequest.Password != "admin123")
            {
                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "Invalid credentials"
                });
            }

            // Generate JWT token
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

    private string GenerateJwtToken(string username)
    {
        var config = _configService.GetConfig().Security.Auth.Jwt;
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(config.Secret);
        
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
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartAIProxy.Controllers;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Models.Config;
using SmartAIProxy.Models.DTO;
using Xunit;

namespace SmartAIProxy.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockConfigService = new Mock<IConfigurationService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(
            _mockConfigService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Login_ReturnsBadRequest_WhenUsernameOrPasswordIsEmpty()
    {
        // Arrange
        var loginRequest = new LoginRequest { Username = "", Password = "password" };

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Username and password are required", response.Message);
    }

    [Fact]
    public void Login_ReturnsBadRequest_WhenPasswordIsEmpty()
    {
        // Arrange
        var loginRequest = new LoginRequest { Username = "admin", Password = "" };

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Username and password are required", response.Message);
    }

    [Fact]
    public void Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        // Arrange
        var loginRequest = new LoginRequest { Username = "admin", Password = "wrongpassword" };

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid credentials", response.Message);
    }

    [Fact]
    public void Login_ReturnsOkResult_WhenCredentialsAreValid()
    {
        // Arrange
        var loginRequest = new LoginRequest { Username = "admin", Password = "admin123" };
        
        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    Jwt = new JwtConfig
                    {
                        Secret = "test-secret-key-that-is-at-least-32-characters-long",
                        Issuer = "SmartAIProxy",
                        Audience = "SmartAIProxy-Client",
                        ExpiryMinutes = 60
                    }
                }
            }
        };

        _mockConfigService.Setup(service => service.GetConfig())
            .Returns(config);

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Login successful", response.Message);
        
        var loginResponse = Assert.IsType<LoginResponse>(response.Data);
        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.Equal("Bearer", loginResponse.TokenType);
        Assert.Equal(60 * 60, loginResponse.ExpiresIn);
    }

    [Fact]
    public void Login_ReturnsServerError_WhenExceptionThrown()
    {
        // Arrange
        var loginRequest = new LoginRequest { Username = "admin", Password = "admin123" };
        _mockConfigService.Setup(service => service.GetConfig())
            .Throws(new Exception("Test exception"));

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Error during login", response.Message);
    }

    [Fact]
    public void GenerateJwtToken_ReturnsValidToken()
    {
        // Arrange
        var username = "admin";
        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    Jwt = new JwtConfig
                    {
                        Secret = "test-secret-key-that-is-at-least-32-characters-long",
                        Issuer = "SmartAIProxy",
                        Audience = "SmartAIProxy-Client",
                        ExpiryMinutes = 60
                    }
                }
            }
        };

        _mockConfigService.Setup(service => service.GetConfig())
            .Returns(config);

        // Use reflection to access the private method
        var methodInfo = typeof(AuthController).GetMethod("GenerateJwtToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var token = (string)methodInfo.Invoke(_controller, new object[] { username });

        // Assert
        Assert.NotNull(token);

        // Validate the token
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "SmartAIProxy",
            ValidateAudience = true,
            ValidAudience = "SmartAIProxy-Client",
            ValidateLifetime = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes("test-secret-key-that-is-at-least-32-characters-long"))
        };

        var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        var jwtToken = (JwtSecurityToken)validatedToken;

        Assert.Equal("SmartAIProxy", jwtToken.Issuer);
        Assert.Equal("SmartAIProxy-Client", jwtToken.Audiences.First());
        Assert.Equal(username, principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("admin", principal.FindFirstValue(ClaimTypes.Role));
    }

    [Fact]
    public void Login_ReturnsOkResult_WithDifferentExpiryTime()
    {
        // Arrange
        var loginRequest = new LoginRequest { Username = "admin", Password = "admin123" };
        
        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    Jwt = new JwtConfig
                    {
                        Secret = "test-secret-key-that-is-at-least-32-characters-long",
                        Issuer = "SmartAIProxy",
                        Audience = "SmartAIProxy-Client",
                        ExpiryMinutes = 120 // Different expiry time
                    }
                }
            }
        };

        _mockConfigService.Setup(service => service.GetConfig())
            .Returns(config);

        // Act
        var result = _controller.Login(loginRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);
        Assert.True(response.Success);
        
        var loginResponse = Assert.IsType<LoginResponse>(response.Data);
        Assert.Equal(120 * 60, loginResponse.ExpiresIn); // Should be 120 minutes in seconds
    }
}
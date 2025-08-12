using Microsoft.AspNetCore.Mvc;
using SmartAIProxy.Controllers;
using Xunit;

namespace SmartAIProxy.Tests.Controllers;

public class HealthControllerTests
{
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _controller = new HealthController();
    }

    [Fact]
    public void HealthCheck_ReturnsOkResult_WithStatusOk()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<object>(okResult.Value);
        
        // Use reflection to check the status property
        var statusProperty = response.GetType().GetProperty("status");
        Assert.NotNull(statusProperty);
        Assert.Equal("ok", statusProperty.GetValue(response));
    }

    [Fact]
    public void HealthCheck_ReturnsCorrectContentType()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        // The default content type for object results in ASP.NET Core is application/json
        Assert.Equal("application/json", okResult.ContentTypes.First());
    }

    [Fact]
    public void HealthCheck_ReturnsStatusCode200()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void HealthCheck_ReturnsNonNullableResult()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
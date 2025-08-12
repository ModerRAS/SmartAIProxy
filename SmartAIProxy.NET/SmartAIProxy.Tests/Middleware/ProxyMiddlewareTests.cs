using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SmartAIProxy.Core.Channels;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Core.Rules;
using SmartAIProxy.Middleware;
using SmartAIProxy.Models.Config;
using SmartAIProxy.Models.DTO;
using Xunit;

namespace SmartAIProxy.Tests.Middleware;

public class ProxyMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNextDelegate;
    private readonly Mock<ILogger<ProxyMiddleware>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<IRuleEngine> _mockRuleEngine;
    private readonly Mock<IChannelService> _mockChannelService;
    private readonly ProxyMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public ProxyMiddlewareTests()
    {
        _mockNextDelegate = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<ProxyMiddleware>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfigService = new Mock<IConfigurationService>();
        _mockRuleEngine = new Mock<IRuleEngine>();
        _mockChannelService = new Mock<IChannelService>();

        _middleware = new ProxyMiddleware(
            _mockNextDelegate.Object,
            _mockLogger.Object,
            _mockHttpClientFactory.Object,
            _mockConfigService.Object,
            _mockRuleEngine.Object,
            _mockChannelService.Object);

        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_CallsNextDelegate_WhenPathDoesNotStartWithV1()
    {
        // Arrange
        _httpContext.Request.Path = "/api/test";

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/chat/completions";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("");

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            }
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(401, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenApiKeyIsInvalid()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/chat/completions";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("Bearer invalid-key");

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            }
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(401, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsBadRequest_WhenContentTypeIsNotJson()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/chat/completions";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("Bearer test-api-key");
        _httpContext.Request.ContentType = "text/plain";

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            }
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(400, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsServiceUnavailable_WhenNoChannelIsAvailable()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/chat/completions";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("Bearer test-api-key");
        _httpContext.Request.ContentType = "application/json";

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            }
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);
        _mockRuleEngine.Setup(engine => engine.EvaluateRules(It.IsAny<List<RuleConfig>>(), It.IsAny<List<ChannelConfig>>(), It.IsAny<Dictionary<string, object>>()))
            .Returns((ChannelConfig?)null);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(503, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ForwardsRequest_WhenAllConditionsAreMet()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/chat/completions";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("Bearer test-api-key");
        _httpContext.Request.ContentType = "application/json";

        var requestBody = "{\"model\": \"gpt-3.5-turbo\", \"messages\": [{\"role\": \"user\", \"content\": \"Hello\"}]}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        _httpContext.Request.Body = stream;

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            },
            Server = new ServerConfig { Timeout = 30 }
        };

        var channel = new ChannelConfig
        {
            Name = "Test Channel",
            Endpoint = "https://api.openai.com/v1",
            ApiKey = "channel-api-key",
            Status = "active"
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);
        _mockRuleEngine.Setup(engine => engine.EvaluateRules(It.IsAny<List<RuleConfig>>(), It.IsAny<List<ChannelConfig>>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(channel);

        // Mock HttpClient
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\": \"chatcmpl-123\", \"object\": \"chat.completion\", \"created\": 1677652288, \"model\": \"gpt-3.5-turbo\", \"choices\": [{\"index\": 0, \"message\": {\"role\": \"assistant\", \"content\": \"Hello! How can I help you today?\"}, \"finish_reason\": \"stop\"}], \"usage\": {\"prompt_tokens\": 12, \"completion_tokens\": 10, \"total_tokens\": 22}}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(factory => factory.CreateClient()).Returns(httpClient);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(200, _httpContext.Response.StatusCode);
        _mockChannelService.Verify(service => service.UpdateChannelUsage("Test Channel", It.IsAny<int>()), Times.Once);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_HandlesException_WhenForwardingRequest()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/chat/completions";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("Bearer test-api-key");
        _httpContext.Request.ContentType = "application/json";

        var requestBody = "{\"model\": \"gpt-3.5-turbo\", \"messages\": [{\"role\": \"user\", \"content\": \"Hello\"}]}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        _httpContext.Request.Body = stream;

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            },
            Server = new ServerConfig { Timeout = 30 }
        };

        var channel = new ChannelConfig
        {
            Name = "Test Channel",
            Endpoint = "https://api.openai.com/v1",
            ApiKey = "channel-api-key",
            Status = "active"
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);
        _mockRuleEngine.Setup(engine => engine.EvaluateRules(It.IsAny<List<RuleConfig>>(), It.IsAny<List<ChannelConfig>>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(channel);

        // Mock HttpClient to throw an exception
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(factory => factory.CreateClient()).Returns(httpClient);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(502, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_HandlesGetRequest()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/models";
        _httpContext.Request.Method = "GET";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("Bearer test-api-key");

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            },
            Server = new ServerConfig { Timeout = 30 }
        };

        var channel = new ChannelConfig
        {
            Name = "Test Channel",
            Endpoint = "https://api.openai.com/v1",
            ApiKey = "channel-api-key",
            Status = "active"
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);
        _mockRuleEngine.Setup(engine => engine.EvaluateRules(It.IsAny<List<RuleConfig>>(), It.IsAny<List<ChannelConfig>>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(channel);

        // Mock HttpClient
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"object\": \"list\", \"data\": [{\"id\": \"gpt-3.5-turbo\", \"object\": \"model\"}]}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(factory => factory.CreateClient()).Returns(httpClient);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(200, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_EstimatesTokensCorrectly()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/chat/completions";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("Bearer test-api-key");
        _httpContext.Request.ContentType = "application/json";

        var requestBody = "{\"model\": \"gpt-3.5-turbo\", \"messages\": [{\"role\": \"user\", \"content\": \"Hello world\"}]}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        _httpContext.Request.Body = stream;

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            },
            Server = new ServerConfig { Timeout = 30 }
        };

        var channel = new ChannelConfig
        {
            Name = "Test Channel",
            Endpoint = "https://api.openai.com/v1",
            ApiKey = "channel-api-key",
            Status = "active"
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);
        _mockRuleEngine.Setup(engine => engine.EvaluateRules(It.IsAny<List<RuleConfig>>(), It.IsAny<List<ChannelConfig>>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(channel);

        // Mock HttpClient
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\": \"chatcmpl-123\", \"object\": \"chat.completion\", \"created\": 1677652288, \"model\": \"gpt-3.5-turbo\", \"choices\": [{\"index\": 0, \"message\": {\"role\": \"assistant\", \"content\": \"Hello! How can I help you today?\"}, \"finish_reason\": \"stop\"}], \"usage\": {\"prompt_tokens\": 12, \"completion_tokens\": 10, \"total_tokens\": 22}}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(factory => factory.CreateClient()).Returns(httpClient);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        // The response content has multiple words, so the token estimation should be greater than 0
        _mockChannelService.Verify(service => service.UpdateChannelUsage("Test Channel", It.Is<int>(tokens => tokens > 0)), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_BuildsTargetUrlCorrectly()
    {
        // Arrange
        _httpContext.Request.Path = "/v1/chat/completions";
        _httpContext.Request.Method = "POST";
        _httpContext.Request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues("Bearer test-api-key");
        _httpContext.Request.ContentType = "application/json";

        var requestBody = "{\"model\": \"gpt-3.5-turbo\", \"messages\": [{\"role\": \"user\", \"content\": \"Hello\"}]}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        _httpContext.Request.Body = stream;

        var config = new AppConfig
        {
            Security = new SecurityConfig
            {
                Auth = new AuthConfig
                {
                    ApiKeys = new Dictionary<string, string> { { "default", "test-api-key" } }
                }
            },
            Server = new ServerConfig { Timeout = 30 }
        };

        var channel = new ChannelConfig
        {
            Name = "Test Channel",
            Endpoint = "https://api.openai.com/v1",
            ApiKey = "channel-api-key",
            Status = "active",
            ModelMapping = new Dictionary<string, string> { { "gpt-3.5-turbo", "gpt-4" } }
        };

        _mockConfigService.Setup(service => service.GetConfig()).Returns(config);
        _mockRuleEngine.Setup(engine => engine.EvaluateRules(It.IsAny<List<RuleConfig>>(), It.IsAny<List<ChannelConfig>>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(channel);

        // Mock HttpClient to capture the request URL
        HttpRequestMessage capturedRequest = null;
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\": \"chatcmpl-123\"}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(factory => factory.CreateClient()).Returns(httpClient);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://api.openai.com/v1/chat/completions", capturedRequest.RequestUri.ToString());
    }
}
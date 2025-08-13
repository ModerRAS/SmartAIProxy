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

/// <summary>
/// 代理中间件测试类
/// 用于验证代理中间件的各种功能，包括请求认证、通道选择、请求转发和错误处理
/// </summary>
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

    /// <summary>
    /// 代理中间件测试构造函数
    /// 设置测试所需的模拟对象和HTTP上下文
    /// </summary>
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

    /// <summary>
    /// 测试InvokeAsync方法在路径不以/v1开头时是否调用下一个委托
    /// </summary>
    [Fact]
    public async Task InvokeAsync_CallsNextDelegate_WhenPathDoesNotStartWithV1()
    {
        // 准备
        _httpContext.Request.Path = "/api/test";

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Once);
    }

    /// <summary>
    /// 测试InvokeAsync方法在API密钥缺失时是否返回未授权状态
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenApiKeyIsMissing()
    {
        // 准备
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

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        Assert.Equal(401, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    /// <summary>
    /// 测试InvokeAsync方法在API密钥无效时是否返回未授权状态
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenApiKeyIsInvalid()
    {
        // 准备
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

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        Assert.Equal(401, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    /// <summary>
    /// 测试InvokeAsync方法在内容类型不是JSON时是否返回错误请求状态
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ReturnsBadRequest_WhenContentTypeIsNotJson()
    {
        // 准备
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

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        Assert.Equal(400, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    /// <summary>
    /// 测试InvokeAsync方法在没有可用通道时是否返回服务不可用状态
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ReturnsServiceUnavailable_WhenNoChannelIsAvailable()
    {
        // 准备
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

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        Assert.Equal(503, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    /// <summary>
    /// 测试InvokeAsync方法在所有条件满足时是否转发请求
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ForwardsRequest_WhenAllConditionsAreMet()
    {
        // 准备
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

        // 模拟HttpClient
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

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        Assert.Equal(200, _httpContext.Response.StatusCode);
        _mockChannelService.Verify(service => service.UpdateChannelUsage("Test Channel", It.IsAny<int>()), Times.Once);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    /// <summary>
    /// 测试InvokeAsync方法在转发请求时是否处理异常
    /// </summary>
    [Fact]
    public async Task InvokeAsync_HandlesException_WhenForwardingRequest()
    {
        // 准备
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

        // 模拟HttpClient抛出异常
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(factory => factory.CreateClient()).Returns(httpClient);

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        Assert.Equal(502, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    /// <summary>
    /// 测试InvokeAsync方法是否处理GET请求
    /// </summary>
    [Fact]
    public async Task InvokeAsync_HandlesGetRequest()
    {
        // 准备
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

        // 模拟HttpClient
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

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        Assert.Equal(200, _httpContext.Response.StatusCode);
        _mockNextDelegate.Verify(next => next(_httpContext), Times.Never);
    }

    /// <summary>
    /// 测试InvokeAsync方法是否正确估算令牌数
    /// </summary>
    [Fact]
    public async Task InvokeAsync_EstimatesTokensCorrectly()
    {
        // 准备
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

        // 模拟HttpClient
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

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        // 响应内容包含多个单词，因此令牌估算应该大于0
        _mockChannelService.Verify(service => service.UpdateChannelUsage("Test Channel", It.Is<int>(tokens => tokens > 0)), Times.Once);
    }

    /// <summary>
    /// 测试InvokeAsync方法是否正确构建目标URL
    /// </summary>
    [Fact]
    public async Task InvokeAsync_BuildsTargetUrlCorrectly()
    {
        // 准备
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

        // 模拟HttpClient以捕获请求URL
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

        // 执行
        await _middleware.InvokeAsync(_httpContext);

        // 断言
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://api.openai.com/v1/chat/completions", capturedRequest.RequestUri.ToString());
    }
}
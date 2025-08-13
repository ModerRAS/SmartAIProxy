using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using SmartAIProxy.Core.Channels;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Core.Rules;
using SmartAIProxy.Models.DTO;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace SmartAIProxy.Middleware;

/// <summary>
/// 代理中间件类，负责处理AI API请求的转发
/// 实现请求认证、通道选择、请求转发和响应处理等功能
/// </summary>
public class ProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProxyMiddleware> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfigurationService _configService;
    private readonly IRuleEngine _ruleEngine;
    private readonly IChannelService _channelService;

    /// <summary>
    /// 代理中间件构造函数
    /// </summary>
    /// <param name="next">下一个请求委托</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    /// <param name="configService">配置服务</param>
    /// <param name="ruleEngine">规则引擎</param>
    /// <param name="channelService">通道服务</param>
    public ProxyMiddleware(
        RequestDelegate next,
        ILogger<ProxyMiddleware> logger,
        IHttpClientFactory httpClientFactory,
        IConfigurationService configService,
        IRuleEngine ruleEngine,
        IChannelService channelService)
    {
        _next = next;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configService = configService;
        _ruleEngine = ruleEngine;
        _channelService = channelService;
    }

    /// <summary>
    /// 中间件主方法，处理传入的HTTP请求
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // 只处理/v1/路径下的API请求
        if (!context.Request.Path.StartsWithSegments("/v1"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // 请求认证
            if (!await AuthenticateRequest(context))
            {
                return;
            }

            // 应用速率限制
            if (!await ApplyRateLimiting(context))
            {
                return;
            }

            // 预处理请求
            var requestBody = await PreprocessRequest(context);
            if (requestBody == null)
            {
                return;
            }

            // 基于规则选择通道
            var channel = await SelectChannel(context, requestBody);
            if (channel == null)
            {
                await ReturnErrorResponse(context, "no_channel_available", "No available channels for routing", 503);
                return;
            }

            // 将请求转发到选定的通道
            await ForwardRequest(context, channel, requestBody, stopwatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing proxy request");
            await ReturnErrorResponse(context, "internal_error", "Internal server error", 500);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// 验证请求的API密钥
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>如果验证成功返回true，否则返回false</returns>
    private async Task<bool> AuthenticateRequest(HttpContext context)
    {
        var config = _configService.GetConfig();
        
        // 从Authorization头中提取API密钥
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader))
        {
            await ReturnErrorResponse(context, "missing_api_key", "Missing API key", 401);
            return false;
        }

        var apiKey = authHeader.Replace("Bearer ", "");
        if (!config.Security.Auth.ApiKeys.ContainsValue(apiKey))
        {
            await ReturnErrorResponse(context, "invalid_api_key", "Invalid API key", 401);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 应用速率限制
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>如果请求未被限制返回true，否则返回false</returns>
    private async Task<bool> ApplyRateLimiting(HttpContext context)
    {
        // 简单的速率限制实现
        // 在生产环境中，您应该使用更复杂的速率限制解决方案
        await Task.CompletedTask;
        return true;
    }

    /// <summary>
    /// 预处理请求，提取请求体内容
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>请求体内容，如果处理失败则返回null</returns>
    private async Task<string?> PreprocessRequest(HttpContext context)
    {
        // 只处理带有JSON体的POST请求
        if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
        {
            if (context.Request.ContentType?.Contains("application/json") != true)
            {
                await ReturnErrorResponse(context, "invalid_content_type", "Content-Type must be application/json", 400);
                return null;
            }

            using var reader = new StreamReader(context.Request.Body);
            return await reader.ReadToEndAsync();
        }

        return string.Empty;
    }

    /// <summary>
    /// 基于规则选择合适的AI服务通道
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="requestBody">请求体内容</param>
    /// <returns>选中的通道配置，如果没有合适的通道则返回null</returns>
    private async Task<Models.Config.ChannelConfig?> SelectChannel(HttpContext context, string requestBody)
    {
        var config = _configService.GetConfig();
        
        // 创建规则评估上下文
        var ruleContext = new Dictionary<string, object>
        {
            ["day_tokens_used"] = 0, // 将从通道使用情况跟踪中检索
            ["time_of_day"] = DateTime.Now.ToString("HH:mm"),
            ["model"] = GetModelFromPath(context.Request.Path),
            ["request_method"] = context.Request.Method
        };

        // 评估规则以选择通道
        return await Task.FromResult(_ruleEngine.EvaluateRules(config.Rules, config.Channels, ruleContext));
    }

    /// <summary>
    /// 从请求路径中提取模型名称
    /// </summary>
    /// <param name="path">请求路径</param>
    /// <returns>模型名称</returns>
    private string GetModelFromPath(string path)
    {
        // 从路径中提取模型，例如 /v1/chat/completions 或 /v1/models/gpt-3.5-turbo/completions
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length >= 2)
        {
            return pathParts[1]; // /v1/ 后的第二部分
        }
        return "default";
    }

    /// <summary>
    /// 将请求转发到选定的AI服务通道
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="channel">目标通道配置</param>
    /// <param name="requestBody">请求体内容</param>
    /// <param name="stopwatch">计时器，用于测量响应时间</param>
    private async Task ForwardRequest(HttpContext context, Models.Config.ChannelConfig channel, string requestBody, Stopwatch stopwatch)
    {
        try
        {
            // 应用重试策略
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            await retryPolicy.ExecuteAsync(async () =>
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(_configService.GetConfig().Server.Timeout);

                // 准备请求
                var targetUrl = BuildTargetUrl(context, channel);
                var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

                // 复制请求头
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.ToLower() != "authorization" && header.Key.ToLower() != "host")
                    {
                        targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // 添加通道API密钥
                targetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", channel.ApiKey);

                // 为POST/PUT请求添加请求体
                if (!string.IsNullOrEmpty(requestBody) &&
                    (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put))
                {
                    targetRequest.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                }

                // 发送请求
                var response = await httpClient.SendAsync(targetRequest);

                // 跟踪指标
                var responseTime = stopwatch.ElapsedMilliseconds;
                // 这里会增加Prometheus指标

                // 将响应复制回客户端
                context.Response.StatusCode = (int)response.StatusCode;

                // 复制响应头
                foreach (var header in response.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in response.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // 复制响应体
                var responseContent = await response.Content.ReadAsStringAsync();
                await context.Response.WriteAsync(responseContent);

                // 更新通道使用情况
                var tokens = EstimateTokens(responseContent);
                _channelService.UpdateChannelUsage(channel.Name, tokens);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding request to channel {ChannelName}", channel.Name);
            await ReturnErrorResponse(context, "forwarding_error", "Error forwarding request to AI provider", 502);
        }
    }

    /// <summary>
    /// 构建目标URL，根据通道配置进行模型映射
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="channel">目标通道配置</param>
    /// <returns>构建的目标URL</returns>
    private string BuildTargetUrl(HttpContext context, Models.Config.ChannelConfig channel)
    {
        // 如果需要，映射模型
        var model = GetModelFromPath(context.Request.Path);
        var mappedModel = channel.ModelMapping.ContainsKey(model) ? channel.ModelMapping[model] : model;
        
        // 构建目标URL
        var basePath = channel.Endpoint.TrimEnd('/');
        var path = context.Request.Path.ToString().Substring(3); // 移除/v1前缀
        
        // 如果模型存在，在路径中替换模型
        if (model != mappedModel)
        {
            path = path.Replace($"/{model}/", $"/{mappedModel}/");
        }
        
        return $"{basePath}{path}";
    }

    /// <summary>
    /// 估算响应内容中的令牌数
    /// </summary>
    /// <param name="responseContent">响应内容</param>
    /// <returns>估算的令牌数</returns>
    private int EstimateTokens(string responseContent)
    {
        // 简单的令牌估算 - 在实际实现中，您应该使用适当的分词器
        return responseContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// 返回错误响应给客户端
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误消息</param>
    /// <param name="statusCode">HTTP状态码</param>
    private async Task ReturnErrorResponse(HttpContext context, string code, string message, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorInfo
            {
                Code = code,
                Message = message,
                Type = GetErrorTypeFromStatusCode(statusCode)
            }
        };

        var json = JsonConvert.SerializeObject(errorResponse);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// 根据HTTP状态码获取错误类型
    /// </summary>
    /// <param name="statusCode">HTTP状态码</param>
    /// <returns>错误类型字符串</returns>
    private string GetErrorTypeFromStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "invalid_request_error",
            401 => "authentication_error",
            404 => "not_found_error",
            429 => "rate_limit_error",
            500 => "server_error",
            502 => "bad_gateway",
            503 => "service_unavailable",
            504 => "gateway_timeout",
            _ => "api_error"
        };
    }
}
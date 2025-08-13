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

public class ProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProxyMiddleware> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfigurationService _configService;
    private readonly IRuleEngine _ruleEngine;
    private readonly IChannelService _channelService;

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

    public async Task InvokeAsync(HttpContext context)
    {
        // Only handle API requests to /v1/
        if (!context.Request.Path.StartsWithSegments("/v1"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Authenticate request
            if (!await AuthenticateRequest(context))
            {
                return;
            }

            // Apply rate limiting
            if (!await ApplyRateLimiting(context))
            {
                return;
            }

            // Preprocess request
            var requestBody = await PreprocessRequest(context);
            if (requestBody == null)
            {
                return;
            }

            // Select channel based on rules
            var channel = await SelectChannel(context, requestBody);
            if (channel == null)
            {
                await ReturnErrorResponse(context, "no_channel_available", "No available channels for routing", 503);
                return;
            }

            // Forward request to selected channel
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

    private async Task<bool> AuthenticateRequest(HttpContext context)
    {
        var config = _configService.GetConfig();
        
        // Extract API key from Authorization header
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

    private async Task<bool> ApplyRateLimiting(HttpContext context)
    {
        // Simple rate limiting implementation
        // In a production environment, you would use a more sophisticated rate limiting solution
        await Task.CompletedTask;
        return true;
    }

    private async Task<string?> PreprocessRequest(HttpContext context)
    {
        // Only process POST requests with JSON body
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

    private async Task<Models.Config.ChannelConfig?> SelectChannel(HttpContext context, string requestBody)
    {
        var config = _configService.GetConfig();
        
        // Create context for rule evaluation
        var ruleContext = new Dictionary<string, object>
        {
            ["day_tokens_used"] = 0, // Would be retrieved from channel usage tracking
            ["time_of_day"] = DateTime.Now.ToString("HH:mm"),
            ["model"] = GetModelFromPath(context.Request.Path),
            ["request_method"] = context.Request.Method
        };

        // Evaluate rules to select channel
        return await Task.FromResult(_ruleEngine.EvaluateRules(config.Rules, config.Channels, ruleContext));
    }

    private string GetModelFromPath(string path)
    {
        // Extract model from path like /v1/chat/completions or /v1/models/gpt-3.5-turbo/completions
        var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length >= 2)
        {
            return pathParts[1]; // Second part after /v1/
        }
        return "default";
    }

    private async Task ForwardRequest(HttpContext context, Models.Config.ChannelConfig channel, string requestBody, Stopwatch stopwatch)
    {
        try
        {
            // Apply retry policies
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            await retryPolicy.ExecuteAsync(async () =>
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(_configService.GetConfig().Server.Timeout);

                // Prepare the request
                var targetUrl = BuildTargetUrl(context, channel);
                var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

                // Copy headers
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.ToLower() != "authorization" && header.Key.ToLower() != "host")
                    {
                        targetRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }

                // Add channel API key
                targetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", channel.ApiKey);

                // Add request body for POST/PUT requests
                if (!string.IsNullOrEmpty(requestBody) && 
                    (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put))
                {
                    targetRequest.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                }

                // Send request
                var response = await httpClient.SendAsync(targetRequest);

                // Track metrics
                var responseTime = stopwatch.ElapsedMilliseconds;
                // Would increment Prometheus metrics here

                // Copy response back to client
                context.Response.StatusCode = (int)response.StatusCode;

                // Copy headers
                foreach (var header in response.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in response.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // Copy response body
                var responseContent = await response.Content.ReadAsStringAsync();
                await context.Response.WriteAsync(responseContent);

                // Update channel usage
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

    private string BuildTargetUrl(HttpContext context, Models.Config.ChannelConfig channel)
    {
        // Map model if needed
        var model = GetModelFromPath(context.Request.Path);
        var mappedModel = channel.ModelMapping.ContainsKey(model) ? channel.ModelMapping[model] : model;
        
        // Build target URL
        var basePath = channel.Endpoint.TrimEnd('/');
        var path = context.Request.Path.ToString().Substring(3); // Remove /v1 prefix
        
        // Replace model in path if it exists
        if (model != mappedModel)
        {
            path = path.Replace($"/{model}/", $"/{mappedModel}/");
        }
        
        return $"{basePath}{path}";
    }

    private int EstimateTokens(string responseContent)
    {
        // Simple token estimation - in a real implementation, you would use a proper tokenizer
        return responseContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

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
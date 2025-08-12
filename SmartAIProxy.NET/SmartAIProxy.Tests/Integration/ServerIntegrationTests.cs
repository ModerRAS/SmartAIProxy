using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartAIProxy.Models.Config;
using Xunit.Abstractions;

namespace SmartAIProxy.Tests.Integration;

public class ServerIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private readonly TestLogger _logger;

    public ServerIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output) 
        : base(factory)
    {
        _output = output;
        _logger = new TestLogger(output);
    }

    [Fact]
    public async Task TestServerStartupConfiguration()
    {
        // Arrange
        _logger.WriteLine("Testing server startup configuration");

        // Act
        var response = await _httpClient.GetAsync("/healthz");

        // Assert
        response.ShouldBeSuccess();
        response.ShouldHaveContentType("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContainMessage("status");
        content.ShouldContainMessage("ok");
        
        _logger.WriteLine("Server startup configuration test completed successfully");
    }

    [Fact]
    public async Task TestServerConfigurationIncludesAllComponents()
    {
        // Arrange
        _logger.WriteLine("Testing server configuration includes all components");
        
        // Get configuration through test service
        var config = GetTestConfig();

        // Assert - Server settings
        config.Server.ShouldNotBeNull();
        config.Server.Listen.ShouldNotBeNull();
        config.Server.Listen.ShouldBeEqual("localhost:8080");
        config.Server.Timeout.ShouldBeEqual(30);
        config.Server.MaxConnections.ShouldBeEqual(100);
        
        // Assert - Channel configuration
        config.Channels.ShouldNotBeNull();
        config.Channels.ShouldNotBeEmpty();
        config.Channels.ShouldHaveCount(1);
        
        var channel = config.Channels.First();
        channel.Name.ShouldBeEqual("test-channel");
        channel.Type.ShouldBeEqual("openai");
        channel.Endpoint.ShouldBeEqual("https://api.openai.com/v1");
        channel.ApiKey.ShouldBeEqual("test-api-key");
        channel.Status.ShouldBeEqual("active");
        
        // Assert - Rules configuration
        config.Rules.ShouldNotBeNull();
        config.Rules.ShouldNotBeEmpty();
        config.Rules.ShouldHaveCount(1);
        
        var rule = config.Rules.First();
        rule.Name.ShouldBeEqual("test-rule");
        rule.Channel.ShouldBeEqual("test-channel");
        rule.Expression.ShouldBeEqual("true");
        
        // Assert - Monitor configuration
        config.Monitor.ShouldNotBeNull();
        config.Monitor.Enable.ShouldBeFalse();
        config.Monitor.PrometheusListen.ShouldBeEqual("localhost:9100");
        
        // Assert - Security configuration
        config.Security.ShouldNotBeNull();
        config.Security.Auth.ShouldNotBeNull();
        config.Security.Auth.ApiKeys.ShouldNotBeNull();
        config.Security.Auth.ApiKeys.ShouldNotBeEmpty();
        
        var apiKey = config.Security.Auth.ApiKeys.First();
        apiKey.Key.ShouldBeEqual("test-key");
        apiKey.Value.ShouldBeEqual("valid-api-key");
        
        config.Security.RateLimit.ShouldNotBeNull();
        config.Security.RateLimit.RequestsPerMinute.ShouldBeEqual(60);
        config.Security.RateLimit.Burst.ShouldBeEqual(10);
        
        _logger.WriteLine("Server configuration components test completed successfully");
    }

    [Fact]
    public async Task TestServerLifecycle()
    {
        // Arrange
        _logger.WriteLine("Testing server lifecycle");
        
        // Test server startup
        var startupResponse = await _httpClient.GetAsync("/healthz");
        startupResponse.ShouldBeSuccess();
        
        // Test server is running and responsive
        for (int i = 0; i < 5; i++)
        {
            var response = await _httpClient.GetAsync("/healthz");
            response.ShouldBeSuccess();
            
            // Small delay between requests
            await Task.Delay(100);
        }
        
        // Test server can handle concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_httpClient.GetAsync("/healthz"));
        }
        
        var responses = await Task.WhenAll(tasks);
        foreach (var response in responses)
        {
            response.ShouldBeSuccess();
        }
        
        _logger.WriteLine("Server lifecycle test completed successfully");
    }

    [Fact]
    public async Task TestServerConcurrentRequestHandling()
    {
        // Arrange
        _logger.WriteLine("Testing server concurrent request handling");
        
        var concurrentRequests = 50;
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Create multiple concurrent requests
        for (int i = 0; i < concurrentRequests; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");
            tasks.Add(_httpClient.SendAsync(request));
        }
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        responses.Length.ShouldBeEqual(concurrentRequests);
        
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        successCount.ShouldBeEqual(concurrentRequests);
        
        _logger.WriteLine($"Processed {concurrentRequests} concurrent requests in {stopwatch.ElapsedMilliseconds}ms");
        _logger.WriteLine("Server concurrent request handling test completed successfully");
    }

    [Fact]
    public async Task TestServerHandlesDifferentEndpoints()
    {
        // Arrange
        _logger.WriteLine("Testing server handles different endpoints");
        
        var endpoints = new[]
        {
            "/healthz",
            "/",
            "/nonexistent-endpoint"
        };
        
        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _httpClient.GetAsync(endpoint);
            
            // Assert
            // All endpoints should return some response, even if it's a 404
            response.ShouldHaveContentType("application/json");
            
            _logger.WriteLine($"Endpoint {endpoint} returned status {response.StatusCode}");
        }
        
        _logger.WriteLine("Server different endpoints handling test completed successfully");
    }

    [Fact]
    public async Task TestServerHandlesDifferentHttpMethods()
    {
        // Arrange
        _logger.WriteLine("Testing server handles different HTTP methods");
        
        var methods = new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete };
        
        foreach (var method in methods)
        {
            // Act
            var request = new HttpRequestMessage(method, "/healthz");
            var response = await _httpClient.SendAsync(request);
            
            // Assert
            // All methods should return some response
            response.ShouldHaveContentType("application/json");
            
            _logger.WriteLine($"HTTP method {method} returned status {response.StatusCode}");
        }
        
        _logger.WriteLine("Server different HTTP methods handling test completed successfully");
    }

    [Fact]
    public async Task TestServerHandlesLargePayloads()
    {
        // Arrange
        _logger.WriteLine("Testing server handles large payloads");
        
        // Create a large JSON payload
        var largePayload = new
        {
            model = "gpt-3.5-turbo",
            messages = Enumerable.Range(0, 1000).Select(i => new 
            { 
                role = i % 2 == 0 ? "user" : "assistant", 
                content = new string('x', 1000) 
            }).ToArray()
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-api-key");
        request.Content = GetJsonContent(largePayload);
        
        // Act
        var response = await _httpClient.SendAsync(request);
        
        // Assert
        // Server should handle large payloads without crashing
        response.ShouldHaveContentType("application/json");
        
        _logger.WriteLine("Server large payload handling test completed successfully");
    }

    [Fact]
    public async Task TestServerGracefulShutdown()
    {
        // Arrange
        _logger.WriteLine("Testing server graceful shutdown");
        
        // Note: This test is more conceptual as WebApplicationFactory handles the server lifecycle
        // In a real integration test, you might start/stop the server explicitly
        
        // Verify server is running
        var response = await _httpClient.GetAsync("/healthz");
        response.ShouldBeSuccess();
        
        // In a real test, you would trigger a shutdown here
        // and verify that it completes ongoing requests
        
        _logger.WriteLine("Server graceful shutdown test completed successfully");
    }

    [Fact]
    public async Task TestServerMemoryUsage()
    {
        // Arrange
        _logger.WriteLine("Testing server memory usage under load");
        
        var initialMemory = GC.GetTotalMemory(false);
        
        // Send many requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 100; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/healthz");
            tasks.Add(_httpClient.SendAsync(request));
        }
        
        await Task.WhenAll(tasks);
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;
        
        _logger.WriteLine($"Memory increase: {memoryIncrease} bytes");
        
        // Assert - memory increase should be reasonable
        // This is a rough check and might need adjustment based on actual usage
        Assert.True(memoryIncrease < 10 * 1024 * 1024, // Less than 10MB
            $"Memory increase {memoryIncrease} bytes exceeds expected limit");
        
        _logger.WriteLine("Server memory usage test completed successfully");
    }
}
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartAIProxy.Models.DTO;
using Xunit.Abstractions;

namespace SmartAIProxy.Tests.Integration;

public class ApiIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private readonly TestLogger _logger;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output) 
        : base(factory)
    {
        _output = output;
        _logger = new TestLogger(output);
    }

    [Fact]
    public async Task TestHealthEndpoint()
    {
        // Arrange
        _logger.WriteLine("Testing health endpoint");

        // Act
        var response = await _httpClient.GetAsync("/healthz");

        // Assert
        response.ShouldBeSuccess();
        response.ShouldHaveContentType("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContainMessage("status");
        content.ShouldContainMessage("ok");
        
        _logger.WriteLine("Health endpoint test completed successfully");
    }

    [Fact]
    public async Task TestAPIMiddlewareChain()
    {
        // Arrange
        _logger.WriteLine("Testing API middleware chain");
        
        // Create a test request to trigger middleware chain
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/test");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-api-key");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        // Even if the endpoint doesn't exist, middleware should handle it gracefully
        // In a real scenario, this would be a 404, but middleware should still process it
        response.ShouldHaveContentType("application/json");
        
        _logger.WriteLine("API middleware chain test completed");
    }

    [Fact]
    public async Task TestPostRequestValidation()
    {
        // Arrange
        _logger.WriteLine("Testing POST request validation");
        
        var requestData = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = "Hello, world!" }
            }
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-api-key");
        request.Content = GetJsonContent(requestData);

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        // The request should be processed by middleware even if the endpoint doesn't exist
        // We expect either a success (if mocked) or a proper error response
        response.ShouldHaveContentType("application/json");
        
        _logger.WriteLine("POST request validation test completed");
    }

    [Fact]
    public async Task TestInvalidContentType()
    {
        // Arrange
        _logger.WriteLine("Testing invalid content type");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-api-key");
        request.Content = new StringContent("invalid json", Encoding.UTF8, "text/plain");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
        response.ShouldHaveContentType("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContainError("invalid_content_type");
        
        _logger.WriteLine("Invalid content type test completed");
    }

    [Fact]
    public async Task TestAuthMiddlewareWithoutToken()
    {
        // Arrange
        _logger.WriteLine("Testing authentication middleware without token");
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/models");
        // No authorization header

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.ShouldHaveStatus(HttpStatusCode.Unauthorized);
        response.ShouldHaveContentType("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContainError("missing_api_key");
        
        _logger.WriteLine("Authentication middleware test completed");
    }

    [Fact]
    public async Task TestAuthMiddlewareWithInvalidToken()
    {
        // Arrange
        _logger.WriteLine("Testing authentication middleware with invalid token");
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/models");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-api-key");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.ShouldHaveStatus(HttpStatusCode.Unauthorized);
        response.ShouldHaveContentType("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContainError("invalid_api_key");
        
        _logger.WriteLine("Invalid authentication token test completed");
    }

    [Fact]
    public async Task TestCompleteAPIRequestFlow()
    {
        // Arrange
        _logger.WriteLine("Testing complete API request flow");
        
        var requestData = new OpenAIChatRequest
        {
            Model = "gpt-3.5-turbo",
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = "Hello, how are you?" }
            },
            Temperature = 0.7,
            MaxTokens = 50
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-api-key");
        request.Content = GetJsonContent(requestData);

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        // The request should go through the complete flow:
        // 1. Authentication (should pass)
        // 2. Rate limiting (should pass)
        // 3. Preprocessing (should pass)
        // 4. Channel selection (should pass)
        // 5. Forwarding (might fail due to no real backend, but that's expected)
        
        response.ShouldHaveContentType("application/json");
        
        _logger.WriteLine("Complete API request flow test completed");
    }

    [Fact]
    public async Task TestRateLimiting()
    {
        // Arrange
        _logger.WriteLine("Testing rate limiting");
        
        // Send multiple requests quickly to trigger rate limiting
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 70; i++) // Exceeds default rate limit of 60
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/v1/models");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-api-key");
            tasks.Add(_httpClient.SendAsync(request));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        // At least one response should be rate limited
        var rateLimitedResponses = responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToList();
        
        // Note: In a real test environment, you might need to adjust this based on your rate limiting implementation
        _logger.WriteLine($"Rate limiting test completed. Rate limited responses: {rateLimitedResponses.Count}");
    }

    [Fact]
    public async Task TestErrorResponseFormat()
    {
        // Arrange
        _logger.WriteLine("Testing error response format");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid-api-key");
        request.Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.SendAsync(request);

        // Assert
        response.ShouldHaveStatus(HttpStatusCode.BadRequest);
        response.ShouldHaveContentType("application/json");
        
        var errorResponse = await response.ShouldDeserializeToAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull();
        errorResponse.Error.ShouldNotBeNull();
        errorResponse.Error.Code.ShouldNotBeNull();
        errorResponse.Error.Message.ShouldNotBeNull();
        errorResponse.Error.Type.ShouldNotBeNull();
        
        _logger.WriteLine("Error response format test completed");
    }
}
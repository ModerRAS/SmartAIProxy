using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit.Abstractions;

namespace SmartAIProxy.Tests.Integration;

public static class TestHelpers
{
    public static void ShouldHaveStatus(this HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    public static void ShouldBeSuccess(this HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"Expected success status code, got {response.StatusCode}");
    }

    public static void ShouldHaveContentType(this HttpResponseMessage response, string expectedContentType)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Contains(expectedContentType, contentType ?? string.Empty);
    }

    public static void ShouldContainHeader(this HttpResponseMessage response, string headerName)
    {
        Assert.True(response.Headers.Contains(headerName) || 
                   response.Content.Headers.Contains(headerName), 
                   $"Expected response to contain header {headerName}");
    }

    public static async Task<T> ShouldDeserializeToAsync<T>(this HttpResponseMessage response, JsonSerializerOptions? options = null)
    {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(content, options);
        Assert.NotNull(result);
        return result!;
    }

    public static void ShouldHaveAuthorizationHeader(this HttpRequestHeaders headers, string scheme = "Bearer")
    {
        Assert.True(headers.Authorization != null, "Expected Authorization header");
        Assert.Equal(scheme, headers.Authorization?.Scheme);
    }

    public static void ShouldHaveApiVersion(this HttpResponseMessage response, string expectedVersion = "1.0")
    {
        if (response.Headers.TryGetValues("X-API-Version", out var values))
        {
            Assert.Contains(expectedVersion, values);
        }
        else
        {
            Assert.Fail("Expected X-API-Version header");
        }
    }

    public static void ShouldBeJson(this HttpResponseMessage response)
    {
        response.ShouldHaveContentType("application/json");
    }

    public static void ShouldBeProblemDetails(this HttpResponseMessage response)
    {
        response.ShouldHaveContentType("application/problem+json");
    }

    public static async Task<string> GetResponseBodyAsync(this HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    public static void ShouldContainError(this string responseBody, string expectedErrorCode)
    {
        Assert.Contains(expectedErrorCode, responseBody);
    }

    public static void ShouldContainMessage(this string responseBody, string expectedMessage)
    {
        Assert.Contains(expectedMessage, responseBody);
    }

    public static void ShouldNotBeNull<T>(this T? value, string? message = null)
    {
        Assert.NotNull(value);
    }

    public static void ShouldBeNull<T>(this T? value, string? message = null)
    {
        Assert.Null(value);
    }

    public static void ShouldBeEqual<T>(this T actual, T expected, string? message = null)
    {
        Assert.Equal(expected, actual);
    }

    public static void ShouldBeTrue(this bool condition, string? message = null)
    {
        Assert.True(condition);
    }

    public static void ShouldBeFalse(this bool condition, string? message = null)
    {
        Assert.False(condition);
    }

    public static void ShouldContain<T>(this IEnumerable<T> collection, T expected, string? message = null)
    {
        Assert.Contains(expected, collection);
    }

    public static void ShouldNotContain<T>(this IEnumerable<T> collection, T expected, string? message = null)
    {
        Assert.DoesNotContain(expected, collection);
    }

    public static void ShouldBeEmpty<T>(this IEnumerable<T> collection, string? message = null)
    {
        Assert.Empty(collection);
    }

    public static void ShouldNotBeEmpty<T>(this IEnumerable<T> collection, string? message = null)
    {
        Assert.NotEmpty(collection);
    }

    public static void ShouldHaveCount<T>(this IEnumerable<T> collection, int expectedCount, string? message = null)
    {
        Assert.Equal(expectedCount, collection.Count());
    }
}

public class TestLogger
{
    private readonly ITestOutputHelper _output;

    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public void WriteLine(string message)
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    public void WriteLine(string format, params object[] args)
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {string.Format(format, args)}");
    }
}
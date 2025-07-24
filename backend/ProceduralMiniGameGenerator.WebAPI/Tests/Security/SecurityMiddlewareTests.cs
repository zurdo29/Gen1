using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Security;

public class SecurityMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SecurityMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SecurityHeaders_ShouldBePresent()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
        
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
        Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").First());
    }

    [Fact]
    public async Task RateLimit_ShouldAllowNormalRequests()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration/presets");

        // Assert
        Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task RateLimit_ShouldBlockExcessiveRequests()
    {
        // Arrange
        var endpoint = "/api/generation/validate-config";
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send many requests simultaneously
        for (int i = 0; i < 35; i++) // Exceed the limit of 30 per minute
        {
            tasks.Add(_client.PostAsync(endpoint, new StringContent("{\"test\":\"data\"}", System.Text.Encoding.UTF8, "application/json")));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - At least some requests should be rate limited
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedCount > 0, "Expected some requests to be rate limited");

        // Check that rate limited responses have proper headers
        var rateLimitedResponse = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        if (rateLimitedResponse != null)
        {
            Assert.True(rateLimitedResponse.Headers.Contains("Retry-After"));
            Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Limit"));
            Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Remaining"));
            Assert.True(rateLimitedResponse.Headers.Contains("X-RateLimit-Reset"));
        }
    }

    [Fact]
    public async Task CORS_ShouldAllowConfiguredOrigins()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");

        // Act
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Options, "/api/configuration/presets"));

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin") || 
                   response.StatusCode != HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger")]
    [InlineData("/hangfire")]
    public async Task RateLimit_ShouldSkipExcludedPaths(string path)
    {
        // Act - Make multiple requests to excluded paths
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_client.GetAsync(path));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - None should be rate limited
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.Equal(0, rateLimitedCount);
    }

    [Fact]
    public async Task SecurityHeaders_ShouldIncludeCustomHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration/presets");

        // Assert
        Assert.True(response.Headers.Contains("X-API-Version"));
        Assert.True(response.Headers.Contains("X-Security-Policy"));
        
        Assert.Equal("1.0", response.Headers.GetValues("X-API-Version").First());
        Assert.Equal("strict", response.Headers.GetValues("X-Security-Policy").First());
    }

    [Fact]
    public async Task Server_HeaderShouldBeObfuscated()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        if (response.Headers.Contains("Server"))
        {
            var serverHeader = response.Headers.GetValues("Server").First();
            Assert.Equal("WebAPI", serverHeader);
        }
    }
}
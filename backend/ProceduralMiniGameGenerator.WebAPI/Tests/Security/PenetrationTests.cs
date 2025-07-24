using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Security;

/// <summary>
/// Penetration tests to validate security measures against common attacks
/// </summary>
public class PenetrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PenetrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<img src=x onerror=alert('xss')>")]
    [InlineData("';DROP TABLE users;--")]
    [InlineData("' OR '1'='1")]
    public async Task XSS_And_SQLInjection_ShouldBeBlocked(string maliciousInput)
    {
        // Arrange
        var payload = $"{{\"name\":\"{maliciousInput}\",\"description\":\"test\"}}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/configuration/presets", content);

        // Assert - Should either be rejected or sanitized
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                   response.StatusCode == HttpStatusCode.OK);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("<script>", responseContent);
            Assert.DoesNotContain("javascript:", responseContent);
            Assert.DoesNotContain("onerror=", responseContent);
        }
    }

    [Fact]
    public async Task DirectoryTraversal_ShouldBeBlocked()
    {
        // Arrange
        var maliciousPaths = new[]
        {
            "../../../etc/passwd",
            "..\\..\\..\\windows\\system32\\config\\sam",
            "%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd",
            "....//....//....//etc/passwd"
        };

        foreach (var path in maliciousPaths)
        {
            // Act
            var response = await _client.GetAsync($"/api/export/level?filename={path}");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task LargePayload_ShouldBeRejected()
    {
        // Arrange - Create a very large payload (>10MB)
        var largeData = new string('x', 10 * 1024 * 1024);
        var payload = $"{{\"data\":\"{largeData}\"}}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/generation/generate", content);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                   response.StatusCode == HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task MalformedJSON_ShouldBeHandledGracefully()
    {
        // Arrange
        var malformedPayloads = new[]
        {
            "{invalid json",
            "{'single': 'quotes'}",
            "{\"unclosed\": \"string",
            "{\"trailing\": \"comma\",}",
            "null",
            "undefined"
        };

        foreach (var payload in malformedPayloads)
        {
            // Act
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/generation/validate-config", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Exception", responseContent);
            Assert.DoesNotContain("StackTrace", responseContent);
        }
    }

    [Fact]
    public async Task HTTPMethods_ShouldBeRestricted()
    {
        // Arrange
        var restrictedMethods = new[] { HttpMethod.Trace, HttpMethod.Head };
        
        foreach (var method in restrictedMethods)
        {
            // Act
            var request = new HttpRequestMessage(method, "/api/generation/generate");
            var response = await _client.SendAsync(request);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.MethodNotAllowed ||
                       response.StatusCode == HttpStatusCode.NotFound);
        }
    }

    [Theory]
    [InlineData("User-Agent", "Mozilla/5.0 <script>alert('xss')</script>")]
    [InlineData("X-Forwarded-For", "127.0.0.1; DROP TABLE users;")]
    [InlineData("Referer", "javascript:alert('xss')")]
    public async Task MaliciousHeaders_ShouldBeHandled(string headerName, string headerValue)
    {
        // Arrange
        _client.DefaultRequestHeaders.Add(headerName, headerValue);

        // Act
        var response = await _client.GetAsync("/api/configuration/presets");

        // Assert - Should not crash the server
        Assert.True(response.StatusCode != HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CORS_ShouldRejectUnauthorizedOrigins()
    {
        // Arrange
        var maliciousOrigins = new[]
        {
            "http://evil.com",
            "https://attacker.net",
            "null",
            "file://",
            "data:text/html,<script>alert('xss')</script>"
        };

        foreach (var origin in maliciousOrigins)
        {
            // Act
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/generation/generate");
            request.Headers.Add("Origin", origin);
            var response = await _client.SendAsync(request);

            // Assert
            if (response.Headers.Contains("Access-Control-Allow-Origin"))
            {
                var allowedOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").First();
                Assert.NotEqual(origin, allowedOrigin);
            }
        }
    }

    [Fact]
    public async Task FileUpload_ShouldValidateFileTypes()
    {
        // Arrange
        var maliciousFiles = new[]
        {
            ("malicious.exe", "application/octet-stream"),
            ("script.js", "application/javascript"),
            ("config.php", "application/x-php"),
            ("shell.sh", "application/x-sh")
        };

        foreach (var (fileName, contentType) in maliciousFiles)
        {
            // Act
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("malicious content"));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var response = await _client.PostAsync("/api/export/level", content);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.UnsupportedMediaType);
        }
    }

    [Fact]
    public async Task RateLimiting_ShouldPreventDDoS()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Simulate DDoS attack
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_client.GetAsync("/api/generation/generate"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.True(rateLimitedCount > 0, "Rate limiting should block excessive requests");

        // Verify rate limit headers are present
        var rateLimitedResponse = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        if (rateLimitedResponse != null)
        {
            Assert.True(rateLimitedResponse.Headers.Contains("Retry-After"));
        }
    }
}
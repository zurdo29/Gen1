using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Security;

public class SecurityServiceTests
{
    private readonly SecurityService _securityService;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<SecurityService>> _loggerMock;

    public SecurityServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<SecurityService>>();
        _securityService = new SecurityService(_memoryCache, _loggerMock.Object);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>", "")]
    [InlineData("Hello <b>World</b>", "Hello World")]
    [InlineData("Normal text", "Normal text")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void SanitizeHtml_ShouldRemoveHtmlTags(string input, string expected)
    {
        // Act
        var result = _securityService.SanitizeHtml(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<script>", "&lt;script&gt;")]
    [InlineData("Hello & World", "Hello &amp; World")]
    [InlineData("\"quoted\"", "&quot;quoted&quot;")]
    [InlineData("'single'", "&#x27;single&#x27;")]
    [InlineData("path/to/file", "path&#x2F;to&#x2F;file")]
    [InlineData("", "")]
    public void SanitizeText_ShouldEscapeDangerousCharacters(string input, string expected)
    {
        // Act
        var result = _securityService.SanitizeText(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("valid_file.txt", true)]
    [InlineData("file-name.json", true)]
    [InlineData("file.with.dots.xml", true)]
    [InlineData("123numbers.csv", true)]
    [InlineData("", false)]
    [InlineData("file with spaces.txt", false)]
    [InlineData("file<script>.txt", false)]
    [InlineData("CON.txt", false)]
    [InlineData("PRN.json", false)]
    [InlineData("file/path.txt", false)]
    public void IsValidFileName_ShouldValidateFileNames(string fileName, bool expected)
    {
        // Act
        var result = _securityService.IsValidFileName(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("{\"valid\": \"json\"}", true)]
    [InlineData("{\"number\": 123}", true)]
    [InlineData("", false)]
    [InlineData("invalid json", false)]
    [InlineData("{\"script\": \"<script>alert('xss')</script>\"}", false)]
    [InlineData("{\"eval\": \"eval('malicious')\"}", false)]
    public void ValidateConfigurationInput_ShouldValidateJsonSafety(string json, bool shouldBeValid)
    {
        // Act
        var result = _securityService.ValidateConfigurationInput(json);

        // Assert
        if (shouldBeValid)
        {
            Assert.Equal(ValidationResult.Success, result);
        }
        else
        {
            Assert.NotEqual(ValidationResult.Success, result);
        }
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldAllowWithinLimit()
    {
        // Arrange
        var ipAddress = "192.168.1.1";
        var endpoint = "/api/test";

        // Act
        var result = await _securityService.CheckRateLimitAsync(ipAddress, endpoint);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldBlockWhenLimitExceeded()
    {
        // Arrange
        var ipAddress = "192.168.1.2";
        var endpoint = "/api/generation/generate";

        // Act - Make requests up to the limit
        for (int i = 0; i < 10; i++)
        {
            await _securityService.CheckRateLimitAsync(ipAddress, endpoint);
        }

        // Try one more request that should be blocked
        var result = await _securityService.CheckRateLimitAsync(ipAddress, endpoint);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateConfigurationInput_ShouldRejectExcessiveNesting()
    {
        // Arrange - Create deeply nested JSON
        var deepJson = "{";
        for (int i = 0; i < 15; i++)
        {
            deepJson += "\"level" + i + "\":{";
        }
        deepJson += "\"value\":\"test\"";
        for (int i = 0; i < 15; i++)
        {
            deepJson += "}";
        }
        deepJson += "}";

        // Act
        var result = _securityService.ValidateConfigurationInput(deepJson);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("excessive nesting", result.ErrorMessage);
    }

    [Fact]
    public void ValidateConfigurationInput_ShouldRejectExcessiveSize()
    {
        // Arrange - Create very large JSON
        var largeJson = "{\"data\":\"" + new string('x', 100001) + "\"}";

        // Act
        var result = _securityService.ValidateConfigurationInput(largeJson);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("too large", result.ErrorMessage);
    }

    [Fact]
    public void SanitizeText_ShouldTruncateLongInput()
    {
        // Arrange
        var longInput = new string('a', 1500);

        // Act
        var result = _securityService.SanitizeText(longInput);

        // Assert
        Assert.Equal(1000, result.Length);
    }
}
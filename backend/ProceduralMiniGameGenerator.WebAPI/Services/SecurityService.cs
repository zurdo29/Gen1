using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Microsoft.Extensions.Caching.Memory;

namespace ProceduralMiniGameGenerator.WebAPI.Services;

/// <summary>
/// Implementation of security service for input sanitization and validation
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly HtmlSanitizer _htmlSanitizer;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecurityService> _logger;
    
    // Rate limiting configuration
    private readonly Dictionary<string, int> _endpointLimits = new()
    {
        { "/api/generation/generate", 10 }, // 10 requests per minute
        { "/api/generation/validate-config", 30 }, // 30 requests per minute
        { "/api/export/level", 5 }, // 5 requests per minute
        { "default", 60 } // 60 requests per minute for other endpoints
    };
    
    private static readonly Regex FileNameRegex = new(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);
    private static readonly Regex SafeTextRegex = new(@"^[a-zA-Z0-9\s._,-]+$", RegexOptions.Compiled);
    
    public SecurityService(IMemoryCache cache, ILogger<SecurityService> logger)
    {
        _cache = cache;
        _logger = logger;
        
        // Configure HTML sanitizer with strict settings
        _htmlSanitizer = new HtmlSanitizer();
        _htmlSanitizer.AllowedTags.Clear();
        _htmlSanitizer.AllowedAttributes.Clear();
        _htmlSanitizer.AllowedCssProperties.Clear();
        _htmlSanitizer.AllowedSchemes.Clear();
    }
    
    public string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        try
        {
            var sanitized = _htmlSanitizer.Sanitize(input);
            
            if (sanitized != input)
            {
                _logger.LogWarning("HTML input was sanitized. Original length: {OriginalLength}, Sanitized length: {SanitizedLength}", 
                    input.Length, sanitized.Length);
            }
            
            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing HTML input");
            return string.Empty;
        }
    }
    
    public string SanitizeText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        // Remove potentially dangerous characters
        var sanitized = input
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;")
            .Replace("/", "&#x2F;")
            .Replace("\\", "&#x5C;")
            .Replace("&", "&amp;");
            
        // Limit length to prevent DoS
        if (sanitized.Length > 1000)
        {
            sanitized = sanitized.Substring(0, 1000);
            _logger.LogWarning("Text input was truncated due to excessive length");
        }
        
        return sanitized;
    }
    
    public bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
            
        // Check for valid characters only
        if (!FileNameRegex.IsMatch(fileName))
            return false;
            
        // Check for reserved names
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
        
        if (reservedNames.Contains(nameWithoutExtension))
            return false;
            
        // Check length
        if (fileName.Length > 255)
            return false;
            
        return true;
    }
    
    public ValidationResult ValidateConfigurationInput(string configurationJson)
    {
        if (string.IsNullOrEmpty(configurationJson))
            return new ValidationResult("Configuration cannot be empty");
            
        try
        {
            // Check for potentially dangerous patterns
            var dangerousPatterns = new[]
            {
                @"<script",
                @"javascript:",
                @"vbscript:",
                @"onload=",
                @"onerror=",
                @"eval\s*\(",
                @"Function\s*\(",
                @"setTimeout\s*\(",
                @"setInterval\s*\("
            };
            
            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(configurationJson, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning("Potentially dangerous pattern detected in configuration: {Pattern}", pattern);
                    return new ValidationResult($"Configuration contains potentially dangerous content: {pattern}");
                }
            }
            
            // Validate JSON structure
            using var document = JsonDocument.Parse(configurationJson);
            
            // Check for excessive nesting (DoS prevention)
            if (GetJsonDepth(document.RootElement) > 10)
            {
                return new ValidationResult("Configuration JSON has excessive nesting depth");
            }
            
            // Check for excessive size
            if (configurationJson.Length > 100000) // 100KB limit
            {
                return new ValidationResult("Configuration JSON is too large");
            }
            
            return ValidationResult.Success!;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in configuration input");
            return new ValidationResult("Invalid JSON format in configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration input");
            return new ValidationResult("Error validating configuration");
        }
    }
    
    public async Task<bool> CheckRateLimitAsync(string ipAddress, string endpoint)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;
            
        var limit = _endpointLimits.GetValueOrDefault(endpoint, _endpointLimits["default"]);
        var cacheKey = $"rate_limit_{ipAddress}_{endpoint}";
        
        var currentCount = _cache.Get<int>(cacheKey);
        
        if (currentCount >= limit)
        {
            _logger.LogWarning("Rate limit exceeded for IP {IpAddress} on endpoint {Endpoint}. Count: {Count}, Limit: {Limit}", 
                ipAddress, endpoint, currentCount, limit);
            return false;
        }
        
        // Increment counter with 1-minute expiration
        _cache.Set(cacheKey, currentCount + 1, TimeSpan.FromMinutes(1));
        
        return true;
    }
    
    private static int GetJsonDepth(JsonElement element)
    {
        var maxDepth = 0;
        
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var depth = GetJsonDepth(property.Value);
                    maxDepth = Math.Max(maxDepth, depth);
                }
                return maxDepth + 1;
                
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var depth = GetJsonDepth(item);
                    maxDepth = Math.Max(maxDepth, depth);
                }
                return maxDepth + 1;
                
            default:
                return 0;
        }
    }
}
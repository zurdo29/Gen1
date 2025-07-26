using System.ComponentModel.DataAnnotations;
using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services;

/// <summary>
/// Service for handling security-related operations including input sanitization and validation
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Sanitizes HTML input to prevent XSS attacks
    /// </summary>
    /// <param name="input">Raw HTML input</param>
    /// <returns>Sanitized HTML string</returns>
    string SanitizeHtml(string input);
    
    /// <summary>
    /// Sanitizes general text input by removing potentially dangerous characters
    /// </summary>
    /// <param name="input">Raw text input</param>
    /// <returns>Sanitized text string</returns>
    string SanitizeText(string input);
    
    /// <summary>
    /// Validates that a string contains only safe characters for file names
    /// </summary>
    /// <param name="fileName">File name to validate</param>
    /// <returns>True if safe, false otherwise</returns>
    bool IsValidFileName(string fileName);
    
    /// <summary>
    /// Validates configuration input against injection attacks
    /// </summary>
    /// <param name="configurationJson">JSON configuration string</param>
    /// <returns>Validation result with any errors</returns>
    WebApiModels.ValidationResult ValidateConfigurationInput(string configurationJson);
    
    /// <summary>
    /// Checks if an IP address should be rate limited
    /// </summary>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="endpoint">API endpoint being accessed</param>
    /// <returns>True if request should be allowed, false if rate limited</returns>
    Task<bool> CheckRateLimitAsync(string ipAddress, string endpoint);
}
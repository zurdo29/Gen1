namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration for AI service integration
    /// </summary>
    public class AIServiceConfig
    {
        /// <summary>
        /// Whether AI service integration is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// API endpoint for the AI service
        /// </summary>
        public string ApiEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// API key for authentication (optional)
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Maximum tokens to request from AI service
        /// </summary>
        public int MaxTokens { get; set; } = 150;

        /// <summary>
        /// Temperature setting for AI generation (0.0 to 1.0)
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Timeout in seconds for AI service requests
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Number of retry attempts for failed requests
        /// </summary>
        public int RetryAttempts { get; set; } = 2;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool IsValid()
        {
            if (!IsEnabled)
                return true;

            return !string.IsNullOrWhiteSpace(ApiEndpoint) &&
                   MaxTokens > 0 &&
                   Temperature >= 0.0 && Temperature <= 1.0 &&
                   TimeoutSeconds > 0 &&
                   RetryAttempts >= 0;
        }
    }
}
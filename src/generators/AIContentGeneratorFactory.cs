using System;
using System.Net.Http;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Factory for creating AI content generator instances
    /// </summary>
    public static class AIContentGeneratorFactory
    {
        /// <summary>
        /// Creates an AI content generator with the specified configuration
        /// </summary>
        /// <param name="config">AI service configuration</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>AI content generator instance</returns>
        public static IAIContentGenerator Create(AIServiceConfig config, ILogger logger)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

            return new AIContentGenerator(httpClient, config, logger);
        }

        /// <summary>
        /// Creates an AI content generator with default configuration (disabled)
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <returns>AI content generator instance with fallback behavior</returns>
        public static IAIContentGenerator CreateDefault(ILogger logger)
        {
            var defaultConfig = new AIServiceConfig
            {
                IsEnabled = false
            };

            return Create(defaultConfig, logger);
        }
    }
}
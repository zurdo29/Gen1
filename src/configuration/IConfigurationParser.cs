using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Interface for parsing configuration data
    /// </summary>
    public interface IConfigurationParser
    {
        /// <summary>
        /// Parses configuration from a dictionary
        /// </summary>
        /// <param name="configData">Configuration data</param>
        /// <returns>Parsed configuration object</returns>
        T ParseConfiguration<T>(Dictionary<string, object> configData) where T : class, new();

        /// <summary>
        /// Validates configuration data
        /// </summary>
        /// <param name="configData">Configuration data to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateConfiguration(Dictionary<string, object> configData);

        /// <summary>
        /// Validates a configuration object
        /// </summary>
        /// <param name="config">Configuration object to validate</param>
        /// <param name="errors">List of validation errors</param>
        /// <returns>True if configuration is valid</returns>
        bool ValidateConfig(ProceduralMiniGameGenerator.Models.GenerationConfig config, out System.Collections.Generic.List<string> errors);

        /// <summary>
        /// Parses configuration from a file
        /// </summary>
        /// <param name="jsonPath">Path to JSON configuration file</param>
        /// <returns>Parsed configuration object</returns>
        ProceduralMiniGameGenerator.Models.GenerationConfig ParseConfig(string jsonPath);

        /// <summary>
        /// Parses configuration from a JSON string
        /// </summary>
        /// <param name="jsonContent">JSON configuration content</param>
        /// <returns>Parsed configuration object</returns>
        ProceduralMiniGameGenerator.Models.GenerationConfig ParseConfigFromString(string jsonContent);

        /// <summary>
        /// Gets a default configuration
        /// </summary>
        /// <returns>Default configuration object</returns>
        ProceduralMiniGameGenerator.Models.GenerationConfig GetDefaultConfig();
    }
}
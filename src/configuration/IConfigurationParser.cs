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
    }
}
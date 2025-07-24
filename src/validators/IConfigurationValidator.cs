using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Validators
{
    /// <summary>
    /// Interface for validating generation configurations
    /// </summary>
    public interface IConfigurationValidator
    {
        /// <summary>
        /// Validates a generation configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <param name="errors">List of validation errors</param>
        /// <returns>True if configuration is valid</returns>
        bool ValidateConfig(GenerationConfig config, out List<string> errors);
        
        /// <summary>
        /// Validates configuration parameters for a specific algorithm
        /// </summary>
        /// <param name="algorithmName">Name of the algorithm</param>
        /// <param name="parameters">Algorithm parameters</param>
        /// <param name="errors">List of validation errors</param>
        /// <returns>True if parameters are valid</returns>
        bool ValidateAlgorithmParameters(string algorithmName, Dictionary<string, object> parameters, out List<string> errors);
        
        /// <summary>
        /// Gets default configuration values
        /// </summary>
        /// <returns>Default configuration</returns>
        GenerationConfig GetDefaultConfig();
    }
}
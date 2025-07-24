using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Main service interface for the procedural generation system
    /// </summary>
    public interface IProceduralGeneratorService
    {
        /// <summary>
        /// Generates a complete level from configuration
        /// </summary>
        /// <param name="configPath">Path to JSON configuration file</param>
        /// <returns>Generated level</returns>
        Level GenerateLevel(string configPath);
        
        /// <summary>
        /// Generates a level from configuration object
        /// </summary>
        /// <param name="config">Generation configuration</param>
        /// <returns>Generated level</returns>
        Level GenerateLevel(GenerationConfig config);
        
        /// <summary>
        /// Validates a configuration file
        /// </summary>
        /// <param name="configPath">Path to configuration file</param>
        /// <param name="errors">List of validation errors</param>
        /// <returns>True if configuration is valid</returns>
        bool ValidateConfiguration(string configPath, out List<string> errors);
        
        /// <summary>
        /// Gets available generation algorithms
        /// </summary>
        /// <returns>List of algorithm names</returns>
        List<string> GetAvailableAlgorithms();
        
        /// <summary>
        /// Gets available entity placement strategies
        /// </summary>
        /// <returns>List of placement strategy names</returns>
        List<string> GetAvailablePlacementStrategies();
        
        /// <summary>
        /// Exports a level to JSON format
        /// </summary>
        /// <param name="level">Level to export</param>
        /// <param name="outputPath">Output file path</param>
        /// <returns>True if export was successful</returns>
        bool ExportLevel(Level level, string outputPath);
        
        /// <summary>
        /// Imports a level from JSON format
        /// </summary>
        /// <param name="jsonPath">Path to JSON file</param>
        /// <returns>Imported level</returns>
        Level ImportLevel(string jsonPath);
    }
}
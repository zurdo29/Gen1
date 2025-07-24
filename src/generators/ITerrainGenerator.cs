using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Interface for terrain generation algorithms
    /// </summary>
    public interface ITerrainGenerator
    {
        /// <summary>
        /// Generates terrain based on configuration and seed
        /// </summary>
        /// <param name="config">Generation configuration</param>
        /// <param name="seed">Random seed for reproducible generation</param>
        /// <returns>Generated tile map</returns>
        TileMap GenerateTerrain(GenerationConfig config, int seed);
        
        /// <summary>
        /// Checks if this generator supports the given parameters
        /// </summary>
        /// <param name="parameters">Algorithm-specific parameters</param>
        /// <returns>True if parameters are supported</returns>
        bool SupportsParameters(Dictionary<string, object> parameters);
        
        /// <summary>
        /// Gets the name of this generation algorithm
        /// </summary>
        /// <returns>Algorithm name</returns>
        string GetAlgorithmName();
        
        /// <summary>
        /// Gets the default parameters for this algorithm
        /// </summary>
        /// <returns>Dictionary of default parameter values</returns>
        Dictionary<string, object> GetDefaultParameters();
        
        /// <summary>
        /// Validates algorithm-specific parameters
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>List of validation error messages</returns>
        List<string> ValidateParameters(Dictionary<string, object> parameters);
    }
}
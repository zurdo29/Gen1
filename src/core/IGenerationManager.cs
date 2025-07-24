using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Generators;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Interface for managing the level generation process
    /// </summary>
    public interface IGenerationManager
    {
        /// <summary>
        /// Generates a complete level based on the provided configuration
        /// </summary>
        /// <param name="config">Generation configuration</param>
        /// <returns>Generated level</returns>
        Level GenerateLevel(GenerationConfig config);
        
        /// <summary>
        /// Sets the random seed for reproducible generation
        /// </summary>
        /// <param name="seed">Random seed value</param>
        void SetSeed(int seed);
        
        /// <summary>
        /// Registers a terrain generation algorithm
        /// </summary>
        /// <param name="name">Algorithm name</param>
        /// <param name="generator">Terrain generator implementation</param>
        void RegisterGenerationAlgorithm(string name, ITerrainGenerator generator);
        
        /// <summary>
        /// Registers an entity placement algorithm
        /// </summary>
        /// <param name="name">Placer name</param>
        /// <param name="placer">Entity placer implementation</param>
        void RegisterEntityPlacer(string name, IEntityPlacer placer);
        
        /// <summary>
        /// Gets the list of available generation algorithms
        /// </summary>
        /// <returns>List of algorithm names</returns>
        List<string> GetAvailableAlgorithms();
        
        /// <summary>
        /// Gets the list of available entity placement strategies
        /// </summary>
        /// <returns>List of placement strategy names</returns>
        List<string> GetAvailablePlacementStrategies();
        
        /// <summary>
        /// Validates that a configuration can be used for generation
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateGenerationConfig(GenerationConfig config);
    }
}
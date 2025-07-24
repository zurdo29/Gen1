using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Main interface for managing the generation process
    /// </summary>
    public interface IGenerationManager
    {
        /// <summary>
        /// Generates a complete level based on configuration
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
        /// <param name="generator">Generator implementation</param>
        void RegisterGenerationAlgorithm(string name, ITerrainGenerator generator);
        
        /// <summary>
        /// Registers an entity placement algorithm
        /// </summary>
        /// <param name="name">Placer name</param>
        /// <param name="placer">Placer implementation</param>
        void RegisterEntityPlacer(string name, IEntityPlacer placer);
    }
}
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Factory interface for creating terrain generators
    /// </summary>
    public interface ITerrainGeneratorFactory
    {
        /// <summary>
        /// Creates a terrain generator based on the specified type
        /// </summary>
        /// <param name="generatorType">Type of generator to create</param>
        /// <returns>Terrain generator instance</returns>
        ITerrainGenerator CreateGenerator(string generatorType);

        /// <summary>
        /// Creates a terrain generator with specific configuration
        /// </summary>
        /// <param name="generatorType">Type of generator to create</param>
        /// <param name="config">Generator configuration</param>
        /// <returns>Terrain generator instance</returns>
        ITerrainGenerator CreateGenerator(string generatorType, GenerationConfig config);

        /// <summary>
        /// Gets available generator types
        /// </summary>
        /// <returns>Array of available generator type names</returns>
        string[] GetAvailableGeneratorTypes();
    }
}
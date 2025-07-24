using System.Collections.Generic;
using System.Numerics;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Interface for placing entities in generated levels
    /// </summary>
    public interface IEntityPlacer
    {
        /// <summary>
        /// Places entities on the terrain according to configuration
        /// </summary>
        /// <param name="terrain">The terrain to place entities on</param>
        /// <param name="config">Generation configuration</param>
        /// <param name="seed">Random seed for placement</param>
        /// <returns>List of placed entities</returns>
        List<Entity> PlaceEntities(TileMap terrain, GenerationConfig config, int seed);
        
        /// <summary>
        /// Checks if a position is valid for entity placement
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="terrain">Terrain map</param>
        /// <param name="existingEntities">Already placed entities</param>
        /// <returns>True if position is valid</returns>
        bool IsValidPosition(Vector2 position, TileMap terrain, List<Entity> existingEntities);
        
        /// <summary>
        /// Gets the name of this placement strategy
        /// </summary>
        /// <returns>Strategy name</returns>
        string GetStrategyName();
        
        /// <summary>
        /// Checks if this placer supports the given parameters
        /// </summary>
        /// <param name="parameters">Parameters to check</param>
        /// <returns>True if parameters are supported</returns>
        bool SupportsParameters(Dictionary<string, object> parameters);
    }
}
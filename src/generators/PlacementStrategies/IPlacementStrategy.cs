using System.Collections.Generic;
using System.Numerics;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Generators.PlacementStrategies
{
    /// <summary>
    /// Interface for entity placement strategies
    /// </summary>
    public interface IPlacementStrategy
    {
        /// <summary>
        /// Name of the placement strategy
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Finds a suitable position for entity placement
        /// </summary>
        /// <param name="terrain">Terrain map</param>
        /// <param name="config">Entity configuration</param>
        /// <param name="existingEntities">Already placed entities</param>
        /// <param name="entity">Entity to place</param>
        /// <param name="random">Random generator</param>
        /// <returns>Position if found, null otherwise</returns>
        Vector2? FindPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity, Core.IRandomGenerator random);
    }
}
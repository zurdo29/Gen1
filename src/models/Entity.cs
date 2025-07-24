using System.Collections.Generic;
using System.Numerics;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Base class for all entities in the game
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// Position of the entity in the level
        /// </summary>
        public Vector2 Position { get; set; }
        
        /// <summary>
        /// Type of the entity
        /// </summary>
        public EntityType Type { get; protected set; }
        
        /// <summary>
        /// Additional properties specific to this entity
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Checks if this entity can be placed at the specified position
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="terrain">Terrain map</param>
        /// <param name="entities">Other entities in the level</param>
        /// <returns>True if placement is valid</returns>
        public abstract bool CanPlaceAt(Vector2 position, TileMap terrain, List<Entity> entities);
    }
}
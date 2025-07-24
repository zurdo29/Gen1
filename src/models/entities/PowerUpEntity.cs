using System.Collections.Generic;
using System.Numerics;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Represents a power-up entity
    /// </summary>
    public class PowerUpEntity : Entity
    {
        public PowerUpEntity()
        {
            Type = EntityType.PowerUp;
        }
        
        /// <summary>
        /// Power-ups can be placed on walkable tiles, preferably in strategic locations
        /// </summary>
        public override bool CanPlaceAt(Vector2 position, TileMap terrain, List<Entity> entities)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            
            // Must be on walkable terrain
            if (!terrain.IsWalkable(x, y))
                return false;
            
            // Check if position is already occupied by another entity
            foreach (var entity in entities)
            {
                if (Vector2.Distance(entity.Position, position) < 1.0f)
                    return false;
            }
            
            return true;
        }
    }
}
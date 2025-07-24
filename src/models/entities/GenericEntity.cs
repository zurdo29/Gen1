using System.Collections.Generic;
using System.Numerics;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Generic entity implementation for unknown or custom entity types
    /// </summary>
    public class GenericEntity : Entity
    {
        public GenericEntity(EntityType entityType)
        {
            Type = entityType;
        }
        
        /// <summary>
        /// Generic entities can be placed on any walkable tile
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
using System.Collections.Generic;
using System.Numerics;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Represents a collectible item entity
    /// </summary>
    public class ItemEntity : Entity
    {
        public ItemEntity()
        {
            Type = EntityType.Item;
        }
        
        /// <summary>
        /// Items can be placed on walkable tiles
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
                if (Vector2.Distance(entity.Position, position) < 0.5f)
                    return false;
            }
            
            return true;
        }
    }
}
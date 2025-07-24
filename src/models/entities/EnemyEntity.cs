using System.Collections.Generic;
using System.Numerics;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Represents an enemy entity
    /// </summary>
    public class EnemyEntity : Entity
    {
        public EnemyEntity()
        {
            Type = EntityType.Enemy;
        }
        
        /// <summary>
        /// Enemy can be placed on walkable tiles, away from player spawn
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
                
                // Keep minimum distance from player spawn
                if (entity.Type == EntityType.Player && Vector2.Distance(entity.Position, position) < 3.0f)
                    return false;
            }
            
            return true;
        }
    }
}
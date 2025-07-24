using System.Collections.Generic;
using System.Numerics;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Represents an obstacle entity
    /// </summary>
    public class ObstacleEntity : Entity
    {
        public ObstacleEntity()
        {
            Type = EntityType.Obstacle;
        }
        
        /// <summary>
        /// Obstacles can be placed on walkable tiles but will block movement
        /// </summary>
        public override bool CanPlaceAt(Vector2 position, TileMap terrain, List<Entity> entities)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            
            // Must be on walkable terrain (obstacles will make it unwalkable)
            if (!terrain.IsWalkable(x, y))
                return false;
            
            // Check if position is already occupied by another entity
            foreach (var entity in entities)
            {
                if (Vector2.Distance(entity.Position, position) < 1.0f)
                    return false;
            }
            
            // Don't block critical paths - ensure there's still a path around
            // This is a simplified check; more sophisticated pathfinding could be added
            return true;
        }
    }
}
using System.Collections.Generic;
using System.Numerics;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Represents an exit point entity
    /// </summary>
    public class ExitEntity : Entity
    {
        public ExitEntity()
        {
            Type = EntityType.Exit;
        }
        
        /// <summary>
        /// Exit can be placed on walkable tiles, preferably away from player spawn
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
                if (Vector2.Distance(entity.Position, position) < 2.0f)
                    return false;
                
                // Keep reasonable distance from player spawn
                if (entity.Type == EntityType.Player && Vector2.Distance(entity.Position, position) < 5.0f)
                    return false;
            }
            
            return true;
        }
    }
}
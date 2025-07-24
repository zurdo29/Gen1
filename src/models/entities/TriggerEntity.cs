using System.Collections.Generic;
using System.Numerics;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Represents a trigger zone entity
    /// </summary>
    public class TriggerEntity : Entity
    {
        public TriggerEntity()
        {
            Type = EntityType.Trigger;
        }
        
        /// <summary>
        /// Triggers can be placed on walkable tiles and don't block movement
        /// </summary>
        public override bool CanPlaceAt(Vector2 position, TileMap terrain, List<Entity> entities)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            
            // Must be on walkable terrain
            if (!terrain.IsWalkable(x, y))
                return false;
            
            // Triggers can overlap with other entities (they're invisible zones)
            // But avoid placing multiple triggers in the same exact position
            foreach (var entity in entities)
            {
                if (entity.Type == EntityType.Trigger && Vector2.Distance(entity.Position, position) < 0.1f)
                    return false;
            }
            
            return true;
        }
    }
}
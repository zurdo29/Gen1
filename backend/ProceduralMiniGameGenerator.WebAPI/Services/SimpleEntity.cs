using System.Collections.Generic;
using System.Numerics;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Simple concrete implementation of Entity for web API
    /// </summary>
    public class SimpleEntity : Entity
    {
        public SimpleEntity(EntityType type, Vector2 position)
        {
            Type = type;
            Position = position;
        }

        public override bool CanPlaceAt(Vector2 position, TileMap terrain, List<Entity> entities)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            
            // Check bounds
            if (x < 0 || x >= terrain.Width || y < 0 || y >= terrain.Height)
                return false;
            
            // Check if tile is walkable
            var tileType = terrain.GetTile(x, y);
            if (tileType == TileType.Wall || tileType == TileType.Water)
                return false;
            
            // Check for existing entities at this position
            foreach (var entity in entities)
            {
                if (entity.Position.X == x && entity.Position.Y == y)
                    return false;
            }
            
            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Generators.PlacementStrategies
{
    /// <summary>
    /// Placement strategy that ensures entities are placed in reachable positions
    /// </summary>
    public class PathfindingPlacementStrategy : IPlacementStrategy
    {
        public string Name => "pathfinding";
        
        public Vector2? FindPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity, IRandomGenerator random)
        {
            var playerEntity = existingEntities.FirstOrDefault(e => e.Type == EntityType.Player);
            if (playerEntity == null)
            {
                // No player yet, use random placement
                return FindRandomReachablePosition(terrain, config, existingEntities, entity, random);
            }
            
            var reachablePositions = FindReachablePositions(terrain, playerEntity.Position, existingEntities, entity);
            reachablePositions = FilterByDistance(reachablePositions, config, existingEntities);
            
            if (reachablePositions.Count == 0)
                return null;
            
            return reachablePositions[random.Next(reachablePositions.Count)];
        }
        
        /// <summary>
        /// Finds all positions reachable from a starting point using flood fill
        /// </summary>
        private List<Vector2> FindReachablePositions(TileMap terrain, Vector2 startPosition, List<Entity> existingEntities, Entity entity)
        {
            var reachablePositions = new List<Vector2>();
            var visited = new bool[terrain.Width, terrain.Height];
            var queue = new Queue<Vector2>();
            
            queue.Enqueue(startPosition);
            visited[(int)startPosition.X, (int)startPosition.Y] = true;
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int x = (int)current.X;
                int y = (int)current.Y;
                
                // Check if this position is valid for the entity
                if (entity.CanPlaceAt(current, terrain, existingEntities))
                {
                    reachablePositions.Add(current);
                }
                
                // Add adjacent walkable positions to queue
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Skip current position
                        if (Math.Abs(dx) + Math.Abs(dy) > 1) continue; // Only 4-directional movement
                        
                        int newX = x + dx;
                        int newY = y + dy;
                        
                        if (newX >= 0 && newX < terrain.Width && newY >= 0 && newY < terrain.Height &&
                            !visited[newX, newY] && terrain.IsWalkable(newX, newY))
                        {
                            visited[newX, newY] = true;
                            queue.Enqueue(new Vector2(newX, newY));
                        }
                    }
                }
            }
            
            return reachablePositions;
        }
        
        /// <summary>
        /// Finds a random reachable position when no player exists yet
        /// </summary>
        private Vector2? FindRandomReachablePosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity, IRandomGenerator random)
        {
            var validPositions = new List<Vector2>();
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var position = new Vector2(x, y);
                    if (entity.CanPlaceAt(position, terrain, existingEntities))
                    {
                        validPositions.Add(position);
                    }
                }
            }
            
            validPositions = FilterByDistance(validPositions, config, existingEntities);
            
            if (validPositions.Count == 0)
                return null;
            
            return validPositions[random.Next(validPositions.Count)];
        }
        
        /// <summary>
        /// Filters positions based on distance constraints
        /// </summary>
        private List<Vector2> FilterByDistance(List<Vector2> positions, EntityConfig config, List<Entity> existingEntities)
        {
            var playerEntity = existingEntities.FirstOrDefault(e => e.Type == EntityType.Player);
            
            return positions.Where(pos =>
            {
                // Check minimum distance from other entities
                foreach (var entity in existingEntities)
                {
                    if (Vector2.Distance(entity.Position, pos) < config.MinDistance)
                        return false;
                }
                
                // Check maximum distance from player
                if (playerEntity != null && config.MaxDistanceFromPlayer < float.MaxValue)
                {
                    if (Vector2.Distance(playerEntity.Position, pos) > config.MaxDistanceFromPlayer)
                        return false;
                }
                
                return true;
            }).ToList();
        }
    }
}
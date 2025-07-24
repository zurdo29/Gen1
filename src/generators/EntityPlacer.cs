using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Implements entity placement algorithms for generated levels
    /// </summary>
    public class EntityPlacer : IEntityPlacer
    {
        private readonly IRandomGenerator _random;
        private readonly ISimpleLoggerService _logger;
        
        public EntityPlacer(IRandomGenerator random, ISimpleLoggerService logger = null)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _logger = logger;
        }
        
        /// <summary>
        /// Places entities on the terrain according to configuration
        /// </summary>
        public List<Entity> PlaceEntities(TileMap terrain, GenerationConfig config, int seed)
        {
            if (terrain == null) throw new ArgumentNullException(nameof(terrain));
            if (config == null) throw new ArgumentNullException(nameof(config));
            
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                var totalEntitiesRequested = config.Entities?.Sum(e => e.Count) ?? 0;
                var walkableTiles = CalculateWalkableTiles(terrain);
                
                _logger?.LogInfo("Starting entity placement", new {
                    OperationId = operationId,
                    TerrainSize = $"{terrain.Width}x{terrain.Height}",
                    WalkableTiles = walkableTiles,
                    TotalEntitiesRequested = totalEntitiesRequested,
                    EntityConfigs = config.Entities?.Select(e => new { 
                        Type = e.Type.ToString(), 
                        Count = e.Count, 
                        Strategy = e.PlacementStrategy 
                    }),
                    Seed = seed
                });
                
                // Log placement density analysis
                var placementDensity = walkableTiles > 0 ? (double)totalEntitiesRequested / walkableTiles : 0;
                if (placementDensity > 0.1) // More than 10% of walkable tiles
                {
                    _logger?.LogWarning("High entity placement density detected", new {
                        OperationId = operationId,
                        PlacementDensity = placementDensity,
                        WalkableTiles = walkableTiles,
                        EntitiesRequested = totalEntitiesRequested
                    });
                }
                
                _random.SetSeed(seed);
                var placedEntities = new List<Entity>();
                
                // First, place the player spawn point
                var playerStopwatch = Stopwatch.StartNew();
                var playerEntity = PlacePlayerSpawn(terrain, placedEntities);
                if (playerEntity != null)
                {
                    placedEntities.Add(playerEntity);
                }
                playerStopwatch.Stop();
                
                _logger?.LogPerformance("PlayerSpawnPlacement", playerStopwatch.Elapsed, new {
                    PlayerPlaced = playerEntity != null
                });
                
                // Then place other entities according to configuration
                foreach (var entityConfig in config.Entities)
                {
                    var entityStopwatch = Stopwatch.StartNew();
                    
                    _logger?.LogInfo($"Placing {entityConfig.Type} entities", new {
                        OperationId = operationId,
                        EntityType = entityConfig.Type.ToString(),
                        Count = entityConfig.Count,
                        Strategy = entityConfig.PlacementStrategy,
                        MinDistance = entityConfig.MinDistance,
                        MaxDistanceFromPlayer = entityConfig.MaxDistanceFromPlayer
                    });
                    
                    var initialCount = placedEntities.Count;
                    var entities = PlaceEntitiesOfType(terrain, entityConfig, placedEntities);
                    placedEntities.AddRange(entities);
                    
                    entityStopwatch.Stop();
                    
                    _logger?.LogPerformance($"EntityPlacement_{entityConfig.Type}", entityStopwatch.Elapsed, new {
                        EntityType = entityConfig.Type.ToString(),
                        Requested = entityConfig.Count,
                        Placed = entities.Count,
                        SuccessRate = entityConfig.Count > 0 ? (double)entities.Count / entityConfig.Count : 1.0,
                        Strategy = entityConfig.PlacementStrategy
                    });
                }
                
                stopwatch.Stop();
                
                // Calculate placement statistics
                var placementStats = CalculatePlacementStatistics(placedEntities, config.Entities);
                
                _logger?.LogGeneration(operationId, "EntityPlacement", stopwatch.Elapsed, new {
                    TerrainSize = $"{terrain.Width}x{terrain.Height}",
                    EntitiesRequested = totalEntitiesRequested,
                    EntitiesPlaced = placedEntities.Count,
                    PlacementSuccessRate = totalEntitiesRequested > 0 ? (double)placedEntities.Count / totalEntitiesRequested : 1.0,
                    PlacementStatistics = placementStats,
                    Seed = seed
                });
                
                // Log any placement failures
                var failedPlacements = totalEntitiesRequested - placedEntities.Count;
                if (failedPlacements > 0)
                {
                    _logger?.LogWarning("Some entities could not be placed", new {
                        OperationId = operationId,
                        FailedPlacements = failedPlacements,
                        SuccessfulPlacements = placedEntities.Count,
                        FailureRate = (double)failedPlacements / totalEntitiesRequested
                    });
                }
                
                _logger?.LogInfo("Entity placement completed", new {
                    OperationId = operationId,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    EntitiesPlaced = placedEntities.Count,
                    EntitiesRequested = totalEntitiesRequested,
                    PlacementStats = placementStats
                });
                
                return placedEntities;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogErrorAsync(ex, "Entity placement failed", new {
                    OperationId = operationId,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    TerrainSize = $"{terrain.Width}x{terrain.Height}",
                    Seed = seed,
                    EntityConfigs = config.Entities?.Select(e => new { 
                        Type = e.Type.ToString(), 
                        Count = e.Count 
                    })
                }).Wait();
                throw;
            }
        }
        
        /// <summary>
        /// Checks if a position is valid for entity placement
        /// </summary>
        public bool IsValidPosition(Vector2 position, TileMap terrain, List<Entity> existingEntities)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            
            // Check bounds
            if (x < 0 || x >= terrain.Width || y < 0 || y >= terrain.Height)
                return false;
            
            // Check if terrain is walkable
            if (!terrain.IsWalkable(x, y))
                return false;
            
            // Check for conflicts with existing entities
            foreach (var entity in existingEntities)
            {
                if (Vector2.Distance(entity.Position, position) < 1.0f)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Places the player spawn point
        /// </summary>
        private Entity? PlacePlayerSpawn(TileMap terrain, List<Entity> existingEntities)
        {
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            
            // Try to place player in a good starting position
            var validPositions = GetValidPositions(terrain, existingEntities, playerEntity);
            
            if (validPositions.Count == 0)
                return null;
            
            // Prefer positions near the center or edges based on level design
            var centerX = terrain.Width / 2f;
            var centerY = terrain.Height / 2f;
            var center = new Vector2(centerX, centerY);
            
            // Sort by distance from center (prefer center for player spawn)
            validPositions.Sort((a, b) => Vector2.Distance(a, center).CompareTo(Vector2.Distance(b, center)));
            
            playerEntity.Position = validPositions[0];
            return playerEntity;
        }
        
        /// <summary>
        /// Places entities of a specific type according to configuration
        /// </summary>
        private List<Entity> PlaceEntitiesOfType(TileMap terrain, EntityConfig config, List<Entity> existingEntities)
        {
            var placedEntities = new List<Entity>();
            
            for (int i = 0; i < config.Count; i++)
            {
                var entity = EntityFactory.CreateEntity(config.Type, config.Properties);
                var position = FindPlacementPosition(terrain, config, existingEntities, entity);
                
                if (position.HasValue)
                {
                    entity.Position = position.Value;
                    placedEntities.Add(entity);
                    existingEntities.Add(entity); // Update existing entities for next placement
                }
            }
            
            return placedEntities;
        }
        
        /// <summary>
        /// Finds a suitable placement position based on strategy
        /// </summary>
        private Vector2? FindPlacementPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity)
        {
            return config.PlacementStrategy.ToLower() switch
            {
                "random" => FindRandomPosition(terrain, config, existingEntities, entity),
                "clustered" => FindClusteredPosition(terrain, config, existingEntities, entity),
                "spread" => FindSpreadPosition(terrain, config, existingEntities, entity),
                "near_walls" => FindNearWallsPosition(terrain, config, existingEntities, entity),
                "center" => FindCenterPosition(terrain, config, existingEntities, entity),
                "far_from_player" => FindFarFromPlayerPosition(terrain, config, existingEntities, entity),
                "corners" => FindCornersPosition(terrain, config, existingEntities, entity),
                _ => FindRandomPosition(terrain, config, existingEntities, entity)
            };
        }
        
        /// <summary>
        /// Finds a random valid position
        /// </summary>
        private Vector2? FindRandomPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity)
        {
            var validPositions = GetValidPositions(terrain, existingEntities, entity);
            validPositions = FilterByDistance(validPositions, config, existingEntities);
            
            if (validPositions.Count == 0)
                return null;
            
            return validPositions[_random.Next(validPositions.Count)];
        }
        
        /// <summary>
        /// Finds a position near other entities of the same type (clustered)
        /// </summary>
        private Vector2? FindClusteredPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity)
        {
            var sameTypeEntities = existingEntities.Where(e => e.Type == entity.Type).ToList();
            
            if (sameTypeEntities.Count == 0)
            {
                // No entities of same type yet, place randomly
                return FindRandomPosition(terrain, config, existingEntities, entity);
            }
            
            // Find positions near existing entities of same type
            var candidatePositions = new List<Vector2>();
            
            foreach (var existingEntity in sameTypeEntities)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    for (int dy = -3; dy <= 3; dy++)
                    {
                        var pos = new Vector2(existingEntity.Position.X + dx, existingEntity.Position.Y + dy);
                        if (entity.CanPlaceAt(pos, terrain, existingEntities))
                        {
                            candidatePositions.Add(pos);
                        }
                    }
                }
            }
            
            candidatePositions = FilterByDistance(candidatePositions, config, existingEntities);
            
            if (candidatePositions.Count == 0)
                return FindRandomPosition(terrain, config, existingEntities, entity);
            
            return candidatePositions[_random.Next(candidatePositions.Count)];
        }
        
        /// <summary>
        /// Finds a position spread out from other entities
        /// </summary>
        private Vector2? FindSpreadPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity)
        {
            var validPositions = GetValidPositions(terrain, existingEntities, entity);
            validPositions = FilterByDistance(validPositions, config, existingEntities);
            
            if (validPositions.Count == 0)
                return null;
            
            // Sort by distance from nearest entity (prefer farther positions)
            validPositions.Sort((a, b) => 
            {
                var minDistA = existingEntities.Count > 0 ? existingEntities.Min(e => Vector2.Distance(e.Position, a)) : float.MaxValue;
                var minDistB = existingEntities.Count > 0 ? existingEntities.Min(e => Vector2.Distance(e.Position, b)) : float.MaxValue;
                return minDistB.CompareTo(minDistA); // Descending order
            });
            
            // Pick from the top 25% of spread positions
            int topCount = Math.Max(1, validPositions.Count / 4);
            return validPositions[_random.Next(topCount)];
        }
        
        /// <summary>
        /// Finds a position near walls
        /// </summary>
        private Vector2? FindNearWallsPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity)
        {
            var validPositions = GetValidPositions(terrain, existingEntities, entity);
            validPositions = FilterByDistance(validPositions, config, existingEntities);
            
            // Filter positions that are near walls
            var nearWallPositions = validPositions.Where(pos =>
            {
                int x = (int)pos.X;
                int y = (int)pos.Y;
                
                // Check adjacent tiles for walls
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (!terrain.IsWalkable(x + dx, y + dy))
                            return true;
                    }
                }
                return false;
            }).ToList();
            
            if (nearWallPositions.Count == 0)
                return FindRandomPosition(terrain, config, existingEntities, entity);
            
            return nearWallPositions[_random.Next(nearWallPositions.Count)];
        }
        
        /// <summary>
        /// Finds a position near the center of the map
        /// </summary>
        private Vector2? FindCenterPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity)
        {
            var validPositions = GetValidPositions(terrain, existingEntities, entity);
            validPositions = FilterByDistance(validPositions, config, existingEntities);
            
            if (validPositions.Count == 0)
                return null;
            
            var center = new Vector2(terrain.Width / 2f, terrain.Height / 2f);
            
            // Sort by distance from center (prefer closer to center)
            validPositions.Sort((a, b) => Vector2.Distance(a, center).CompareTo(Vector2.Distance(b, center)));
            
            // Pick from the top 25% of center positions
            int topCount = Math.Max(1, validPositions.Count / 4);
            return validPositions[_random.Next(topCount)];
        }
        
        /// <summary>
        /// Finds a position far from the player
        /// </summary>
        private Vector2? FindFarFromPlayerPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity)
        {
            var playerEntity = existingEntities.FirstOrDefault(e => e.Type == EntityType.Player);
            if (playerEntity == null)
                return FindRandomPosition(terrain, config, existingEntities, entity);
            
            var validPositions = GetValidPositions(terrain, existingEntities, entity);
            validPositions = FilterByDistance(validPositions, config, existingEntities);
            
            if (validPositions.Count == 0)
                return null;
            
            // Sort by distance from player (prefer farther positions)
            validPositions.Sort((a, b) => 
                Vector2.Distance(b, playerEntity.Position).CompareTo(Vector2.Distance(a, playerEntity.Position)));
            
            // Pick from the top 25% of far positions
            int topCount = Math.Max(1, validPositions.Count / 4);
            return validPositions[_random.Next(topCount)];
        }
        
        /// <summary>
        /// Finds a position in the corners of the map
        /// </summary>
        private Vector2? FindCornersPosition(TileMap terrain, EntityConfig config, List<Entity> existingEntities, Entity entity)
        {
            var validPositions = GetValidPositions(terrain, existingEntities, entity);
            validPositions = FilterByDistance(validPositions, config, existingEntities);
            
            if (validPositions.Count == 0)
                return null;
            
            // Define corner regions (each corner is 1/4 of the map)
            var corners = new[]
            {
                new Vector2(terrain.Width * 0.25f, terrain.Height * 0.25f), // Top-left
                new Vector2(terrain.Width * 0.75f, terrain.Height * 0.25f), // Top-right
                new Vector2(terrain.Width * 0.25f, terrain.Height * 0.75f), // Bottom-left
                new Vector2(terrain.Width * 0.75f, terrain.Height * 0.75f)  // Bottom-right
            };
            
            // Find positions closest to any corner
            var cornerPositions = validPositions.Where(pos =>
            {
                var minDistToCorner = corners.Min(corner => Vector2.Distance(pos, corner));
                return minDistToCorner <= Math.Min(terrain.Width, terrain.Height) * 0.3f; // Within 30% of map size from corner
            }).ToList();
            
            if (cornerPositions.Count == 0)
                return FindRandomPosition(terrain, config, existingEntities, entity);
            
            return cornerPositions[_random.Next(cornerPositions.Count)];
        }
        
        /// <summary>
        /// Gets all valid positions for an entity
        /// </summary>
        private List<Vector2> GetValidPositions(TileMap terrain, List<Entity> existingEntities, Entity entity)
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
            
            return validPositions;
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
        
        /// <summary>
        /// Calculates walkable tiles in terrain
        /// </summary>
        private int CalculateWalkableTiles(TileMap terrain)
        {
            int walkableCount = 0;
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    if (terrain.IsWalkable(x, y))
                        walkableCount++;
                }
            }
            return walkableCount;
        }
        
        /// <summary>
        /// Calculates placement statistics for logging
        /// </summary>
        private Dictionary<string, object> CalculatePlacementStatistics(List<Entity> placedEntities, List<EntityConfig>? entityConfigs)
        {
            var entityCounts = placedEntities.GroupBy(e => e.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            
            var requestedCounts = entityConfigs?.ToDictionary(
                e => e.Type.ToString(), 
                e => e.Count) ?? new Dictionary<string, int>();
            
            var placementRates = new Dictionary<string, object>();
            foreach (var kvp in requestedCounts)
            {
                var placed = entityCounts.GetValueOrDefault(kvp.Key, 0);
                placementRates[kvp.Key] = new
                {
                    Requested = kvp.Value,
                    Placed = placed,
                    SuccessRate = kvp.Value > 0 ? (double)placed / kvp.Value : 1.0
                };
            }
            
            // Calculate spatial distribution
            var spatialStats = CalculateSpatialDistribution(placedEntities);
            
            return new Dictionary<string, object>
            {
                ["TotalPlaced"] = placedEntities.Count,
                ["EntityComposition"] = entityCounts,
                ["PlacementRates"] = placementRates,
                ["SpatialDistribution"] = spatialStats
            };
        }
        
        /// <summary>
        /// Calculates spatial distribution statistics
        /// </summary>
        private Dictionary<string, object> CalculateSpatialDistribution(List<Entity> entities)
        {
            if (!entities.Any())
                return new Dictionary<string, object>();

            var positions = entities.Select(e => e.Position).ToList();
            var minX = positions.Min(p => p.X);
            var maxX = positions.Max(p => p.X);
            var minY = positions.Min(p => p.Y);
            var maxY = positions.Max(p => p.Y);
            
            var centerX = positions.Average(p => p.X);
            var centerY = positions.Average(p => p.Y);
            
            // Calculate average distance between entities
            var totalDistance = 0.0;
            var pairCount = 0;
            
            for (int i = 0; i < entities.Count; i++)
            {
                for (int j = i + 1; j < entities.Count; j++)
                {
                    totalDistance += Vector2.Distance(entities[i].Position, entities[j].Position);
                    pairCount++;
                }
            }
            
            var averageDistance = pairCount > 0 ? totalDistance / pairCount : 0.0;
            
            return new Dictionary<string, object>
            {
                ["Bounds"] = new { MinX = minX, MaxX = maxX, MinY = minY, MaxY = maxY },
                ["Center"] = new { X = centerX, Y = centerY },
                ["Spread"] = new { Width = maxX - minX, Height = maxY - minY },
                ["AverageInterEntityDistance"] = averageDistance
            };
        }
    }
}
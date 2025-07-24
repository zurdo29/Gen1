using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Entity placer with comprehensive logging integration
    /// </summary>
    public class LoggingEntityPlacer : IEntityPlacer
    {
        private readonly IEntityPlacer _basePlacer;
        private readonly ILoggerService _loggerService;

        public LoggingEntityPlacer(IEntityPlacer basePlacer, ILoggerService loggerService)
        {
            _basePlacer = basePlacer ?? throw new ArgumentNullException(nameof(basePlacer));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Places entities with comprehensive logging
        /// </summary>
        public List<Entity> PlaceEntities(TileMap terrain, GenerationConfig config, int seed)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                var totalEntitiesRequested = config.Entities?.Sum(e => e.Count) ?? 0;
                var walkableTiles = CalculateWalkableTiles(terrain);
                
                LogSafely(LogLevel.Information, 
                    "Starting entity placement", 
                    new { 
                        OperationId = operationId,
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        WalkableTiles = walkableTiles,
                        TotalEntitiesRequested = totalEntitiesRequested,
                        EntityConfigs = config.Entities?.Select(e => new { 
                            Type = e.Type.ToString(), 
                            Count = e.Count, 
                            Strategy = e.PlacementStrategy 
                        }),
                        Seed = seed,
                        Operation = "EntityPlacement"
                    });
                
                // Log placement density analysis
                var placementDensity = walkableTiles > 0 ? (double)totalEntitiesRequested / walkableTiles : 0;
                if (placementDensity > 0.1) // More than 10% of walkable tiles
                {
                    LogSafely(LogLevel.Warning, 
                        "High entity placement density detected", 
                        new { 
                            OperationId = operationId,
                            PlacementDensity = placementDensity,
                            WalkableTiles = walkableTiles,
                            EntitiesRequested = totalEntitiesRequested
                        });
                }

                // Log individual entity type placement
                var placedEntities = new List<Entity>();
                
                if (config.Entities != null)
                {
                    foreach (var entityConfig in config.Entities)
                    {
                        var entityStopwatch = Stopwatch.StartNew();
                        
                        LogSafely(LogLevel.Debug, 
                            $"Placing {entityConfig.Type} entities", 
                            new { 
                                OperationId = operationId,
                                EntityType = entityConfig.Type.ToString(),
                                Count = entityConfig.Count,
                                Strategy = entityConfig.PlacementStrategy,
                                MinDistance = entityConfig.MinDistance,
                                MaxDistanceFromPlayer = entityConfig.MaxDistanceFromPlayer
                            });

                        var initialCount = placedEntities.Count;
                        var entities = _basePlacer.PlaceEntities(terrain, config, seed);
                        var newEntities = entities.Skip(initialCount).ToList();
                        
                        entityStopwatch.Stop();
                        
                        LogSafely(LogLevel.Debug, 
                            $"Completed placing {entityConfig.Type} entities", 
                            new { 
                                OperationId = operationId,
                                EntityType = entityConfig.Type.ToString(),
                                Requested = entityConfig.Count,
                                Placed = newEntities.Count,
                                SuccessRate = entityConfig.Count > 0 ? (double)newEntities.Count / entityConfig.Count : 1.0,
                                DurationMs = entityStopwatch.ElapsedMilliseconds,
                                Strategy = entityConfig.PlacementStrategy
                            });
                        
                        placedEntities = entities;
                    }
                }
                else
                {
                    placedEntities = _basePlacer.PlaceEntities(terrain, config, seed);
                }
                
                stopwatch.Stop();
                
                // Calculate placement statistics
                var placementStats = CalculatePlacementStatistics(placedEntities, config.Entities);
                
                LogGenerationSafely(
                    operationId, 
                    "EntityPlacement", 
                    stopwatch.Elapsed,
                    new { 
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        EntitiesRequested = totalEntitiesRequested,
                        EntitiesPlaced = placedEntities.Count,
                        PlacementSuccessRate = totalEntitiesRequested > 0 ? (double)placedEntities.Count / totalEntitiesRequested : 1.0,
                        PlacementStatistics = placementStats,
                        Seed = seed
                    });
                
                LogPerformanceSafely(
                    "EntityPlacement",
                    stopwatch.Elapsed,
                    new {
                        EntitiesPerSecond = placedEntities.Count / stopwatch.Elapsed.TotalSeconds,
                        PlacementEfficiency = CalculatePlacementEfficiency(placedEntities, terrain),
                        MemoryEstimate = EstimateEntityMemoryUsage(placedEntities),
                        PlacementDensity = placementDensity
                    });
                
                // Log any placement failures
                var failedPlacements = totalEntitiesRequested - placedEntities.Count;
                if (failedPlacements > 0)
                {
                    LogSafely(LogLevel.Warning, 
                        "Some entities could not be placed", 
                        new { 
                            OperationId = operationId,
                            FailedPlacements = failedPlacements,
                            SuccessfulPlacements = placedEntities.Count,
                            FailureRate = (double)failedPlacements / totalEntitiesRequested,
                            PossibleCauses = new[] { "Insufficient walkable space", "Distance constraints too restrictive", "Placement strategy conflicts" }
                        });
                }
                
                LogSafely(LogLevel.Information, 
                    "Entity placement completed", 
                    new { 
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
                LogErrorSafely(ex, 
                    "Entity placement failed", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        Seed = seed,
                        EntityConfigs = config.Entities?.Select(e => new { 
                            Type = e.Type.ToString(), 
                            Count = e.Count 
                        })
                    });
                throw;
            }
        }

        /// <summary>
        /// Checks if a position is valid for entity placement with logging
        /// </summary>
        public bool IsValidPosition(Vector2 position, TileMap terrain, List<Entity> existingEntities)
        {
            try
            {
                var isValid = _basePlacer.IsValidPosition(position, terrain, existingEntities);
                
                // Only log invalid positions for debugging purposes
                if (!isValid)
                {
                    LogSafely(LogLevel.Trace, 
                        "Invalid entity position detected", 
                        new { 
                            Position = new { X = position.X, Y = position.Y },
                            TerrainSize = $"{terrain.Width}x{terrain.Height}",
                            ExistingEntityCount = existingEntities.Count,
                            Operation = "PositionValidation"
                        });
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                LogErrorSafely(ex, 
                    "Position validation failed", 
                    new { 
                        Position = new { X = position.X, Y = position.Y },
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        ExistingEntityCount = existingEntities.Count
                    });
                return false;
            }
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

        /// <summary>
        /// Calculates placement efficiency
        /// </summary>
        private double CalculatePlacementEfficiency(List<Entity> entities, TileMap terrain)
        {
            if (!entities.Any()) return 1.0;
            
            var walkableTiles = CalculateWalkableTiles(terrain);
            if (walkableTiles == 0) return 0.0;
            
            return Math.Min(1.0, (double)entities.Count / walkableTiles);
        }

        /// <summary>
        /// Estimates memory usage for entities
        /// </summary>
        private long EstimateEntityMemoryUsage(List<Entity> entities)
        {
            return entities.Count * 64; // 64 bytes per entity estimate
        }

        /// <summary>
        /// Safely logs a message without throwing exceptions
        /// </summary>
        private void LogSafely(LogLevel level, string message, object context = null)
        {
            try
            {
                _loggerService.LogAsync(level, message, context).Wait();
            }
            catch
            {
                // Ignore logging errors to prevent disrupting the main operation
            }
        }

        /// <summary>
        /// Safely logs generation metrics without throwing exceptions
        /// </summary>
        private void LogGenerationSafely(string configId, string step, TimeSpan duration, object metadata = null)
        {
            try
            {
                _loggerService.LogGenerationAsync(configId, step, duration, metadata).Wait();
            }
            catch
            {
                // Ignore logging errors to prevent disrupting the main operation
            }
        }

        /// <summary>
        /// Safely logs performance metrics without throwing exceptions
        /// </summary>
        private void LogPerformanceSafely(string operation, TimeSpan duration, object metrics = null)
        {
            try
            {
                _loggerService.LogPerformanceAsync(operation, duration, metrics).Wait();
            }
            catch
            {
                // Ignore logging errors to prevent disrupting the main operation
            }
        }

        /// <summary>
        /// Safely logs errors without throwing exceptions
        /// </summary>
        private void LogErrorSafely(Exception exception, string context, object additionalData = null)
        {
            try
            {
                _loggerService.LogErrorAsync(exception, context, additionalData).Wait();
            }
            catch
            {
                // Ignore logging errors to prevent disrupting the main operation
            }
        }
    }
}
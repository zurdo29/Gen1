using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Level assembler with comprehensive logging integration
    /// </summary>
    public class LoggingLevelAssembler : ILevelAssembler
    {
        private readonly ILevelAssembler _baseAssembler;
        private readonly ILoggerService _loggerService;

        public LoggingLevelAssembler(ILevelAssembler baseAssembler, ILoggerService loggerService)
        {
            _baseAssembler = baseAssembler ?? throw new ArgumentNullException(nameof(baseAssembler));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Assembles level with comprehensive logging
        /// </summary>
        public Level AssembleLevel(TileMap terrain, List<Entity> entities, GenerationConfig config)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting level assembly", 
                    new { 
                        OperationId = operationId,
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        EntityCount = entities.Count,
                        Algorithm = config.GenerationAlgorithm,
                        Seed = config.Seed,
                        Operation = "LevelAssembly"
                    });
                
                // Log pre-assembly validation
                var validationResults = ValidateAssemblyInputs(terrain, entities, config);
                if (validationResults != null && validationResults.Any())
                {
                    LogSafely(LogLevel.Warning, 
                        "Level assembly validation warnings", 
                        new { 
                            OperationId = operationId,
                            ValidationWarnings = validationResults
                        });
                }

                // Log memory estimation
                var estimatedMemory = EstimateLevelMemoryUsage(terrain, entities);
                LogSafely(LogLevel.Debug, 
                    "Level assembly memory estimation", 
                    new { 
                        OperationId = operationId,
                        EstimatedMemoryBytes = estimatedMemory,
                        EstimatedMemoryMB = estimatedMemory / (1024.0 * 1024.0)
                    });

                var level = _baseAssembler.AssembleLevel(terrain, entities, config);
                
                stopwatch.Stop();
                
                // Calculate assembly statistics
                var assemblyStats = CalculateAssemblyStatistics(level, terrain, entities, config);
                
                LogGenerationSafely(
                    operationId, 
                    "LevelAssembly", 
                    stopwatch.Elapsed,
                    new { 
                        LevelName = level.Name,
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        EntityCount = entities.Count,
                        Algorithm = config.GenerationAlgorithm,
                        Seed = config.Seed,
                        AssemblyStatistics = assemblyStats
                    });
                
                LogPerformanceSafely(
                    "LevelAssembly",
                    stopwatch.Elapsed,
                    new {
                        TilesProcessed = terrain.Width * terrain.Height,
                        EntitiesProcessed = entities.Count,
                        ProcessingRate = (terrain.Width * terrain.Height + entities.Count) / stopwatch.Elapsed.TotalSeconds,
                        MemoryEstimate = estimatedMemory,
                        MetadataKeys = level.Metadata?.Count ?? 0
                    });
                
                LogSafely(LogLevel.Information, 
                    "Level assembly completed successfully", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level.Name,
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        EntityCount = entities.Count,
                        MetadataKeys = level.Metadata?.Keys.ToList(),
                        AssemblyStats = assemblyStats
                    });
                
                return level;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Level assembly failed", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        EntityCount = entities.Count,
                        Algorithm = config.GenerationAlgorithm,
                        Seed = config.Seed
                    });
                throw;
            }
        }

        /// <summary>
        /// Applies visual theme with logging
        /// </summary>
        public void ApplyVisualTheme(Level level, VisualTheme theme)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting visual theme application", 
                    new { 
                        OperationId = operationId,
                        LevelName = level.Name,
                        ThemeName = theme.Name,
                        TerrainSize = $"{level.Terrain.Width}x{level.Terrain.Height}",
                        EntityCount = level.Entities.Count,
                        Operation = "VisualThemeApplication"
                    });

                // Log theme details
                LogSafely(LogLevel.Debug, 
                    "Visual theme details", 
                    new { 
                        OperationId = operationId,
                        ThemeName = theme.Name,
                        ColorCount = theme.Colors?.Count ?? 0,
                        TileSpriteCount = theme.TileSprites?.Count ?? 0,
                        EntitySpriteCount = theme.EntitySprites?.Count ?? 0,
                        PropertiesCount = theme.Properties?.Count ?? 0
                    });

                _baseAssembler.ApplyVisualTheme(level, theme);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "VisualThemeApplication",
                    stopwatch.Elapsed,
                    new {
                        ThemeName = theme.Name,
                        EntitiesProcessed = level.Entities.Count,
                        TilesProcessed = level.Terrain.Width * level.Terrain.Height,
                        ProcessingRate = (level.Entities.Count + level.Terrain.Width * level.Terrain.Height) / stopwatch.Elapsed.TotalSeconds
                    });
                
                LogSafely(LogLevel.Information, 
                    "Visual theme application completed successfully", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level.Name,
                        ThemeName = theme.Name,
                        EntitiesThemed = level.Entities.Count
                    });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Visual theme application failed", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level.Name,
                        ThemeName = theme.Name
                    });
                throw;
            }
        }

        /// <summary>
        /// Validates assembly inputs and returns warnings
        /// </summary>
        private List<string> ValidateAssemblyInputs(TileMap terrain, List<Entity> entities, GenerationConfig config)
        {
            var warnings = new List<string>();
            
            // Validate terrain
            if (terrain.Width <= 0 || terrain.Height <= 0)
                warnings.Add("Invalid terrain dimensions");
            
            // Validate entities
            var entitiesOutOfBounds = entities.Where(e => 
                e.Position.X < 0 || e.Position.X >= terrain.Width ||
                e.Position.Y < 0 || e.Position.Y >= terrain.Height).Count();
            
            if (entitiesOutOfBounds > 0)
                warnings.Add($"{entitiesOutOfBounds} entities are positioned outside terrain bounds");

            // Check for overlapping entities
            var overlappingEntities = 0;
            for (int i = 0; i < entities.Count; i++)
            {
                for (int j = i + 1; j < entities.Count; j++)
                {
                    if (entities[i].Position == entities[j].Position)
                        overlappingEntities++;
                }
            }
            
            if (overlappingEntities > 0)
                warnings.Add($"{overlappingEntities} pairs of entities are overlapping");

            // Check walkable area utilization
            var walkableTiles = CalculateWalkableTiles(terrain);
            var entityDensity = walkableTiles > 0 ? (double)entities.Count / walkableTiles : 0;
            
            if (entityDensity > 0.5)
                warnings.Add($"High entity density detected: {entityDensity:P1} of walkable tiles occupied");
            
            return warnings;
        }

        /// <summary>
        /// Calculates assembly statistics for logging
        /// </summary>
        private Dictionary<string, object> CalculateAssemblyStatistics(Level level, TileMap terrain, List<Entity> entities, GenerationConfig config)
        {
            var stats = new Dictionary<string, object>
            {
                ["LevelName"] = level.Name,
                ["TerrainDimensions"] = new { Width = terrain.Width, Height = terrain.Height },
                ["TotalTiles"] = terrain.Width * terrain.Height,
                ["EntityCount"] = entities.Count,
                ["MetadataKeys"] = level.Metadata?.Keys.ToList() ?? new List<string>()
            };
            
            // Calculate entity distribution
            var entityTypes = entities.GroupBy(e => e.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            stats["EntityDistribution"] = entityTypes;

            // Calculate terrain composition
            var terrainComposition = CalculateTerrainComposition(terrain);
            stats["TerrainComposition"] = terrainComposition;

            // Calculate navigability metrics
            var walkableTiles = CalculateWalkableTiles(terrain);
            stats["NavigabilityMetrics"] = new
            {
                WalkableTiles = walkableTiles,
                NavigabilityRatio = (double)walkableTiles / (terrain.Width * terrain.Height),
                EntityDensity = walkableTiles > 0 ? (double)entities.Count / walkableTiles : 0
            };

            // Calculate complexity metrics
            stats["ComplexityMetrics"] = new
            {
                TerrainComplexity = CalculateTerrainComplexity(terrain),
                EntityComplexity = CalculateEntityComplexity(entities),
                OverallComplexity = CalculateOverallComplexity(terrain, entities)
            };
            
            return stats;
        }

        /// <summary>
        /// Calculates terrain composition statistics
        /// </summary>
        private Dictionary<string, object> CalculateTerrainComposition(TileMap terrain)
        {
            var tileCounts = new Dictionary<TileType, int>();
            var totalTiles = terrain.Width * terrain.Height;

            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tileType = terrain.GetTile(x, y);
                    tileCounts[tileType] = tileCounts.GetValueOrDefault(tileType, 0) + 1;
                }
            }

            return tileCounts.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => (object)new { Count = kvp.Value, Percentage = (kvp.Value * 100.0) / totalTiles }
            );
        }

        /// <summary>
        /// Calculates walkable tiles count
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
        /// Calculates terrain complexity based on tile transitions
        /// </summary>
        private double CalculateTerrainComplexity(TileMap terrain)
        {
            int transitions = 0;
            int totalChecks = 0;
            
            for (int x = 0; x < terrain.Width - 1; x++)
            {
                for (int y = 0; y < terrain.Height - 1; y++)
                {
                    var currentTile = terrain.GetTile(x, y);
                    var rightTile = terrain.GetTile(x + 1, y);
                    var downTile = terrain.GetTile(x, y + 1);
                    
                    if (currentTile != rightTile) transitions++;
                    if (currentTile != downTile) transitions++;
                    totalChecks += 2;
                }
            }
            
            return totalChecks > 0 ? (double)transitions / totalChecks : 0.0;
        }

        /// <summary>
        /// Calculates entity complexity based on distribution and types
        /// </summary>
        private double CalculateEntityComplexity(List<Entity> entities)
        {
            if (entities == null || !entities.Any()) return 0.0;
            
            var typeCount = entities.GroupBy(e => e.Type).Count();
            var averagePropertiesPerEntity = entities.Average(e => e.Properties?.Count ?? 0);
            
            return (typeCount * averagePropertiesPerEntity) / 10.0; // Normalized complexity score
        }

        /// <summary>
        /// Calculates overall level complexity
        /// </summary>
        private double CalculateOverallComplexity(TileMap terrain, List<Entity> entities)
        {
            var terrainComplexity = CalculateTerrainComplexity(terrain);
            var entityComplexity = CalculateEntityComplexity(entities);
            var sizeComplexity = Math.Log10(terrain.Width * terrain.Height) / 10.0;
            
            return (terrainComplexity + entityComplexity + sizeComplexity) / 3.0;
        }

        /// <summary>
        /// Estimates memory usage for the complete level
        /// </summary>
        private long EstimateLevelMemoryUsage(TileMap terrain, List<Entity> entities)
        {
            var terrainMemory = terrain.Width * terrain.Height * 8; // 8 bytes per tile
            var entityMemory = entities.Count * 64; // 64 bytes per entity
            var metadataMemory = 1024; // Estimated metadata overhead
            
            return terrainMemory + entityMemory + metadataMemory;
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
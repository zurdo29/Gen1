using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Implementation of level assembler that combines terrain and entities into complete levels
    /// </summary>
    public class LevelAssembler : ILevelAssembler
    {
        private readonly ThemeApplicationService _themeApplicationService;
        private readonly ISimpleLoggerService _logger;
        
        /// <summary>
        /// Initializes a new instance of the LevelAssembler
        /// </summary>
        /// <param name="themeApplicationService">Service for applying themes to levels</param>
        /// <param name="logger">Logger service for performance metrics</param>
        public LevelAssembler(ThemeApplicationService themeApplicationService = null, ISimpleLoggerService logger = null)
        {
            _themeApplicationService = themeApplicationService;
            _logger = logger;
        }
        /// <summary>
        /// Assembles a complete level from terrain and entities
        /// </summary>
        /// <param name="terrain">Generated terrain</param>
        /// <param name="entities">Placed entities</param>
        /// <param name="config">Generation configuration</param>
        /// <returns>Assembled level</returns>
        public Level AssembleLevel(TileMap terrain, List<Entity> entities, GenerationConfig config)
        {
            if (terrain == null)
                throw new ArgumentNullException(nameof(terrain));
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger?.LogInfo("Starting level assembly", new {
                    OperationId = operationId,
                    TerrainSize = $"{terrain.Width}x{terrain.Height}",
                    EntityCount = entities.Count,
                    Algorithm = config.GenerationAlgorithm,
                    Seed = config.Seed
                });
                
                // Log pre-assembly validation
                var validationResults = ValidateAssemblyInputs(terrain, entities, config);
                if (validationResults.Any())
                {
                    _logger?.LogWarning("Level assembly validation warnings", new {
                        OperationId = operationId,
                        ValidationWarnings = validationResults
                    });
                }

                // Log memory estimation
                var estimatedMemory = EstimateLevelMemoryUsage(terrain, entities);
                _logger?.LogInfo("Level assembly memory estimation", new {
                    OperationId = operationId,
                    EstimatedMemoryBytes = estimatedMemory,
                    EstimatedMemoryMB = estimatedMemory / (1024.0 * 1024.0)
                });

                var level = new Level
                {
                    Terrain = terrain,
                    Entities = new List<Entity>(entities),
                    Name = GenerateLevelName(config),
                    Metadata = CreateLevelMetadata(terrain, entities, config)
                };

                stopwatch.Stop();
                
                // Calculate assembly statistics
                var assemblyStats = CalculateAssemblyStatistics(level, terrain, entities, config);
                
                _logger?.LogGeneration($"LevelAssembly (Config: {operationId})", stopwatch.Elapsed, new {
                    LevelName = level.Name,
                    TerrainSize = $"{terrain.Width}x{terrain.Height}",
                    EntityCount = entities.Count,
                    Algorithm = config.GenerationAlgorithm,
                    Seed = config.Seed,
                    AssemblyStatistics = assemblyStats
                });
                
                _logger?.LogPerformance("LevelAssembly", stopwatch.Elapsed, new {
                    TilesProcessed = terrain.Width * terrain.Height,
                    EntitiesProcessed = entities.Count,
                    ProcessingRate = (terrain.Width * terrain.Height + entities.Count) / stopwatch.Elapsed.TotalSeconds,
                    MemoryEstimate = estimatedMemory,
                    MetadataKeys = level.Metadata?.Count ?? 0
                });
                
                _logger?.LogInfo("Level assembly completed successfully", new {
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
                _logger?.LogError("Level assembly failed", ex, new {
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
        /// Applies a visual theme to a level
        /// </summary>
        /// <param name="level">Level to apply theme to</param>
        /// <param name="theme">Visual theme to apply</param>
        public void ApplyVisualTheme(Level level, VisualTheme theme)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));

            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger?.LogInfo("Starting visual theme application", new {
                    OperationId = operationId,
                    LevelName = level.Name,
                    ThemeName = theme.Name,
                    TerrainSize = $"{level.Terrain.Width}x{level.Terrain.Height}",
                    EntityCount = level.Entities.Count
                });

                // Log theme details
                _logger?.LogInfo("Visual theme details", new {
                    OperationId = operationId,
                    ThemeName = theme.Name,
                    ColorCount = theme.Colors?.Count ?? 0,
                    TileSpriteCount = theme.TileSprites?.Count ?? 0,
                    EntitySpriteCount = theme.EntitySprites?.Count ?? 0,
                    EffectCount = theme.Effects?.Count ?? 0
                });

                if (_themeApplicationService != null)
                {
                    // Use the comprehensive theme application service
                    var warnings = _themeApplicationService.ApplyThemeToLevel(level, theme);
                    
                    // Store any warnings in metadata for debugging
                    if (warnings.Any())
                    {
                        level.Metadata["ThemeApplicationWarnings"] = warnings;
                        _logger?.LogWarning("Theme application warnings", new {
                            OperationId = operationId,
                            WarningCount = warnings.Count,
                            Warnings = warnings
                        });
                    }
                    
                    // Create and store application report
                    var report = _themeApplicationService.CreateApplicationReport(level, theme, warnings);
                    level.Metadata["ThemeApplicationReport"] = report;
                }
                else
                {
                    // Fallback to basic theme application for backward compatibility
                    ApplyBasicTheme(level, theme);
                }
                
                stopwatch.Stop();
                
                _logger?.LogPerformance("VisualThemeApplication", stopwatch.Elapsed, new {
                    ThemeName = theme.Name,
                    EntitiesProcessed = level.Entities.Count,
                    TilesProcessed = level.Terrain.Width * level.Terrain.Height,
                    ProcessingRate = (level.Entities.Count + level.Terrain.Width * level.Terrain.Height) / stopwatch.Elapsed.TotalSeconds
                });
                
                _logger?.LogInfo("Visual theme application completed successfully", new {
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
                _logger?.LogError("Visual theme application failed", ex, new {
                    OperationId = operationId,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    LevelName = level.Name,
                    ThemeName = theme.Name
                });
                throw;
            }
        }
        
        /// <summary>
        /// Applies basic theme information (fallback method)
        /// </summary>
        private void ApplyBasicTheme(Level level, VisualTheme theme)
        {
            // Store theme information in level metadata
            level.Metadata["VisualTheme"] = theme.Name;
            level.Metadata["TileSprites"] = theme.TileSprites;
            level.Metadata["EntitySprites"] = theme.EntitySprites;
            level.Metadata["ColorPalette"] = theme.Colors;
            
            // Apply theme-specific properties to entities
            foreach (var entity in level.Entities)
            {
                if (theme.EntitySprites.ContainsKey(entity.Type))
                {
                    entity.Properties["Sprite"] = theme.EntitySprites[entity.Type];
                }
                else
                {
                    // Basic fallback - use a generic sprite path
                    entity.Properties["Sprite"] = $"assets/fallback/entity_{entity.Type}.png";
                }
            }
        }

        /// <summary>
        /// Generates a descriptive name for the level based on configuration
        /// </summary>
        private string GenerateLevelName(GenerationConfig config)
        {
            var algorithmNames = new Dictionary<string, string>
            {
                { "perlin", "Natural" },
                { "cellular", "Cavern" },
                { "maze", "Labyrinth" },
                { "rooms", "Dungeon" }
            };

            var baseName = algorithmNames.ContainsKey(config.GenerationAlgorithm.ToLower()) 
                ? algorithmNames[config.GenerationAlgorithm.ToLower()]
                : "Generated";

            var size = GetSizeCategory(config.Width, config.Height);
            var entityCount = config.Entities?.Sum(e => e.Count) ?? 0;
            
            if (entityCount > 0)
            {
                return $"{size} {baseName} Level ({entityCount} entities)";
            }
            
            return $"{size} {baseName} Level";
        }

        /// <summary>
        /// Determines size category based on dimensions
        /// </summary>
        private string GetSizeCategory(int width, int height)
        {
            var totalTiles = width * height;
            
            if (totalTiles < 1000)
                return "Small";
            else if (totalTiles < 5000)
                return "Medium";
            else
                return "Large";
        }

        /// <summary>
        /// Creates comprehensive metadata for the level
        /// </summary>
        private Dictionary<string, object> CreateLevelMetadata(TileMap terrain, List<Entity> entities, GenerationConfig config)
        {
            var metadata = new Dictionary<string, object>
            {
                ["GeneratedAt"] = DateTime.UtcNow,
                ["GenerationSeed"] = config.Seed,
                ["GenerationAlgorithm"] = config.GenerationAlgorithm,
                ["AlgorithmParameters"] = new Dictionary<string, object>(config.AlgorithmParameters),
                ["Dimensions"] = new { Width = terrain.Width, Height = terrain.Height },
                ["TotalTiles"] = terrain.Width * terrain.Height
            };

            // Add terrain statistics
            var terrainStats = CalculateTerrainStatistics(terrain);
            metadata["TerrainStatistics"] = terrainStats;

            // Add entity statistics
            var entityStats = CalculateEntityStatistics(entities);
            metadata["EntityStatistics"] = entityStats;

            // Add navigability information
            metadata["NavigableArea"] = CalculateNavigableArea(terrain);
            metadata["NavigabilityRatio"] = CalculateNavigabilityRatio(terrain);

            // Add gameplay configuration
            if (config.Gameplay != null)
            {
                metadata["GameplayConfig"] = config.Gameplay;
            }

            return metadata;
        }

        /// <summary>
        /// Calculates statistics about terrain composition
        /// </summary>
        private Dictionary<string, object> CalculateTerrainStatistics(TileMap terrain)
        {
            var tileCounts = new Dictionary<TileType, int>();
            var totalTiles = terrain.Width * terrain.Height;

            // Count each tile type
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tileType = terrain.GetTile(x, y);
                    tileCounts[tileType] = tileCounts.GetValueOrDefault(tileType, 0) + 1;
                }
            }

            // Convert to percentages and create statistics
            var stats = new Dictionary<string, object>
            {
                ["TotalTiles"] = totalTiles,
                ["TileComposition"] = tileCounts.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => new { Count = kvp.Value, Percentage = (kvp.Value * 100.0) / totalTiles }
                )
            };

            return stats;
        }

        /// <summary>
        /// Calculates statistics about entity placement
        /// </summary>
        private Dictionary<string, object> CalculateEntityStatistics(List<Entity> entities)
        {
            var entityCounts = entities.GroupBy(e => e.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            var stats = new Dictionary<string, object>
            {
                ["TotalEntities"] = entities.Count,
                ["EntityComposition"] = entityCounts
            };

            // Calculate entity density if we have position information
            if (entities.Any())
            {
                var positions = entities.Select(e => e.Position).ToList();
                var minX = positions.Min(p => p.X);
                var maxX = positions.Max(p => p.X);
                var minY = positions.Min(p => p.Y);
                var maxY = positions.Max(p => p.Y);
                
                stats["EntityBounds"] = new 
                { 
                    MinX = minX, MaxX = maxX, 
                    MinY = minY, MaxY = maxY,
                    Width = maxX - minX,
                    Height = maxY - minY
                };
            }

            return stats;
        }

        /// <summary>
        /// Calculates the total navigable area in the terrain
        /// </summary>
        private int CalculateNavigableArea(TileMap terrain)
        {
            int navigableCount = 0;
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    if (terrain.IsWalkable(x, y))
                    {
                        navigableCount++;
                    }
                }
            }
            
            return navigableCount;
        }

        /// <summary>
        /// Calculates the ratio of navigable to total tiles
        /// </summary>
        private double CalculateNavigabilityRatio(TileMap terrain)
        {
            var navigableArea = CalculateNavigableArea(terrain);
            var totalArea = terrain.Width * terrain.Height;
            
            return totalArea > 0 ? (double)navigableArea / totalArea : 0.0;
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
            var walkableTiles = CalculateNavigableArea(terrain);
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
            var walkableTiles = CalculateNavigableArea(terrain);
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
                kvp => new { Count = kvp.Value, Percentage = (kvp.Value * 100.0) / totalTiles }
            );
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
            if (!entities.Any()) return 0.0;
            
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
    }
}
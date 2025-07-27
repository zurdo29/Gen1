using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Comprehensive validator for all configuration classes
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// Validates a complete generation configuration and returns detailed results
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result with errors and warnings</returns>
        public static ConfigValidationResult ValidateConfiguration(GenerationConfig config)
        {
            var result = new ConfigValidationResult();
            
            if (config == null)
            {
                result.Errors.Add("Configuration cannot be null");
                return result;
            }

            // Validate the main configuration
            var configErrors = config.Validate();
            result.Errors.AddRange(configErrors);

            // Apply defaults and collect warnings
            var warnings = config.ApplyDefaults();
            result.Warnings.AddRange(warnings);

            // Additional cross-validation checks
            ValidateCrossReferences(config, result);
            ValidateLogicalConsistency(config, result);

            return result;
        }

        /// <summary>
        /// Validates cross-references between different configuration sections
        /// </summary>
        private static void ValidateCrossReferences(GenerationConfig config, ConfigValidationResult result)
        {
            // Check if visual theme references match entity types
            if (config.VisualTheme?.EntitySprites != null && config.Entities != null)
            {
                var entityTypes = config.Entities.Select(e => e.Type.ToString().ToLower()).Distinct();
                var spriteKeys = config.VisualTheme.EntitySprites.Keys.Select(k => k.ToLower());
                
                foreach (var entityType in entityTypes)
                {
                    if (!spriteKeys.Contains(entityType))
                    {
                        result.Warnings.Add($"No sprite defined for entity type '{entityType}' in visual theme");
                    }
                }
            }

            // Check if terrain types in config match visual theme tile sprites
            if (config.TerrainTypes != null && config.VisualTheme?.TileSprites != null)
            {
                var terrainTypes = config.TerrainTypes.Select(t => t.ToLower());
                var tileKeys = config.VisualTheme.TileSprites.Keys.Select(k => k.ToLower());
                
                foreach (var terrainType in terrainTypes)
                {
                    if (!tileKeys.Contains(terrainType))
                    {
                        result.Warnings.Add($"No tile sprite defined for terrain type '{terrainType}' in visual theme");
                    }
                }
            }
        }

        /// <summary>
        /// Validates logical consistency across configuration sections
        /// </summary>
        private static void ValidateLogicalConsistency(GenerationConfig config, ConfigValidationResult result)
        {
            // Check if level size can accommodate all entities
            if (config.Entities != null && config.Entities.Count > 0)
            {
                var totalEntities = config.Entities.Sum(e => e.Count);
                var levelArea = config.Width * config.Height;
                var maxEntitiesForSize = levelArea / 4; // Rough estimate: 1 entity per 4 tiles

                if (totalEntities > maxEntitiesForSize)
                {
                    result.Warnings.Add($"Level size ({config.Width}x{config.Height}) may be too small for {totalEntities} entities. Consider increasing level size or reducing entity count.");
                }
            }

            // Check if time limit is reasonable for level size
            if (config.Gameplay?.TimeLimit > 0)
            {
                var levelDiagonal = Math.Sqrt(config.Width * config.Width + config.Height * config.Height);
                var estimatedTraversalTime = levelDiagonal / (config.Gameplay.PlayerSpeed * 10); // Rough estimate
                
                if (config.Gameplay.TimeLimit < estimatedTraversalTime)
                {
                    result.Warnings.Add($"Time limit ({config.Gameplay.TimeLimit}s) may be too short for level size. Estimated minimum traversal time: {estimatedTraversalTime:F1}s");
                }
            }

            // Check victory conditions consistency
            if (config.Gameplay?.VictoryConditions != null)
            {
                if (config.Gameplay.VictoryConditions.Contains("collect_all_items"))
                {
                    var itemCount = config.Entities?.Where(e => e.Type == EntityType.Item).Sum(e => e.Count) ?? 0;
                    if (itemCount == 0)
                    {
                        result.Errors.Add("Victory condition 'collect_all_items' specified but no items are configured to be placed");
                    }
                }

                if (config.Gameplay.VictoryConditions.Contains("defeat_all_enemies"))
                {
                    var enemyCount = config.Entities?.Where(e => e.Type == EntityType.Enemy).Sum(e => e.Count) ?? 0;
                    if (enemyCount == 0)
                    {
                        result.Errors.Add("Victory condition 'defeat_all_enemies' specified but no enemies are configured to be placed");
                    }
                }

                if (config.Gameplay.VictoryConditions.Contains("reach_exit"))
                {
                    var exitCount = config.Entities?.Where(e => e.Type == EntityType.Exit).Sum(e => e.Count) ?? 0;
                    if (exitCount == 0)
                    {
                        result.Warnings.Add("Victory condition 'reach_exit' specified but no exit is configured to be placed. An exit will be automatically generated.");
                    }
                }
            }
        }

        /// <summary>
        /// Creates a default configuration with all required fields populated
        /// </summary>
        /// <returns>A valid default configuration</returns>
        public static GenerationConfig CreateDefaultConfiguration()
        {
            return new GenerationConfig
            {
                Width = 50,
                Height = 50,
                Seed = new Random().Next(),
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "scale", 0.1 },
                    { "octaves", 4 },
                    { "persistence", 0.5 },
                    { "lacunarity", 2.0 }
                },
                TerrainTypes = new List<string> { "ground", "wall", "water" },
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Player,
                        Count = 1,
                        PlacementStrategy = "center"
                    },
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = 3,
                        MinDistance = 5.0f,
                        PlacementStrategy = "random"
                    },
                    new EntityConfig
                    {
                        Type = EntityType.Item,
                        Count = 5,
                        MinDistance = 3.0f,
                        PlacementStrategy = "spread"
                    },
                    new EntityConfig
                    {
                        Type = EntityType.Exit,
                        Count = 1,
                        PlacementStrategy = "far_from_player"
                    }
                },
                VisualTheme = new VisualThemeConfig
                {
                    ThemeName = "default",
                    ColorPalette = ColorPalette.FromDictionary(new Dictionary<string, string>
                    {
                        { "ground", "#8B4513" },
                        { "wall", "#654321" },
                        { "water", "#4169E1" },
                        { "player", "#FFD700" },
                        { "enemy", "#DC143C" },
                        { "item", "#32CD32" },
                        { "exit", "#FF69B4" }
                    })
                },
                Gameplay = new GameplayConfig
                {
                    PlayerSpeed = 5.0f,
                    PlayerHealth = 100,
                    Difficulty = "normal",
                    TimeLimit = (int)0.0f,
                    VictoryConditions = new List<string> { "reach_exit" }
                }
            };
        }
    }

    /// <summary>
    /// Result of configuration validation
    /// </summary>
    public class ConfigValidationResult
    {
        /// <summary>
        /// Critical errors that prevent configuration from being used
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Warnings about potential issues or applied defaults
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// True if configuration is valid (no errors)
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// True if there are any warnings
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Gets a formatted summary of all issues
        /// </summary>
        public string GetSummary()
        {
            var summary = new List<string>();
            
            if (Errors.Count > 0)
            {
                summary.Add($"Errors ({Errors.Count}):");
                summary.AddRange(Errors.Select(e => $"  - {e}"));
            }
            
            if (Warnings.Count > 0)
            {
                summary.Add($"Warnings ({Warnings.Count}):");
                summary.AddRange(Warnings.Select(w => $"  - {w}"));
            }
            
            if (IsValid && !HasWarnings)
            {
                summary.Add("Configuration is valid with no issues.");
            }
            
            return string.Join(Environment.NewLine, summary);
        }

        /// <summary>
        /// Converts this ConfigValidationResult to a generic ValidationResult
        /// </summary>
        public ValidationResult ToValidationResult()
        {
            var result = new ValidationResult();
            result.Errors.AddRange(this.Errors);
            result.Warnings.AddRange(this.Warnings);
            return result;
        }
    }
}
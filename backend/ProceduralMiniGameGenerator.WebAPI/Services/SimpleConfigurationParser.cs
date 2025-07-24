using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Configuration;
using System.Text.Json;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Simple implementation of IConfigurationParser for web API
    /// </summary>
    public class SimpleConfigurationParser : IConfigurationParser
    {
        /// <summary>
        /// Parses a JSON configuration file
        /// </summary>
        public GenerationConfig ParseConfig(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {jsonPath}");
            }
            
            var jsonContent = File.ReadAllText(jsonPath);
            return ParseConfigFromString(jsonContent);
        }

        /// <summary>
        /// Parses configuration from JSON string
        /// </summary>
        public GenerationConfig ParseConfigFromString(string jsonContent)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                
                var config = JsonSerializer.Deserialize<GenerationConfig>(jsonContent, options);
                
                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration");
                }
                
                // Apply defaults for any missing values
                config.ApplyDefaults();
                
                return config;
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates a configuration object
        /// </summary>
        public bool ValidateConfig(GenerationConfig config, out List<string> errors)
        {
            errors = new List<string>();
            
            if (config == null)
            {
                errors.Add("Configuration cannot be null");
                return false;
            }
            
            try
            {
                // Use the built-in validation from GenerationConfig
                var configErrors = config.Validate();
                errors.AddRange(configErrors);
                
                // Additional web-specific validation
                if (config.Width * config.Height > 1000000) // 1M tiles max
                {
                    errors.Add("Level size too large (maximum 1,000,000 tiles)");
                }
                
                if (config.Entities != null && config.Entities.Sum(e => e.Count) > 10000)
                {
                    errors.Add("Too many entities requested (maximum 10,000 entities)");
                }
                
                return errors.Count == 0;
            }
            catch (Exception ex)
            {
                errors.Add($"Validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a default configuration
        /// </summary>
        public GenerationConfig GetDefaultConfig()
        {
            return new GenerationConfig
            {
                Width = 50,
                Height = 50,
                Seed = Environment.TickCount,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    ["scale"] = 0.1,
                    ["octaves"] = 4,
                    ["persistence"] = 0.5,
                    ["lacunarity"] = 2.0
                },
                TerrainTypes = new List<string> { "ground", "wall", "water" },
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Player,
                        Count = 1,
                        Properties = new Dictionary<string, object>
                        {
                            ["isSpawnPoint"] = true
                        }
                    },
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = 5,
                        Properties = new Dictionary<string, object>
                        {
                            ["difficulty"] = 1
                        }
                    },
                    new EntityConfig
                    {
                        Type = EntityType.Collectible,
                        Count = 10,
                        Properties = new Dictionary<string, object>
                        {
                            ["value"] = 100
                        }
                    }
                },
                VisualTheme = new VisualThemeConfig
                {
                    Name = "Default",
                    ColorPalette = new ColorPalette
                    {
                        Primary = "#4CAF50",
                        Secondary = "#2196F3",
                        Accent = "#FF9800",
                        Background = "#FFFFFF",
                        Text = "#000000"
                    }
                },
                Gameplay = new GameplayConfig
                {
                    Difficulty = 1,
                    ObjectiveType = "collect_all",
                    TimeLimit = 300,
                    PlayerLives = 3
                }
            };
        }
    }
}
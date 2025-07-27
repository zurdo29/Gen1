using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Configuration;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Simple implementation of IConfigurationParser for web API
    /// </summary>
    public class SimpleConfigurationParser : IConfigurationParser
    {
        private readonly ILogger<SimpleConfigurationParser> _logger;

        public SimpleConfigurationParser(ILogger<SimpleConfigurationParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        /// <summary>
        /// Default JSON serialization options
        /// </summary>
        private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        // Configuration limits
        private const int MaxLevelTiles = 1_000_000;
        private const int MaxEntitiesPerLevel = 10_000;
        private const int DefaultLevelSize = 50;
        private const int DefaultTimeLimit = 300;
        private const int DefaultPlayerHealth = 3;
        /// <summary>
        /// Parses configuration from a dictionary
        /// </summary>
        public T ParseConfiguration<T>(Dictionary<string, object> configData) where T : class, new()
        {
            if (configData == null)
                throw new ArgumentNullException(nameof(configData));

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(configData);
                var result = JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);
                return result ?? new T();
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Failed to parse configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates configuration data
        /// </summary>
        public bool ValidateConfiguration(Dictionary<string, object> configData)
        {
            if (configData == null)
                return false;

            try
            {
                // Try to parse as GenerationConfig to validate structure
                var config = ParseConfiguration<GenerationConfig>(configData);
                return ValidateConfig(config, out _);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a JSON configuration file
        /// </summary>
        public GenerationConfig ParseConfig(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                _logger.LogError("Configuration file path is null or empty");
                throw new ArgumentException("Configuration file path cannot be null or empty", nameof(jsonPath));
            }

            if (!File.Exists(jsonPath))
            {
                _logger.LogError("Configuration file not found: {JsonPath}", jsonPath);
                throw new FileNotFoundException($"Configuration file not found: {jsonPath}");
            }
            
            _logger.LogDebug("Reading configuration from file: {JsonPath}", jsonPath);
            var jsonContent = File.ReadAllText(jsonPath);
            return ParseConfigFromString(jsonContent);
        }

        /// <summary>
        /// Parses configuration from JSON string
        /// </summary>
        public GenerationConfig ParseConfigFromString(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogError("JSON content is null or empty");
                throw new ArgumentException("JSON content cannot be null or empty", nameof(jsonContent));
            }

            try
            {
                _logger.LogDebug("Parsing configuration from JSON string");
                var config = JsonSerializer.Deserialize<GenerationConfig>(jsonContent, DefaultJsonOptions);
                
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
                if (config.Width * config.Height > MaxLevelTiles)
                {
                    errors.Add($"Level size too large (maximum {MaxLevelTiles:N0} tiles)");
                }
                
                if (config.Entities != null && config.Entities.Sum(e => e.Count) > MaxEntitiesPerLevel)
                {
                    errors.Add($"Too many entities requested (maximum {MaxEntitiesPerLevel:N0} entities)");
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
                Width = DefaultLevelSize,
                Height = DefaultLevelSize,
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
                    }.ToDictionary()
                },
                Gameplay = new GameplayConfig
                {
                    Difficulty = "normal",
                    Objectives = new List<string> { "collect_all" },
                    TimeLimit = DefaultTimeLimit,
                    PlayerHealth = DefaultPlayerHealth
                }
            };
        }
    }
}
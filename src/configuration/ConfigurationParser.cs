using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Implementation of configuration parser for JSON files
    /// </summary>
    public class ConfigurationParser : IConfigurationParser
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ISimpleLoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the ConfigurationParser
        /// </summary>
        public ConfigurationParser(ISimpleLoggerService logger = null)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        /// <summary>
        /// Parses a JSON configuration file
        /// </summary>
        /// <param name="jsonPath">Path to the JSON file</param>
        /// <returns>Parsed configuration</returns>
        /// <exception cref="ArgumentException">Thrown when jsonPath is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when JSON parsing fails</exception>
        public GenerationConfig ParseConfig(string jsonPath)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger?.LogInfo("Starting configuration file parsing", new {
                    OperationId = operationId,
                    FilePath = jsonPath,
                    Operation = "ConfigurationFileParsing"
                });

                if (string.IsNullOrWhiteSpace(jsonPath))
                {
                    _logger?.LogError("Configuration file path validation failed", null, new {
                        OperationId = operationId,
                        FilePath = jsonPath,
                        ValidationError = "Path is null or empty"
                    });
                    throw new ArgumentException("JSON file path cannot be null or empty", nameof(jsonPath));
                }

                if (!File.Exists(jsonPath))
                {
                    _logger?.LogError("Configuration file not found", null, new {
                        OperationId = operationId,
                        FilePath = jsonPath,
                        FileExists = false
                    });
                    throw new FileNotFoundException($"Configuration file not found: {jsonPath}");
                }

                // Log file information and validation
                var fileInfo = new FileInfo(jsonPath);
                _logger?.LogInfo("Configuration file validation passed", new { 
                    OperationId = operationId,
                    FilePath = jsonPath,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    IsReadOnly = fileInfo.IsReadOnly,
                    Extension = fileInfo.Extension
                });

                // Log file read operation
                var readStopwatch = Stopwatch.StartNew();
                string jsonContent = File.ReadAllText(jsonPath);
                readStopwatch.Stop();
                
                _logger?.LogPerformance("ConfigurationFileRead", readStopwatch.Elapsed, new {
                    OperationId = operationId,
                    FilePath = jsonPath,
                    FileSize = fileInfo.Length,
                    ReadSpeed = fileInfo.Length / readStopwatch.Elapsed.TotalSeconds
                });

                var config = ParseConfigFromString(jsonContent);
                
                stopwatch.Stop();
                _logger?.LogGeneration(operationId, "ConfigurationFileParsing", stopwatch.Elapsed, new { 
                    FilePath = jsonPath,
                    FileSize = fileInfo.Length,
                    ConfigSize = $"{config.Width}x{config.Height}",
                    EntityCount = config.Entities?.Count ?? 0,
                    Algorithm = config.GenerationAlgorithm,
                    Seed = config.Seed,
                    ParameterCount = config.AlgorithmParameters?.Count ?? 0
                });

                _logger?.LogInfo("Configuration file parsing completed successfully", new {
                    OperationId = operationId,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    FilePath = jsonPath,
                    ConfigSize = $"{config.Width}x{config.Height}",
                    Algorithm = config.GenerationAlgorithm,
                    EntityCount = config.Entities?.Count ?? 0
                });

                return config;
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException))
            {
                stopwatch.Stop();
                _logger?.LogError($"Failed to read configuration file '{jsonPath}'", ex, new { 
                    OperationId = operationId,
                    FilePath = jsonPath,
                    Duration = stopwatch.Elapsed,
                    FileExists = File.Exists(jsonPath)
                });
                throw new InvalidOperationException($"Failed to read configuration file '{jsonPath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses configuration from JSON string
        /// </summary>
        /// <param name="jsonContent">JSON content as string</param>
        /// <returns>Parsed configuration</returns>
        /// <exception cref="ArgumentException">Thrown when jsonContent is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when JSON parsing fails</exception>
        public GenerationConfig ParseConfigFromString(string jsonContent)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger?.LogInfo("Starting configuration string parsing", new { 
                    OperationId = operationId,
                    ContentLength = jsonContent?.Length ?? 0,
                    Operation = "ConfigurationStringParsing"
                });

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger?.LogError("Configuration content validation failed", null, new {
                        OperationId = operationId,
                        ContentLength = jsonContent?.Length ?? 0,
                        ValidationError = "Content is null or empty"
                    });
                    throw new ArgumentException("JSON content cannot be null or empty", nameof(jsonContent));
                }

                // Log JSON parsing attempt
                var parseStopwatch = Stopwatch.StartNew();
                var config = JsonSerializer.Deserialize<GenerationConfig>(jsonContent, _jsonOptions);
                parseStopwatch.Stop();
                
                _logger?.LogPerformance("JSONDeserialization", parseStopwatch.Elapsed, new {
                    OperationId = operationId,
                    ContentLength = jsonContent.Length,
                    DeserializationSpeed = jsonContent.Length / parseStopwatch.Elapsed.TotalSeconds
                });
                
                if (config == null)
                {
                    _logger?.LogError("JSON deserialization returned null", null, new {
                        OperationId = operationId,
                        ContentLength = jsonContent.Length
                    });
                    throw new InvalidOperationException("Failed to deserialize JSON content - result was null");
                }

                // Apply default values for missing properties
                var defaultsStopwatch = Stopwatch.StartNew();
                var warnings = config.ApplyDefaults();
                defaultsStopwatch.Stop();
                
                _logger?.LogPerformance("ConfigurationDefaultsApplication", defaultsStopwatch.Elapsed, new {
                    OperationId = operationId,
                    DefaultsApplied = warnings.Count
                });
                
                // Log warnings if any defaults were applied
                if (warnings.Count > 0)
                {
                    _logger?.LogWarning($"Applied {warnings.Count} default values during configuration parsing", new { 
                        OperationId = operationId,
                        Warnings = warnings,
                        WarningCount = warnings.Count
                    });
                    
                    foreach (var warning in warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }

                stopwatch.Stop();
                _logger?.LogGeneration(operationId, "ConfigurationStringParsing", stopwatch.Elapsed, new { 
                    ContentLength = jsonContent.Length,
                    ConfigSize = $"{config.Width}x{config.Height}",
                    EntityCount = config.Entities?.Count ?? 0,
                    WarningCount = warnings.Count,
                    Algorithm = config.GenerationAlgorithm,
                    Seed = config.Seed,
                    ParameterCount = config.AlgorithmParameters?.Count ?? 0
                });

                _logger?.LogInfo("Configuration string parsing completed successfully", new {
                    OperationId = operationId,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    ContentLength = jsonContent.Length,
                    ConfigSize = $"{config.Width}x{config.Height}",
                    Algorithm = config.GenerationAlgorithm,
                    EntityCount = config.Entities?.Count ?? 0,
                    WarningsCount = warnings.Count
                });

                return config;
            }
            catch (JsonException ex)
            {
                stopwatch.Stop();
                _logger?.LogError("Invalid JSON format during configuration parsing", ex, new { 
                    OperationId = operationId,
                    ContentLength = jsonContent?.Length ?? 0,
                    Duration = stopwatch.Elapsed,
                    JsonError = ex.Message,
                    LineNumber = ex.LineNumber,
                    BytePositionInLine = ex.BytePositionInLine
                });
                throw new InvalidOperationException($"Invalid JSON format: {ex.Message}", ex);
            }
            catch (Exception ex) when (!(ex is ArgumentException))
            {
                stopwatch.Stop();
                _logger?.LogError("Failed to parse configuration", ex, new { 
                    OperationId = operationId,
                    ContentLength = jsonContent?.Length ?? 0,
                    Duration = stopwatch.Elapsed 
                });
                throw new InvalidOperationException($"Failed to parse configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates a configuration object
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <param name="errors">List of validation errors</param>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateConfig(GenerationConfig config, out List<string> errors)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            errors = new List<string>();

            try
            {
                _logger?.LogInfo("Starting configuration validation", new { 
                    OperationId = operationId,
                    ConfigSize = config != null ? $"{config.Width}x{config.Height}" : "null",
                    Algorithm = config?.GenerationAlgorithm,
                    Operation = "ConfigurationValidation"
                });

                if (config == null)
                {
                    errors.Add("Configuration cannot be null");
                    _logger?.LogWarning("Configuration validation failed - null configuration", new {
                        OperationId = operationId,
                        ValidationResult = "Failed",
                        Reason = "Null configuration"
                    });
                    return false;
                }

                // Log configuration details before validation
                _logger?.LogInfo("Configuration validation details", new {
                    OperationId = operationId,
                    Width = config.Width,
                    Height = config.Height,
                    Algorithm = config.GenerationAlgorithm,
                    Seed = config.Seed,
                    EntityCount = config.Entities?.Count ?? 0,
                    ParameterCount = config.AlgorithmParameters?.Count ?? 0,
                    TerrainTypeCount = config.TerrainTypes?.Count ?? 0
                });

                var validationStopwatch = Stopwatch.StartNew();
                var validationErrors = config.Validate();
                validationStopwatch.Stop();
                
                _logger?.LogPerformance("ConfigurationValidationRules", validationStopwatch.Elapsed, new {
                    OperationId = operationId,
                    RulesExecuted = true,
                    ErrorsFound = validationErrors.Count
                });
                
                errors.AddRange(validationErrors);
                
                stopwatch.Stop();
                var isValid = errors.Count == 0;
                
                _logger?.LogGeneration(operationId, "ConfigurationValidation", stopwatch.Elapsed, new { 
                    IsValid = isValid,
                    ErrorCount = errors.Count,
                    ConfigComplexity = CalculateConfigComplexity(config),
                    ConfigSize = $"{config.Width}x{config.Height}",
                    Algorithm = config.GenerationAlgorithm
                });

                if (isValid)
                {
                    _logger?.LogInfo("Configuration validation completed successfully", new {
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        ValidationResult = "Passed",
                        ConfigComplexity = CalculateConfigComplexity(config)
                    });
                }
                else
                {
                    _logger?.LogWarning($"Configuration validation failed with {errors.Count} errors", new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        ValidationResult = "Failed",
                        Errors = errors,
                        ErrorCount = errors.Count,
                        ConfigSize = $"{config.Width}x{config.Height}"
                    });
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                errors.Add($"Validation failed with exception: {ex.Message}");
                _logger?.LogError("Configuration validation failed with exception", ex, new { 
                    OperationId = operationId,
                    Duration = stopwatch.Elapsed,
                    ConfigSize = config != null ? $"{config.Width}x{config.Height}" : "null"
                });
                return false;
            }
        }

        /// <summary>
        /// Calculates configuration complexity for performance metrics
        /// </summary>
        private int CalculateConfigComplexity(GenerationConfig config)
        {
            if (config == null) return 0;
            
            int complexity = 0;
            complexity += config.Width * config.Height / 1000; // Size factor
            complexity += config.Entities?.Count ?? 0; // Entity count
            complexity += config.AlgorithmParameters?.Count ?? 0; // Parameter count
            complexity += config.TerrainTypes?.Count ?? 0; // Terrain type count
            
            return complexity;
        }

        /// <summary>
        /// Gets a default configuration
        /// </summary>
        /// <returns>Default configuration object</returns>
        public GenerationConfig GetDefaultConfig()
        {
            return new GenerationConfig
            {
                Width = 50,
                Height = 50,
                Seed = 0,
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
                        Type = EntityType.Enemy,
                        Count = 3,
                        MinDistance = 2.0f,
                        MaxDistanceFromPlayer = 50.0f,
                        PlacementStrategy = "random"
                    },
                    new EntityConfig
                    {
                        Type = EntityType.Item,
                        Count = 5,
                        MinDistance = 1.0f,
                        MaxDistanceFromPlayer = 100.0f,
                        PlacementStrategy = "spread"
                    },
                    new EntityConfig
                    {
                        Type = EntityType.Exit,
                        Count = 1,
                        MinDistance = 5.0f,
                        MaxDistanceFromPlayer = float.MaxValue,
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
                        { "item", "#32CD32" }
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

        /// <summary>
        /// Parses configuration from a dictionary (IConfigurationParser interface implementation)
        /// </summary>
        /// <param name="configData">Configuration data</param>
        /// <returns>Parsed configuration object</returns>
        public T ParseConfiguration<T>(Dictionary<string, object> configData) where T : class, new()
        {
            if (configData == null)
                throw new ArgumentNullException(nameof(configData));

            var json = JsonSerializer.Serialize(configData, _jsonOptions);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
        }

        /// <summary>
        /// Validates configuration data (IConfigurationParser interface implementation)
        /// </summary>
        /// <param name="configData">Configuration data to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateConfiguration(Dictionary<string, object> configData)
        {
            if (configData == null)
                return false;

            try
            {
                var config = ParseConfiguration<GenerationConfig>(configData);
                var errors = new List<string>();
                return ValidateConfig(config, out errors);
            }
            catch
            {
                return false;
            }
        }
    }
}
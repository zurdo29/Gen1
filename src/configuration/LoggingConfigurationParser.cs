using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Configuration parser with comprehensive logging integration
    /// </summary>
    public class LoggingConfigurationParser : IConfigurationParser
    {
        private readonly IConfigurationParser _baseParser;
        private readonly ILoggerService _loggerService;

        public LoggingConfigurationParser(IConfigurationParser baseParser, ILoggerService loggerService)
        {
            _baseParser = baseParser ?? throw new ArgumentNullException(nameof(baseParser));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Parses a JSON configuration file with logging
        /// </summary>
        public GenerationConfig ParseConfig(string jsonPath)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting configuration file parsing", 
                    new { 
                        OperationId = operationId,
                        FilePath = jsonPath,
                        Operation = "ConfigurationFileParsing"
                    });

                if (string.IsNullOrWhiteSpace(jsonPath))
                {
                    throw new ArgumentException("JSON file path cannot be null or empty", nameof(jsonPath));
                }

                if (!File.Exists(jsonPath))
                {
                    LogSafely(LogLevel.Error, 
                        "Configuration file not found", 
                        new { 
                            OperationId = operationId,
                            FilePath = jsonPath,
                            FileExists = false
                        });
                    throw new FileNotFoundException($"Configuration file not found: {jsonPath}");
                }

                // Log file information
                var fileInfo = new FileInfo(jsonPath);
                LogSafely(LogLevel.Debug, 
                    "Configuration file details", 
                    new { 
                        OperationId = operationId,
                        FilePath = jsonPath,
                        FileSize = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    });

                var config = _baseParser.ParseConfig(jsonPath);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "ConfigurationFileParsing",
                    stopwatch.Elapsed,
                    new {
                        FileSizeBytes = fileInfo.Length,
                        ParsedSuccessfully = true,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        EntityCount = config.Entities?.Count ?? 0
                    });

                LogSafely(LogLevel.Information, 
                    "Configuration file parsing completed successfully", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        FilePath = jsonPath,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm,
                        Seed = config.Seed,
                        EntityCount = config.Entities?.Count ?? 0
                    });

                return config;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Configuration file parsing failed", 
                    new { 
                        OperationId = operationId,
                        FilePath = jsonPath,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                throw;
            }
        }

        /// <summary>
        /// Parses configuration from JSON string with logging
        /// </summary>
        public GenerationConfig ParseConfigFromString(string jsonContent)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting configuration string parsing", 
                    new { 
                        OperationId = operationId,
                        ContentLength = jsonContent?.Length ?? 0,
                        Operation = "ConfigurationStringParsing"
                    });

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    LogSafely(LogLevel.Error, 
                        "Configuration content is null or empty", 
                        new { 
                            OperationId = operationId,
                            ContentLength = jsonContent?.Length ?? 0
                        });
                    throw new ArgumentException("JSON content cannot be null or empty", nameof(jsonContent));
                }

                var config = _baseParser.ParseConfigFromString(jsonContent);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "ConfigurationStringParsing",
                    stopwatch.Elapsed,
                    new {
                        ContentLength = jsonContent.Length,
                        ParsedSuccessfully = true,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        EntityCount = config.Entities?.Count ?? 0,
                        CharactersPerSecond = jsonContent.Length / stopwatch.Elapsed.TotalSeconds
                    });

                LogSafely(LogLevel.Information, 
                    "Configuration string parsing completed successfully", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        ContentLength = jsonContent.Length,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm,
                        Seed = config.Seed,
                        EntityCount = config.Entities?.Count ?? 0
                    });

                return config;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Configuration string parsing failed", 
                    new { 
                        OperationId = operationId,
                        ContentLength = jsonContent?.Length ?? 0,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                throw;
            }
        }

        /// <summary>
        /// Validates a configuration object with logging
        /// </summary>
        public bool ValidateConfig(GenerationConfig config, out List<string> errors)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting configuration validation", 
                    new { 
                        OperationId = operationId,
                        ConfigSize = config != null ? $"{config.Width}x{config.Height}" : "null",
                        Algorithm = config?.GenerationAlgorithm,
                        Operation = "ConfigurationValidation"
                    });

                var isValid = _baseParser.ValidateConfig(config, out errors);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "ConfigurationValidation",
                    stopwatch.Elapsed,
                    new {
                        IsValid = isValid,
                        ErrorCount = errors.Count,
                        ConfigComplexity = CalculateConfigComplexity(config)
                    });

                var logLevel = isValid ? LogLevel.Information : LogLevel.Warning;
                LogSafely(logLevel, 
                    $"Configuration validation completed - {(isValid ? "Valid" : "Invalid")}", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        IsValid = isValid,
                        ErrorCount = errors.Count,
                        Errors = errors
                    });

                return isValid;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                errors = new List<string> { $"Validation failed with exception: {ex.Message}" };
                
                LogErrorSafely(ex, 
                    "Configuration validation failed with exception", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                
                return false;
            }
        }

        /// <summary>
        /// Gets a default configuration with logging
        /// </summary>
        public GenerationConfig GetDefaultConfig()
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Creating default configuration", 
                    new { 
                        OperationId = operationId,
                        Operation = "DefaultConfigurationCreation"
                    });

                var config = _baseParser.GetDefaultConfig();
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "DefaultConfigurationCreation",
                    stopwatch.Elapsed,
                    new {
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm,
                        EntityCount = config.Entities?.Count ?? 0
                    });

                LogSafely(LogLevel.Information, 
                    "Default configuration created successfully", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm,
                        EntityCount = config.Entities?.Count ?? 0
                    });

                return config;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Default configuration creation failed", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                throw;
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

        /// <summary>
        /// Parses configuration from a dictionary (IConfigurationParser interface implementation)
        /// </summary>
        /// <param name="configData">Configuration data</param>
        /// <returns>Parsed configuration object</returns>
        public T ParseConfiguration<T>(Dictionary<string, object> configData) where T : class, new()
        {
            return _baseParser.ParseConfiguration<T>(configData);
        }

        /// <summary>
        /// Validates configuration data (IConfigurationParser interface implementation)
        /// </summary>
        /// <param name="configData">Configuration data to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateConfiguration(Dictionary<string, object> configData)
        {
            return _baseParser.ValidateConfiguration(configData);
        }
    }
}
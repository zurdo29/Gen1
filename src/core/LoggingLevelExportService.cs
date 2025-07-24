using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Level export service with comprehensive logging integration
    /// </summary>
    public class LoggingLevelExportService : ILevelExportService
    {
        private readonly ILevelExportService _baseService;
        private readonly ILoggerService _loggerService;

        public LoggingLevelExportService(ILevelExportService baseService, ILoggerService loggerService)
        {
            _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Exports a level to JSON format with comprehensive logging
        /// </summary>
        public ExportResult ExportLevel(Level level, GenerationConfig generationConfig, string outputPath)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting level export", 
                    new { 
                        OperationId = operationId,
                        OutputPath = outputPath,
                        LevelName = level?.Name,
                        TerrainSize = level?.Terrain != null ? $"{level.Terrain.Width}x{level.Terrain.Height}" : "null",
                        EntityCount = level?.Entities?.Count ?? 0,
                        Algorithm = generationConfig?.GenerationAlgorithm,
                        Operation = "LevelExport"
                    });

                // Log export configuration details
                if (generationConfig != null)
                {
                    LogSafely(LogLevel.Debug, 
                        "Export configuration details", 
                        new { 
                            OperationId = operationId,
                            ConfigSize = $"{generationConfig.Width}x{generationConfig.Height}",
                            Seed = generationConfig.Seed,
                            Algorithm = generationConfig.GenerationAlgorithm,
                            ParameterCount = generationConfig.AlgorithmParameters?.Count ?? 0,
                            EntityConfigCount = generationConfig.Entities?.Count ?? 0
                        });
                }

                var result = _baseService.ExportLevel(level, generationConfig, outputPath);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "LevelExport",
                    stopwatch.Elapsed,
                    new {
                        Success = result.Success,
                        FileSizeBytes = result.FileSize,
                        FileSizeMB = result.FileSize / (1024.0 * 1024.0),
                        ExportRate = result.FileSize / stopwatch.Elapsed.TotalSeconds, // bytes per second
                        TerrainTiles = level?.Terrain != null ? level.Terrain.Width * level.Terrain.Height : 0,
                        EntityCount = level?.Entities?.Count ?? 0
                    });

                var logLevel = result.Success ? LogLevel.Information : LogLevel.Error;
                LogSafely(logLevel, 
                    $"Level export {(result.Success ? "completed successfully" : "failed")}", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Success = result.Success,
                        OutputPath = outputPath,
                        FileSizeBytes = result.FileSize,
                        ErrorCount = result.Errors.Count,
                        WarningCount = result.Warnings.Count,
                        Errors = result.Errors,
                        Warnings = result.Warnings
                    });
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Level export failed with exception", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        OutputPath = outputPath,
                        LevelName = level?.Name
                    });
                throw;
            }
        }

        /// <summary>
        /// Exports a level to JSON string with logging
        /// </summary>
        public string ExportLevelToJson(Level level, GenerationConfig generationConfig)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting level JSON serialization", 
                    new { 
                        OperationId = operationId,
                        LevelName = level?.Name,
                        TerrainSize = level?.Terrain != null ? $"{level.Terrain.Width}x{level.Terrain.Height}" : "null",
                        EntityCount = level?.Entities?.Count ?? 0,
                        Operation = "LevelJsonSerialization"
                    });

                var json = _baseService.ExportLevelToJson(level, generationConfig);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "LevelJsonSerialization",
                    stopwatch.Elapsed,
                    new {
                        JsonLength = json.Length,
                        JsonSizeMB = json.Length / (1024.0 * 1024.0),
                        SerializationRate = json.Length / stopwatch.Elapsed.TotalSeconds, // characters per second
                        TerrainTiles = level?.Terrain != null ? level.Terrain.Width * level.Terrain.Height : 0,
                        EntityCount = level?.Entities?.Count ?? 0
                    });

                LogSafely(LogLevel.Information, 
                    "Level JSON serialization completed successfully", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        JsonLength = json.Length,
                        JsonSizeKB = json.Length / 1024.0
                    });
                
                return json;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Level JSON serialization failed", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level?.Name
                    });
                throw;
            }
        }

        /// <summary>
        /// Imports a level from JSON file with logging
        /// </summary>
        public ImportResult ImportLevel(string jsonPath)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting level import from file", 
                    new { 
                        OperationId = operationId,
                        JsonPath = jsonPath,
                        Operation = "LevelImport"
                    });

                // Log file information if file exists
                if (System.IO.File.Exists(jsonPath))
                {
                    var fileInfo = new System.IO.FileInfo(jsonPath);
                    LogSafely(LogLevel.Debug, 
                        "Import file details", 
                        new { 
                            OperationId = operationId,
                            FilePath = jsonPath,
                            FileSize = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime
                        });
                }

                var result = _baseService.ImportLevel(jsonPath);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "LevelImport",
                    stopwatch.Elapsed,
                    new {
                        Success = result.Success,
                        ImportedLevel = result.Level != null,
                        TerrainSize = result.Level?.Terrain != null ? $"{result.Level.Terrain.Width}x{result.Level.Terrain.Height}" : "null",
                        EntityCount = result.Level?.Entities?.Count ?? 0,
                        ErrorCount = result.Errors.Count,
                        WarningCount = result.Warnings.Count
                    });

                var logLevel = result.Success ? LogLevel.Information : LogLevel.Error;
                LogSafely(logLevel, 
                    $"Level import {(result.Success ? "completed successfully" : "failed")}", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Success = result.Success,
                        JsonPath = jsonPath,
                        LevelName = result.Level?.Name,
                        ErrorCount = result.Errors.Count,
                        WarningCount = result.Warnings.Count,
                        Errors = result.Errors,
                        Warnings = result.Warnings
                    });
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Level import failed with exception", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        JsonPath = jsonPath
                    });
                throw;
            }
        }

        /// <summary>
        /// Imports a level from JSON string with logging
        /// </summary>
        public ImportResult ImportLevelFromJson(string json)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting level import from JSON string", 
                    new { 
                        OperationId = operationId,
                        JsonLength = json?.Length ?? 0,
                        Operation = "LevelJsonImport"
                    });

                var result = _baseService.ImportLevelFromJson(json);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "LevelJsonImport",
                    stopwatch.Elapsed,
                    new {
                        Success = result.Success,
                        JsonLength = json?.Length ?? 0,
                        DeserializationRate = (json?.Length ?? 0) / stopwatch.Elapsed.TotalSeconds, // characters per second
                        ImportedLevel = result.Level != null,
                        TerrainSize = result.Level?.Terrain != null ? $"{result.Level.Terrain.Width}x{result.Level.Terrain.Height}" : "null",
                        EntityCount = result.Level?.Entities?.Count ?? 0,
                        ErrorCount = result.Errors.Count,
                        WarningCount = result.Warnings.Count
                    });

                var logLevel = result.Success ? LogLevel.Information : LogLevel.Error;
                LogSafely(logLevel, 
                    $"Level JSON import {(result.Success ? "completed successfully" : "failed")}", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Success = result.Success,
                        JsonLength = json?.Length ?? 0,
                        LevelName = result.Level?.Name,
                        ErrorCount = result.Errors.Count,
                        WarningCount = result.Warnings.Count,
                        Errors = result.Errors,
                        Warnings = result.Warnings
                    });
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Level JSON import failed with exception", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        JsonLength = json?.Length ?? 0
                    });
                throw;
            }
        }

        /// <summary>
        /// Validates exported level with logging
        /// </summary>
        public ValidationResult ValidateExportedLevel(string jsonPath)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting exported level validation", 
                    new { 
                        OperationId = operationId,
                        JsonPath = jsonPath,
                        Operation = "ExportedLevelValidation"
                    });

                var result = _baseService.ValidateExportedLevel(jsonPath);
                
                stopwatch.Stop();
                
                LogPerformanceSafely(
                    "ExportedLevelValidation",
                    stopwatch.Elapsed,
                    new {
                        IsValid = result.Errors.Count == 0,
                        ErrorCount = result.Errors.Count,
                        WarningCount = result.Warnings.Count,
                        ValidationComplexity = result.Errors.Count + result.Warnings.Count
                    });

                var logLevel = result.Errors.Count == 0 ? LogLevel.Information : LogLevel.Warning;
                LogSafely(logLevel, 
                    $"Exported level validation completed - {(result.Errors.Count == 0 ? "Valid" : "Invalid")}", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        JsonPath = jsonPath,
                        IsValid = result.Errors.Count == 0,
                        ErrorCount = result.Errors.Count,
                        WarningCount = result.Warnings.Count,
                        Errors = result.Errors,
                        Warnings = result.Warnings
                    });
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Exported level validation failed with exception", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        JsonPath = jsonPath
                    });
                
                var result = new ValidationResult();
                result.Errors.Add($"Validation failed with exception: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Gets supported export formats with logging
        /// </summary>
        public List<string> GetSupportedFormats()
        {
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Debug, 
                    "Retrieving supported export formats", 
                    new { 
                        OperationId = operationId,
                        Operation = "GetSupportedFormats"
                    });

                var formats = _baseService.GetSupportedFormats();
                
                LogSafely(LogLevel.Debug, 
                    "Supported export formats retrieved", 
                    new { 
                        OperationId = operationId,
                        FormatCount = formats.Count,
                        Formats = formats
                    });
                
                return formats;
            }
            catch (Exception ex)
            {
                LogErrorSafely(ex, 
                    "Failed to retrieve supported export formats", 
                    new { 
                        OperationId = operationId
                    });
                throw;
            }
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
    }
}
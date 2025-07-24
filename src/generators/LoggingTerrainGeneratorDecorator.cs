using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Decorator that adds comprehensive logging to terrain generators
    /// </summary>
    public class LoggingTerrainGeneratorDecorator : ITerrainGenerator
    {
        private readonly ITerrainGenerator _baseGenerator;
        private readonly ILoggerService _loggerService;

        public LoggingTerrainGeneratorDecorator(ITerrainGenerator baseGenerator, ILoggerService loggerService)
        {
            _baseGenerator = baseGenerator ?? throw new ArgumentNullException(nameof(baseGenerator));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Generates terrain with comprehensive logging
        /// </summary>
        public TileMap GenerateTerrain(GenerationConfig config, int seed)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            var algorithmName = _baseGenerator.GetAlgorithmName();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    $"Starting terrain generation with {algorithmName} algorithm", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        Width = config.Width,
                        Height = config.Height,
                        Seed = seed,
                        TotalTiles = config.Width * config.Height,
                        Parameters = config.AlgorithmParameters,
                        Operation = "TerrainGeneration"
                    });

                // Log parameter validation
                var parameterValidation = _baseGenerator.ValidateParameters(config.AlgorithmParameters);
                if (parameterValidation.Any())
                {
                    LogSafely(LogLevel.Warning, 
                        "Terrain generation parameter validation warnings", 
                        new { 
                            OperationId = operationId,
                            Algorithm = algorithmName,
                            ValidationErrors = parameterValidation
                        });
                }

                // Log memory estimation
                var estimatedMemory = EstimateMemoryUsage(config.Width, config.Height);
                LogSafely(LogLevel.Debug, 
                    "Terrain generation memory estimation", 
                    new { 
                        OperationId = operationId,
                        EstimatedMemoryBytes = estimatedMemory,
                        EstimatedMemoryMB = estimatedMemory / (1024.0 * 1024.0)
                    });

                var terrain = _baseGenerator.GenerateTerrain(config, seed);
                
                stopwatch.Stop();
                
                // Calculate terrain statistics for logging
                var terrainStats = CalculateTerrainStatistics(terrain);
                
                LogGenerationSafely(
                    operationId, 
                    "TerrainGeneration", 
                    stopwatch.Elapsed,
                    new { 
                        Algorithm = algorithmName,
                        Width = terrain.Width,
                        Height = terrain.Height,
                        Seed = seed,
                        TotalTiles = terrain.Width * terrain.Height,
                        TerrainStatistics = terrainStats,
                        Parameters = config.AlgorithmParameters
                    });
                
                LogPerformanceSafely(
                    $"TerrainGeneration_{algorithmName}",
                    stopwatch.Elapsed,
                    new {
                        TilesPerSecond = (terrain.Width * terrain.Height) / stopwatch.Elapsed.TotalSeconds,
                        MemoryEstimate = EstimateMemoryUsage(terrain.Width, terrain.Height),
                        TerrainComplexity = CalculateTerrainComplexity(terrain),
                        ParameterCount = config.AlgorithmParameters?.Count ?? 0
                    });
                
                LogSafely(LogLevel.Information, 
                    $"Terrain generation completed successfully with {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Algorithm = algorithmName,
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        TilesGenerated = terrain.Width * terrain.Height,
                        TerrainStats = terrainStats
                    });
                
                return terrain;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    $"Terrain generation failed with {algorithmName} algorithm", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Width = config.Width,
                        Height = config.Height,
                        Seed = seed,
                        Parameters = config.AlgorithmParameters
                    });
                throw;
            }
        }

        /// <summary>
        /// Checks if this generator supports the given parameters with logging
        /// </summary>
        public bool SupportsParameters(Dictionary<string, object> parameters)
        {
            var operationId = Guid.NewGuid().ToString();
            var algorithmName = _baseGenerator.GetAlgorithmName();
            
            try
            {
                LogSafely(LogLevel.Debug, 
                    $"Checking parameter support for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        Parameters = parameters,
                        Operation = "ParameterSupportCheck"
                    });

                var supports = _baseGenerator.SupportsParameters(parameters);
                
                LogSafely(LogLevel.Debug, 
                    $"Parameter support check completed for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        Supports = supports,
                        ParameterCount = parameters?.Count ?? 0
                    });

                return supports;
            }
            catch (Exception ex)
            {
                LogErrorSafely(ex, 
                    $"Parameter support check failed for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        Parameters = parameters
                    });
                return false;
            }
        }

        /// <summary>
        /// Gets the name of this generation algorithm
        /// </summary>
        public string GetAlgorithmName() => _baseGenerator.GetAlgorithmName();

        /// <summary>
        /// Gets the default parameters for this algorithm with logging
        /// </summary>
        public Dictionary<string, object> GetDefaultParameters()
        {
            var operationId = Guid.NewGuid().ToString();
            var algorithmName = _baseGenerator.GetAlgorithmName();
            
            try
            {
                LogSafely(LogLevel.Debug, 
                    $"Retrieving default parameters for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        Operation = "DefaultParametersRetrieval"
                    });

                var defaultParams = _baseGenerator.GetDefaultParameters();
                
                LogSafely(LogLevel.Debug, 
                    $"Default parameters retrieved for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        DefaultParameters = defaultParams,
                        ParameterCount = defaultParams?.Count ?? 0
                    });

                return defaultParams;
            }
            catch (Exception ex)
            {
                LogErrorSafely(ex, 
                    $"Failed to retrieve default parameters for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName
                    });
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Validates algorithm-specific parameters with logging
        /// </summary>
        public List<string> ValidateParameters(Dictionary<string, object> parameters)
        {
            var operationId = Guid.NewGuid().ToString();
            var algorithmName = _baseGenerator.GetAlgorithmName();
            
            try
            {
                LogSafely(LogLevel.Debug, 
                    $"Validating parameters for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        Parameters = parameters,
                        ParameterCount = parameters?.Count ?? 0,
                        Operation = "ParameterValidation"
                    });

                var validationErrors = _baseGenerator.ValidateParameters(parameters);
                
                var logLevel = validationErrors.Any() ? LogLevel.Warning : LogLevel.Debug;
                LogSafely(logLevel, 
                    $"Parameter validation completed for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        IsValid = !validationErrors.Any(),
                        ErrorCount = validationErrors.Count,
                        ValidationErrors = validationErrors
                    });

                return validationErrors;
            }
            catch (Exception ex)
            {
                LogErrorSafely(ex, 
                    $"Parameter validation failed for {algorithmName}", 
                    new { 
                        OperationId = operationId,
                        Algorithm = algorithmName,
                        Parameters = parameters
                    });
                return new List<string> { $"Validation failed with exception: {ex.Message}" };
            }
        }

        /// <summary>
        /// Calculates terrain statistics for logging
        /// </summary>
        private Dictionary<string, object> CalculateTerrainStatistics(TileMap terrain)
        {
            var tileCounts = new Dictionary<TileType, int>();
            var totalTiles = terrain.Width * terrain.Height;
            var walkableTiles = 0;
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tileType = terrain.GetTile(x, y);
                    tileCounts[tileType] = tileCounts.GetValueOrDefault(tileType, 0) + 1;
                    
                    if (terrain.IsWalkable(x, y))
                        walkableTiles++;
                }
            }
            
            return new Dictionary<string, object>
            {
                ["TotalTiles"] = totalTiles,
                ["WalkableTiles"] = walkableTiles,
                ["WalkablePercentage"] = (walkableTiles * 100.0) / totalTiles,
                ["TileComposition"] = tileCounts.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => new { Count = kvp.Value, Percentage = (kvp.Value * 100.0) / totalTiles }
                )
            };
        }

        /// <summary>
        /// Calculates terrain complexity for performance metrics
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
        /// Estimates memory usage for terrain generation
        /// </summary>
        private long EstimateMemoryUsage(int width, int height)
        {
            // Estimate based on tile map size plus overhead
            return width * height * 8; // 8 bytes per tile with overhead
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
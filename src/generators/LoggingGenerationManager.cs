using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Configuration;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Generation manager with comprehensive logging integration
    /// </summary>
    public class LoggingGenerationManager : IGenerationManager
    {
        private readonly IGenerationManager _baseManager;
        private readonly ILoggerService _loggerService;
        private readonly IConfigurationParser _configurationParser;
        private readonly ITerrainGeneratorFactory _terrainGeneratorFactory;
        private readonly IEntityPlacer _entityPlacer;
        private readonly ILevelAssembler _levelAssembler;

        public LoggingGenerationManager(
            IGenerationManager baseManager,
            ILoggerService loggerService,
            IConfigurationParser configurationParser,
            ITerrainGeneratorFactory terrainGeneratorFactory,
            IEntityPlacer entityPlacer,
            ILevelAssembler levelAssembler)
        {
            _baseManager = baseManager ?? throw new ArgumentNullException(nameof(baseManager));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
            _configurationParser = configurationParser ?? throw new ArgumentNullException(nameof(configurationParser));
            _terrainGeneratorFactory = terrainGeneratorFactory ?? throw new ArgumentNullException(nameof(terrainGeneratorFactory));
            _entityPlacer = entityPlacer ?? throw new ArgumentNullException(nameof(entityPlacer));
            _levelAssembler = levelAssembler ?? throw new ArgumentNullException(nameof(levelAssembler));
        }

        /// <summary>
        /// Generates a complete level with comprehensive logging throughout the pipeline
        /// </summary>
        public Level GenerateLevel(GenerationConfig config)
        {
            var overallStopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            var sessionId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting complete level generation pipeline", 
                    new { 
                        OperationId = operationId,
                        SessionId = sessionId,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm,
                        Seed = config.Seed,
                        EntityCount = config.Entities?.Sum(e => e.Count) ?? 0,
                        Operation = "CompleteLevelGeneration"
                    });
                
                // Step 1: Configuration validation with logging
                var validationResult = ValidateConfigurationWithLogging(config, operationId, sessionId);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors)}");
                }
                
                // Step 2: Terrain generation with logging
                var terrain = GenerateTerrainWithLogging(config, operationId, sessionId);
                
                // Step 3: Entity placement with logging
                var entities = PlaceEntitiesWithLogging(terrain, config, operationId, sessionId);
                
                // Step 4: Level assembly with logging
                var level = AssembleLevelWithLogging(terrain, entities, config, operationId, sessionId);
                
                overallStopwatch.Stop();
                
                // Log overall pipeline performance
                LogPerformanceSafely(
                    "CompleteLevelGeneration",
                    overallStopwatch.Elapsed,
                    new {
                        TotalTiles = terrain.Width * terrain.Height,
                        TotalEntities = entities.Count,
                        TilesPerSecond = (terrain.Width * terrain.Height) / overallStopwatch.Elapsed.TotalSeconds,
                        EntitiesPerSecond = entities.Count / overallStopwatch.Elapsed.TotalSeconds,
                        OverallComplexity = CalculateOverallComplexity(terrain, entities, config)
                    });
                
                LogSafely(LogLevel.Information, 
                    "Complete level generation pipeline completed successfully", 
                    new { 
                        OperationId = operationId,
                        SessionId = sessionId,
                        TotalDurationMs = overallStopwatch.ElapsedMilliseconds,
                        LevelName = level.Name,
                        TerrainSize = $"{terrain.Width}x{terrain.Height}",
                        EntityCount = entities.Count,
                        Algorithm = config.GenerationAlgorithm
                    });
                
                return level;
            }
            catch (Exception ex)
            {
                overallStopwatch.Stop();
                LogErrorSafely(ex, 
                    "Complete level generation pipeline failed", 
                    new { 
                        OperationId = operationId,
                        SessionId = sessionId,
                        TotalDurationMs = overallStopwatch.ElapsedMilliseconds,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm,
                        Seed = config.Seed
                    });
                throw;
            }
        }

        /// <summary>
        /// Sets the random seed with logging
        /// </summary>
        public void SetSeed(int seed)
        {
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Debug, 
                    "Setting random seed", 
                    new { 
                        OperationId = operationId,
                        Seed = seed,
                        Operation = "SeedConfiguration"
                    });

                _baseManager.SetSeed(seed);
                
                LogSafely(LogLevel.Debug, 
                    "Random seed set successfully", 
                    new { 
                        OperationId = operationId,
                        Seed = seed
                    });
            }
            catch (Exception ex)
            {
                LogErrorSafely(ex, 
                    "Failed to set random seed", 
                    new { 
                        OperationId = operationId,
                        Seed = seed
                    });
                throw;
            }
        }

        /// <summary>
        /// Registers a terrain generation algorithm with logging
        /// </summary>
        public void RegisterGenerationAlgorithm(string name, ITerrainGenerator generator)
        {
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Registering terrain generation algorithm", 
                    new { 
                        OperationId = operationId,
                        AlgorithmName = name,
                        GeneratorType = generator.GetType().Name,
                        Operation = "AlgorithmRegistration"
                    });

                // Wrap the generator with logging if it's not already wrapped
                var loggingGenerator = generator is LoggingTerrainGeneratorDecorator 
                    ? generator 
                    : new LoggingTerrainGeneratorDecorator(generator, _loggerService);

                _baseManager.RegisterGenerationAlgorithm(name, loggingGenerator);
                
                LogSafely(LogLevel.Information, 
                    "Terrain generation algorithm registered successfully", 
                    new { 
                        OperationId = operationId,
                        AlgorithmName = name,
                        GeneratorType = generator.GetType().Name
                    });
            }
            catch (Exception ex)
            {
                LogErrorSafely(ex, 
                    "Failed to register terrain generation algorithm", 
                    new { 
                        OperationId = operationId,
                        AlgorithmName = name,
                        GeneratorType = generator?.GetType().Name
                    });
                throw;
            }
        }

        /// <summary>
        /// Registers an entity placement algorithm with logging
        /// </summary>
        public void RegisterEntityPlacer(string name, IEntityPlacer placer)
        {
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Registering entity placement algorithm", 
                    new { 
                        OperationId = operationId,
                        PlacerName = name,
                        PlacerType = placer.GetType().Name,
                        Operation = "PlacerRegistration"
                    });

                // Wrap the placer with logging if it's not already wrapped
                var loggingPlacer = placer is LoggingEntityPlacer 
                    ? placer 
                    : new LoggingEntityPlacer(placer, _loggerService);

                _baseManager.RegisterEntityPlacer(name, loggingPlacer);
                
                LogSafely(LogLevel.Information, 
                    "Entity placement algorithm registered successfully", 
                    new { 
                        OperationId = operationId,
                        PlacerName = name,
                        PlacerType = placer.GetType().Name
                    });
            }
            catch (Exception ex)
            {
                LogErrorSafely(ex, 
                    "Failed to register entity placement algorithm", 
                    new { 
                        OperationId = operationId,
                        PlacerName = name,
                        PlacerType = placer?.GetType().Name
                    });
                throw;
            }
        }

        /// <summary>
        /// Validates configuration with detailed logging
        /// </summary>
        private (bool IsValid, List<string> Errors) ValidateConfigurationWithLogging(
            GenerationConfig config, string operationId, string sessionId)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting configuration validation", 
                    new { 
                        OperationId = operationId,
                        SessionId = sessionId,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm,
                        Operation = "ConfigurationValidation"
                    });
                
                var isValid = _configurationParser.ValidateConfig(config, out var errors);
                
                stopwatch.Stop();
                
                LogGenerationSafely(
                    operationId, 
                    "ConfigurationValidation", 
                    stopwatch.Elapsed,
                    new { 
                        SessionId = sessionId,
                        IsValid = isValid,
                        ErrorCount = errors.Count,
                        Errors = errors
                    });
                
                var logLevel = isValid ? LogLevel.Information : LogLevel.Warning;
                LogSafely(logLevel, 
                    $"Configuration validation completed - {(isValid ? "Valid" : "Invalid")}", 
                    new { 
                        OperationId = operationId,
                        SessionId = sessionId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        IsValid = isValid,
                        ErrorCount = errors.Count
                    });
                
                return (isValid, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Configuration validation failed with exception", 
                    new { 
                        OperationId = operationId,
                        SessionId = sessionId,
                        DurationMs = stopwatch.ElapsedMilliseconds
                    });
                
                return (false, new List<string> { $"Validation failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Generates terrain with comprehensive logging
        /// </summary>
        private TileMap GenerateTerrainWithLogging(
            GenerationConfig config, string operationId, string sessionId)
        {
            var generator = _terrainGeneratorFactory.CreateGenerator(config.GenerationAlgorithm);
            return generator.GenerateTerrain(config, config.Seed);
        }

        /// <summary>
        /// Places entities with comprehensive logging
        /// </summary>
        private List<Entity> PlaceEntitiesWithLogging(
            TileMap terrain, GenerationConfig config, string operationId, string sessionId)
        {
            return _entityPlacer.PlaceEntities(terrain, config, config.Seed);
        }

        /// <summary>
        /// Assembles level with comprehensive logging
        /// </summary>
        private Level AssembleLevelWithLogging(
            TileMap terrain, List<Entity> entities, GenerationConfig config, string operationId, string sessionId)
        {
            return _levelAssembler.AssembleLevel(terrain, entities, config);
        }

        /// <summary>
        /// Calculates overall complexity for performance metrics
        /// </summary>
        private double CalculateOverallComplexity(TileMap terrain, List<Entity> entities, GenerationConfig config)
        {
            var sizeComplexity = Math.Log10(terrain.Width * terrain.Height) / 10.0;
            var entityComplexity = entities.Count / 100.0;
            var parameterComplexity = (config.AlgorithmParameters?.Count ?? 0) / 10.0;
            
            return (sizeComplexity + entityComplexity + parameterComplexity) / 3.0;
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
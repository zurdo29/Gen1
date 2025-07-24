using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Integration tests for logging throughout the generation pipeline
    /// </summary>
    public static class LoggingIntegrationTests
    {
        /// <summary>
        /// Runs all logging integration tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Logging Integration Tests ===");
            
            try
            {
                TestConfigurationParserLogging();
                TestTerrainGeneratorLogging();
                TestEntityPlacerLogging();
                TestLevelAssemblerLogging();
                TestCompleteGenerationPipelineLogging();
                TestErrorLogging();
                TestPerformanceLogging();
                
                Console.WriteLine("✓ All logging integration tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Logging integration tests failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Tests configuration parser logging integration
        /// </summary>
        private static void TestConfigurationParserLogging()
        {
            Console.WriteLine("Testing configuration parser logging...");
            
            var mockLogger = new MockLoggerService();
            var baseParser = new ConfigurationParser();
            var loggingParser = new LoggingConfigurationParser(baseParser, mockLogger);
            
            // Test successful parsing
            var config = loggingParser.GetDefaultConfig();
            
            // Verify logging calls were made
            if (!mockLogger.LogCalls.Any(call => call.Message.Contains("Creating default configuration")))
            {
                throw new Exception("Configuration parser logging not working - missing log calls");
            }
            
            // Test validation logging
            var isValid = loggingParser.ValidateConfig(config, out var errors);
            
            if (!mockLogger.LogCalls.Any(call => call.Message.Contains("Configuration validation")))
            {
                throw new Exception("Configuration validation logging not working");
            }
            
            Console.WriteLine("✓ Configuration parser logging test passed");
        }

        /// <summary>
        /// Tests terrain generator logging integration
        /// </summary>
        private static void TestTerrainGeneratorLogging()
        {
            Console.WriteLine("Testing terrain generator logging...");
            
            var mockLogger = new MockLoggerService();
            var randomGenerator = new RandomGenerator();
            var baseGenerator = new PerlinNoiseGenerator(randomGenerator);
            var loggingGenerator = new LoggingTerrainGeneratorDecorator(baseGenerator, mockLogger);
            
            var config = new GenerationConfig
            {
                Width = 20,
                Height = 20,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "scale", 0.1f },
                    { "octaves", 4 }
                }
            };
            
            var terrain = loggingGenerator.GenerateTerrain(config, 12345);
            
            // Verify logging calls were made
            if (!mockLogger.LogCalls.Any(call => call.Message.Contains("terrain generation")))
            {
                throw new Exception("Terrain generator logging not working - missing log calls");
            }
            
            if (!mockLogger.PerformanceCalls.Any(call => call.Operation.Contains("TerrainGeneration")))
            {
                throw new Exception("Terrain generator performance logging not working");
            }
            
            Console.WriteLine("✓ Terrain generator logging test passed");
        }

        /// <summary>
        /// Tests entity placer logging integration
        /// </summary>
        private static void TestEntityPlacerLogging()
        {
            Console.WriteLine("Testing entity placer logging...");
            
            var mockLogger = new MockLoggerService();
            var randomGenerator = new RandomGenerator();
            var basePlacer = new EntityPlacer(randomGenerator);
            var loggingPlacer = new LoggingEntityPlacer(basePlacer, mockLogger);
            
            // Create a simple terrain
            var terrain = new TileMap(10, 10);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = 2,
                        PlacementStrategy = "random"
                    }
                }
            };
            
            var entities = loggingPlacer.PlaceEntities(terrain, config, 12345);
            
            // Verify logging calls were made
            if (!mockLogger.LogCalls.Any(call => call.Message.Contains("entity placement")))
            {
                throw new Exception("Entity placer logging not working - missing log calls");
            }
            
            if (!mockLogger.GenerationCalls.Any(call => call.Step.Contains("EntityPlacement")))
            {
                throw new Exception("Entity placer generation logging not working");
            }
            
            Console.WriteLine("✓ Entity placer logging test passed");
        }

        /// <summary>
        /// Tests level assembler logging integration
        /// </summary>
        private static void TestLevelAssemblerLogging()
        {
            Console.WriteLine("Testing level assembler logging...");
            
            var mockLogger = new MockLoggerService();
            var baseAssembler = new LevelAssembler();
            var loggingAssembler = new LoggingLevelAssembler(baseAssembler, mockLogger);
            
            // Create test data
            var terrain = new TileMap(5, 5);
            var entities = new List<Entity>();
            var config = new GenerationConfig { Width = 5, Height = 5, GenerationAlgorithm = "test" };
            
            var level = loggingAssembler.AssembleLevel(terrain, entities, config);
            
            // Verify logging calls were made
            if (!mockLogger.LogCalls.Any(call => call.Message.Contains("level assembly")))
            {
                throw new Exception("Level assembler logging not working - missing log calls");
            }
            
            if (!mockLogger.GenerationCalls.Any(call => call.Step.Contains("LevelAssembly")))
            {
                throw new Exception("Level assembler generation logging not working");
            }
            
            Console.WriteLine("✓ Level assembler logging test passed");
        }

        /// <summary>
        /// Tests complete generation pipeline logging
        /// </summary>
        private static void TestCompleteGenerationPipelineLogging()
        {
            Console.WriteLine("Testing complete generation pipeline logging...");
            
            var mockLogger = new MockLoggerService();
            
            // Set up services
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerService>(mockLogger);
            services.AddLoggingIntegratedGenerationServices();
            services.AddLoggingIntegratedTerrainGenerators();
            services.AddTransient<IRandomGenerator, RandomGenerator>();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Create logging orchestrator (similar to the one in the web API)
            var orchestrator = new LoggingGenerationOrchestrator(
                mockLogger,
                serviceProvider.GetRequiredService<IPluginLoader>(),
                serviceProvider);
            
            var config = new GenerationConfig
            {
                Width = 15,
                Height = 15,
                GenerationAlgorithm = "perlin",
                Seed = 12345,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 1, PlacementStrategy = "random" }
                }
            };
            
            var level = orchestrator.GenerateLevelAsync(config).Result;
            
            // Verify comprehensive logging
            var expectedLogMessages = new[]
            {
                "Starting complete level generation pipeline",
                "Configuration validation",
                "terrain generation",
                "entity placement",
                "level assembly",
                "completed successfully"
            };
            
            foreach (var expectedMessage in expectedLogMessages)
            {
                if (!mockLogger.LogCalls.Any(call => call.Message.ToLower().Contains(expectedMessage.ToLower())))
                {
                    throw new Exception($"Missing expected log message: {expectedMessage}");
                }
            }
            
            // Verify performance logging
            if (!mockLogger.PerformanceCalls.Any(call => call.Operation.Contains("CompleteLevelGeneration")))
            {
                throw new Exception("Missing complete generation performance logging");
            }
            
            Console.WriteLine("✓ Complete generation pipeline logging test passed");
        }

        /// <summary>
        /// Tests error logging integration
        /// </summary>
        private static void TestErrorLogging()
        {
            Console.WriteLine("Testing error logging...");
            
            var mockLogger = new MockLoggerService();
            var baseParser = new ConfigurationParser();
            var loggingParser = new LoggingConfigurationParser(baseParser, mockLogger);
            
            try
            {
                // This should cause an error
                loggingParser.ParseConfig("nonexistent-file.json");
            }
            catch (Exception)
            {
                // Expected to fail
            }
            
            // Verify error logging
            if (!mockLogger.ErrorCalls.Any(call => call.Context.Contains("parsing failed")))
            {
                throw new Exception("Error logging not working");
            }
            
            Console.WriteLine("✓ Error logging test passed");
        }

        /// <summary>
        /// Tests performance logging integration
        /// </summary>
        private static void TestPerformanceLogging()
        {
            Console.WriteLine("Testing performance logging...");
            
            var mockLogger = new MockLoggerService();
            var randomGenerator = new RandomGenerator();
            var baseGenerator = new PerlinNoiseGenerator(randomGenerator);
            var loggingGenerator = new LoggingTerrainGeneratorDecorator(baseGenerator, mockLogger);
            
            var config = new GenerationConfig
            {
                Width = 50,
                Height = 50,
                GenerationAlgorithm = "perlin"
            };
            
            var stopwatch = Stopwatch.StartNew();
            var terrain = loggingGenerator.GenerateTerrain(config, 12345);
            stopwatch.Stop();
            
            // Verify performance metrics were logged
            var performanceCall = mockLogger.PerformanceCalls.FirstOrDefault(call => 
                call.Operation.Contains("TerrainGeneration"));
            
            if (performanceCall == null)
            {
                throw new Exception("Performance logging not working");
            }
            
            if (performanceCall.Duration <= TimeSpan.Zero)
            {
                throw new Exception("Performance duration not logged correctly");
            }
            
            if (performanceCall.Metrics == null || !performanceCall.Metrics.ContainsKey("TilesPerSecond"))
            {
                throw new Exception("Performance metrics not logged correctly");
            }
            
            Console.WriteLine("✓ Performance logging test passed");
        }
    }

    /// <summary>
    /// Mock logger service for testing
    /// </summary>
    public class MockLoggerService : ILoggerService
    {
        public List<LogCall> LogCalls { get; } = new List<LogCall>();
        public List<GenerationCall> GenerationCalls { get; } = new List<GenerationCall>();
        public List<ErrorCall> ErrorCalls { get; } = new List<ErrorCall>();
        public List<PerformanceCall> PerformanceCalls { get; } = new List<PerformanceCall>();
        public List<RequestCall> RequestCalls { get; } = new List<RequestCall>();

        public Task LogAsync(LogLevel level, string message, object? context = null)
        {
            LogCalls.Add(new LogCall { Level = level, Message = message, Context = context });
            return Task.CompletedTask;
        }

        public Task LogGenerationAsync(string configId, string step, TimeSpan duration, object? metadata = null)
        {
            GenerationCalls.Add(new GenerationCall { ConfigId = configId, Step = step, Duration = duration, Metadata = metadata });
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(Exception exception, string context, object? additionalData = null)
        {
            ErrorCalls.Add(new ErrorCall { Exception = exception, Context = context, AdditionalData = additionalData });
            return Task.CompletedTask;
        }

        public Task LogPerformanceAsync(string operation, TimeSpan duration, object? metrics = null)
        {
            PerformanceCalls.Add(new PerformanceCall { Operation = operation, Duration = duration, Metrics = metrics as Dictionary<string, object> });
            return Task.CompletedTask;
        }

        public Task LogRequestAsync(string requestId, string method, string path, int statusCode, TimeSpan duration)
        {
            RequestCalls.Add(new RequestCall { RequestId = requestId, Method = method, Path = path, StatusCode = statusCode, Duration = duration });
            return Task.CompletedTask;
        }

        public ILoggerService CreateScoped(string scope, object? context = null)
        {
            return this; // For testing, return the same instance
        }

        public class LogCall
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; } = string.Empty;
            public object? Context { get; set; }
        }

        public class GenerationCall
        {
            public string ConfigId { get; set; } = string.Empty;
            public string Step { get; set; } = string.Empty;
            public TimeSpan Duration { get; set; }
            public object? Metadata { get; set; }
        }

        public class ErrorCall
        {
            public Exception Exception { get; set; } = null!;
            public string Context { get; set; } = string.Empty;
            public object? AdditionalData { get; set; }
        }

        public class PerformanceCall
        {
            public string Operation { get; set; } = string.Empty;
            public TimeSpan Duration { get; set; }
            public Dictionary<string, object>? Metrics { get; set; }
        }

        public class RequestCall
        {
            public string RequestId { get; set; } = string.Empty;
            public string Method { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public int StatusCode { get; set; }
            public TimeSpan Duration { get; set; }
        }
    }
}
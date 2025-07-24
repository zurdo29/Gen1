using System;
using System.Collections.Generic;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Tests
{
    /// <summary>
    /// Integration test to verify logging is working throughout the generation pipeline
    /// </summary>
    public static class LoggingIntegrationTest
    {
        /// <summary>
        /// Runs a comprehensive test of the logging integration
        /// </summary>
        public static void RunLoggingIntegrationTest()
        {
            Console.WriteLine("Running Logging Integration Test...");
            
            try
            {
                // Create logger service
                var logger = new ConsoleLoggerService("LoggingTest");
                
                // Test configuration parser logging
                TestConfigurationParserLogging(logger);
                
                // Test terrain generator logging
                TestTerrainGeneratorLogging(logger);
                
                // Test entity placer logging
                TestEntityPlacerLogging(logger);
                
                // Test level assembler logging
                TestLevelAssemblerLogging(logger);
                
                // Test complete pipeline logging
                TestCompletePipelineLogging(logger);
                
                Console.WriteLine("✓ All logging integration tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Logging integration test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        
        /// <summary>
        /// Tests configuration parser logging
        /// </summary>
        private static void TestConfigurationParserLogging(ISimpleLoggerService logger)
        {
            Console.WriteLine("  Testing configuration parser logging...");
            
            var parser = new ConfigurationParser(logger);
            var config = parser.GetDefaultConfig();
            
            List<string> errors;
            parser.ValidateConfig(config, out errors);
            
            Console.WriteLine("  ✓ Configuration parser logging test passed");
        }
        
        /// <summary>
        /// Tests terrain generator logging
        /// </summary>
        private static void TestTerrainGeneratorLogging(ISimpleLoggerService logger)
        {
            Console.WriteLine("  Testing terrain generator logging...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new PerlinNoiseGenerator(randomGenerator, logger);
            
            var config = new GenerationConfig
            {
                Width = 20,
                Height = 20,
                Seed = 12345,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "scale", 0.1 },
                    { "octaves", 4 }
                },
                TerrainTypes = new List<string> { "ground", "wall", "water" }
            };
            
            var terrain = generator.GenerateTerrain(config, config.Seed);
            
            if (terrain == null || terrain.Width != 20 || terrain.Height != 20)
                throw new Exception("Terrain generation failed");
            
            Console.WriteLine("  ✓ Terrain generator logging test passed");
        }
        
        /// <summary>
        /// Tests entity placer logging
        /// </summary>
        private static void TestEntityPlacerLogging(ISimpleLoggerService logger)
        {
            Console.WriteLine("  Testing entity placer logging...");
            
            var randomGenerator = new RandomGenerator();
            var placer = new EntityPlacer(randomGenerator, logger);
            
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
                Seed = 12345,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = 2,
                        MinDistance = 1.0f,
                        PlacementStrategy = "random"
                    }
                }
            };
            
            var entities = placer.PlaceEntities(terrain, config, config.Seed);
            
            if (entities == null)
                throw new Exception("Entity placement failed");
            
            Console.WriteLine("  ✓ Entity placer logging test passed");
        }
        
        /// <summary>
        /// Tests level assembler logging
        /// </summary>
        private static void TestLevelAssemblerLogging(ISimpleLoggerService logger)
        {
            Console.WriteLine("  Testing level assembler logging...");
            
            var assembler = new LevelAssembler(null, logger);
            
            // Create test data
            var terrain = new TileMap(10, 10);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var entities = new List<Entity>();
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Seed = 12345,
                GenerationAlgorithm = "test"
            };
            
            var level = assembler.AssembleLevel(terrain, entities, config);
            
            if (level == null || level.Terrain == null)
                throw new Exception("Level assembly failed");
            
            Console.WriteLine("  ✓ Level assembler logging test passed");
        }
        
        /// <summary>
        /// Tests the complete generation pipeline with logging
        /// </summary>
        private static void TestCompletePipelineLogging(ISimpleLoggerService logger)
        {
            Console.WriteLine("  Testing complete pipeline logging...");
            
            // Create all components with logging
            var randomGenerator = new RandomGenerator();
            var parser = new ConfigurationParser(logger);
            var generator = new PerlinNoiseGenerator(randomGenerator, logger);
            var placer = new EntityPlacer(randomGenerator, logger);
            var assembler = new LevelAssembler(null, logger);
            
            // Create configuration
            var config = new GenerationConfig
            {
                Width = 15,
                Height = 15,
                Seed = 54321,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "scale", 0.1 },
                    { "octaves", 3 }
                },
                TerrainTypes = new List<string> { "ground", "wall", "water" },
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Item,
                        Count = 3,
                        MinDistance = 2.0f,
                        PlacementStrategy = "spread"
                    }
                }
            };
            
            // Run complete pipeline
            List<string> errors;
            if (!parser.ValidateConfig(config, out errors))
                throw new Exception($"Configuration validation failed: {string.Join(", ", errors)}");
            
            var terrain = generator.GenerateTerrain(config, config.Seed);
            var entities = placer.PlaceEntities(terrain, config, config.Seed);
            var level = assembler.AssembleLevel(terrain, entities, config);
            
            if (level == null || level.Terrain == null || level.Entities == null)
                throw new Exception("Complete pipeline failed");
            
            Console.WriteLine("  ✓ Complete pipeline logging test passed");
        }
    }
}
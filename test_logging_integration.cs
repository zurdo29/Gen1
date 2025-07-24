using System;
using System.Collections.Generic;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Tests
{
    /// <summary>
    /// Simple test to verify logging integration is working
    /// </summary>
    public static class LoggingIntegrationVerification
    {
        /// <summary>
        /// Runs a simple test to verify logging integration
        /// </summary>
        public static void RunSimpleTest()
        {
            Console.WriteLine("Running simple logging integration verification...");
            
            try
            {
                // Create logger service
                var logger = new ConsoleLoggerService("LoggingVerification");
                
                // Test configuration parser logging
                Console.WriteLine("  Testing configuration parser logging...");
                var parser = new ConfigurationParser(logger);
                var config = parser.GetDefaultConfig();
                List<string> errors;
                parser.ValidateConfig(config, out errors);
                
                // Test terrain generator logging
                Console.WriteLine("  Testing terrain generator logging...");
                var randomGenerator = new RandomGenerator();
                var generator = new PerlinNoiseGenerator(randomGenerator, logger);
                var terrain = generator.GenerateTerrain(config, config.Seed);
                
                // Test entity placer logging
                Console.WriteLine("  Testing entity placer logging...");
                var placer = new EntityPlacer(randomGenerator, logger);
                var entities = placer.PlaceEntities(terrain, config, config.Seed);
                
                // Test level assembler logging
                Console.WriteLine("  Testing level assembler logging...");
                var assembler = new LevelAssembler(null, logger);
                var level = assembler.AssembleLevel(terrain, entities, config);
                
                Console.WriteLine("✓ Simple logging integration verification passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Simple logging integration verification failed: {ex.Message}");
                throw;
            }
        }
    }
}
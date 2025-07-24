using System;
using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Basic tests for terrain generator interface and base functionality
    /// </summary>
    public class TerrainGeneratorTests
    {
        /// <summary>
        /// Tests basic terrain generation functionality
        /// </summary>
        public static void TestBasicGeneration()
        {
            Console.WriteLine("Testing basic terrain generation...");
            
            var randomGenerator = new RandomGenerator(12345);
            var generator = new TestTerrainGenerator(randomGenerator);
            
            var config = new GenerationConfig
            {
                Width = 20,
                Height = 20,
                GenerationAlgorithm = "test",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "fillPercentage", 0.3 },
                    { "groundType", "ground" }
                }
            };
            
            var tileMap = generator.GenerateTerrain(config, 12345);
            
            // Verify basic properties
            if (tileMap.Width != 20 || tileMap.Height != 20)
            {
                throw new Exception($"Expected 20x20 map, got {tileMap.Width}x{tileMap.Height}");
            }
            
            // Verify borders are walls
            for (int x = 0; x < tileMap.Width; x++)
            {
                if (tileMap.GetTile(x, 0) != TileType.Wall || tileMap.GetTile(x, tileMap.Height - 1) != TileType.Wall)
                {
                    throw new Exception("Top or bottom border is not a wall");
                }
            }
            
            for (int y = 0; y < tileMap.Height; y++)
            {
                if (tileMap.GetTile(0, y) != TileType.Wall || tileMap.GetTile(tileMap.Width - 1, y) != TileType.Wall)
                {
                    throw new Exception("Left or right border is not a wall");
                }
            }
            
            // Count different tile types
            int groundCount = 0, wallCount = 0;
            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    var tile = tileMap.GetTile(x, y);
                    if (tile == TileType.Ground) groundCount++;
                    else if (tile == TileType.Wall) wallCount++;
                }
            }
            
            Console.WriteLine($"Generated map with {groundCount} ground tiles and {wallCount} wall tiles");
            
            // Test reproducibility
            var tileMap2 = generator.GenerateTerrain(config, 12345);
            
            bool identical = true;
            for (int x = 0; x < tileMap.Width && identical; x++)
            {
                for (int y = 0; y < tileMap.Height && identical; y++)
                {
                    if (tileMap.GetTile(x, y) != tileMap2.GetTile(x, y))
                    {
                        identical = false;
                    }
                }
            }
            
            if (!identical)
            {
                throw new Exception("Same seed should produce identical results");
            }
            
            Console.WriteLine("✓ Basic terrain generation test passed");
        }
        
        /// <summary>
        /// Tests parameter validation
        /// </summary>
        public static void TestParameterValidation()
        {
            Console.WriteLine("Testing parameter validation...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new TestTerrainGenerator(randomGenerator);
            
            // Test valid parameters
            var validParams = new Dictionary<string, object>
            {
                { "fillPercentage", 0.5 },
                { "groundType", "grass" }
            };
            
            if (!generator.SupportsParameters(validParams))
            {
                throw new Exception("Valid parameters should be supported");
            }
            
            // Test invalid parameters
            var invalidParams = new Dictionary<string, object>
            {
                { "fillPercentage", 1.5 }, // Invalid: > 1.0
                { "groundType", "grass" }
            };
            
            if (generator.SupportsParameters(invalidParams))
            {
                throw new Exception("Invalid parameters should not be supported");
            }
            
            var errors = generator.ValidateParameters(invalidParams);
            if (errors.Count == 0)
            {
                throw new Exception("Should have validation errors for invalid parameters");
            }
            
            Console.WriteLine($"✓ Parameter validation test passed (found {errors.Count} expected errors)");
        }
        
        /// <summary>
        /// Tests algorithm name and default parameters
        /// </summary>
        public static void TestAlgorithmInfo()
        {
            Console.WriteLine("Testing algorithm info...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new TestTerrainGenerator(randomGenerator);
            
            var name = generator.GetAlgorithmName();
            if (string.IsNullOrEmpty(name))
            {
                throw new Exception("Algorithm name should not be null or empty");
            }
            
            var defaults = generator.GetDefaultParameters();
            if (defaults == null || defaults.Count == 0)
            {
                throw new Exception("Should have default parameters");
            }
            
            Console.WriteLine($"✓ Algorithm info test passed (name: {name}, {defaults.Count} default parameters)");
        }
        
        /// <summary>
        /// Runs all tests
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                TestBasicGeneration();
                TestParameterValidation();
                TestAlgorithmInfo();
                Console.WriteLine("✓ All terrain generator tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Test failed: {ex.Message}");
                throw;
            }
        }
    }
}
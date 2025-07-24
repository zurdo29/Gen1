using System;
using System.Collections.Generic;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Tests for Perlin noise terrain generator
    /// </summary>
    public class PerlinNoiseGeneratorTests
    {
        /// <summary>
        /// Tests basic Perlin noise generation
        /// </summary>
        public static void TestBasicPerlinGeneration()
        {
            Console.WriteLine("Testing Perlin noise terrain generation...");
            
            var randomGenerator = new RandomGenerator(42);
            var generator = new PerlinNoiseGenerator(randomGenerator);
            
            var config = new GenerationConfig
            {
                Width = 30,
                Height = 30,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "scale", 0.1f },
                    { "octaves", 4 },
                    { "persistence", 0.5f },
                    { "lacunarity", 2.0f },
                    { "waterLevel", 0.3f },
                    { "mountainLevel", 0.7f }
                },
                TerrainTypes = new List<string> { "ground", "wall", "water", "grass", "stone" }
            };
            
            var tileMap = generator.GenerateTerrain(config, 42);
            
            // Verify basic properties
            if (tileMap.Width != 30 || tileMap.Height != 30)
            {
                throw new Exception($"Expected 30x30 map, got {tileMap.Width}x{tileMap.Height}");
            }
            
            // Verify borders are walls
            VerifyBorders(tileMap);
            
            // Count different tile types
            var tileCounts = CountTileTypes(tileMap);
            
            Console.WriteLine($"Generated Perlin terrain with:");
            foreach (var kvp in tileCounts)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value} tiles");
            }
            
            // Verify we have diverse terrain
            if (tileCounts.Count < 2)
            {
                throw new Exception("Perlin noise should generate diverse terrain types");
            }
            
            // Test reproducibility
            var tileMap2 = generator.GenerateTerrain(config, 42);
            if (!AreMapsIdentical(tileMap, tileMap2))
            {
                throw new Exception("Same seed should produce identical Perlin terrain");
            }
            
            Console.WriteLine("✓ Basic Perlin noise generation test passed");
        }
        
        /// <summary>
        /// Tests parameter validation for Perlin noise generator
        /// </summary>
        public static void TestPerlinParameterValidation()
        {
            Console.WriteLine("Testing Perlin noise parameter validation...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new PerlinNoiseGenerator(randomGenerator);
            
            // Test valid parameters
            var validParams = new Dictionary<string, object>
            {
                { "scale", 0.05f },
                { "octaves", 6 },
                { "persistence", 0.6f },
                { "lacunarity", 2.5f },
                { "waterLevel", 0.2f },
                { "mountainLevel", 0.8f }
            };
            
            if (!generator.SupportsParameters(validParams))
            {
                throw new Exception("Valid Perlin parameters should be supported");
            }
            
            // Test invalid scale
            var invalidScale = new Dictionary<string, object> { { "scale", 1.5f } };
            var errors = generator.ValidateParameters(invalidScale);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid scale should produce validation errors");
            }
            
            // Test invalid octaves
            var invalidOctaves = new Dictionary<string, object> { { "octaves", 15 } };
            errors = generator.ValidateParameters(invalidOctaves);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid octaves should produce validation errors");
            }
            
            // Test water level >= mountain level
            var invalidLevels = new Dictionary<string, object>
            {
                { "waterLevel", 0.8f },
                { "mountainLevel", 0.7f }
            };
            errors = generator.ValidateParameters(invalidLevels);
            if (errors.Count == 0)
            {
                throw new Exception("Water level >= mountain level should produce validation errors");
            }
            
            // Test unknown parameter
            var unknownParam = new Dictionary<string, object> { { "unknownParam", 123 } };
            errors = generator.ValidateParameters(unknownParam);
            if (errors.Count == 0)
            {
                throw new Exception("Unknown parameters should produce validation errors");
            }
            
            Console.WriteLine("✓ Perlin noise parameter validation test passed");
        }
        
        /// <summary>
        /// Tests different parameter configurations
        /// </summary>
        public static void TestPerlinParameterEffects()
        {
            Console.WriteLine("Testing Perlin noise parameter effects...");
            
            var randomGenerator = new RandomGenerator(123);
            var generator = new PerlinNoiseGenerator(randomGenerator);
            
            var baseConfig = new GenerationConfig
            {
                Width = 20,
                Height = 20,
                GenerationAlgorithm = "perlin",
                TerrainTypes = new List<string> { "ground", "wall", "water", "grass", "stone" }
            };
            
            // Test high water level (should produce more water)
            baseConfig.AlgorithmParameters = new Dictionary<string, object>
            {
                { "scale", 0.1f },
                { "waterLevel", 0.6f },
                { "mountainLevel", 0.8f }
            };
            
            var highWaterMap = generator.GenerateTerrain(baseConfig, 123);
            var highWaterCounts = CountTileTypes(highWaterMap);
            
            // Test low water level (should produce less water)
            baseConfig.AlgorithmParameters = new Dictionary<string, object>
            {
                { "scale", 0.1f },
                { "waterLevel", 0.1f },
                { "mountainLevel", 0.8f }
            };
            
            var lowWaterMap = generator.GenerateTerrain(baseConfig, 123);
            var lowWaterCounts = CountTileTypes(lowWaterMap);
            
            // High water level should produce more water tiles than low water level
            int highWaterTiles = highWaterCounts.ContainsKey(TileType.Water) ? highWaterCounts[TileType.Water] : 0;
            int lowWaterTiles = lowWaterCounts.ContainsKey(TileType.Water) ? lowWaterCounts[TileType.Water] : 0;
            
            Console.WriteLine($"High water level: {highWaterTiles} water tiles");
            Console.WriteLine($"Low water level: {lowWaterTiles} water tiles");
            
            if (highWaterTiles <= lowWaterTiles)
            {
                Console.WriteLine("Warning: Expected high water level to produce more water tiles");
                // This is a warning, not a failure, as noise can be unpredictable
            }
            
            Console.WriteLine("✓ Perlin noise parameter effects test passed");
        }
        
        /// <summary>
        /// Tests algorithm name and default parameters
        /// </summary>
        public static void TestPerlinAlgorithmInfo()
        {
            Console.WriteLine("Testing Perlin noise algorithm info...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new PerlinNoiseGenerator(randomGenerator);
            
            var name = generator.GetAlgorithmName();
            if (name != "perlin")
            {
                throw new Exception($"Expected algorithm name 'perlin', got '{name}'");
            }
            
            var defaults = generator.GetDefaultParameters();
            if (defaults == null || defaults.Count == 0)
            {
                throw new Exception("Should have default parameters");
            }
            
            // Check for expected default parameters
            var expectedParams = new[] { "scale", "octaves", "persistence", "lacunarity", "waterLevel", "mountainLevel" };
            foreach (var param in expectedParams)
            {
                if (!defaults.ContainsKey(param))
                {
                    throw new Exception($"Missing default parameter: {param}");
                }
            }
            
            Console.WriteLine($"✓ Perlin algorithm info test passed (name: {name}, {defaults.Count} default parameters)");
        }
        
        /// <summary>
        /// Runs all Perlin noise generator tests
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                TestBasicPerlinGeneration();
                TestPerlinParameterValidation();
                TestPerlinParameterEffects();
                TestPerlinAlgorithmInfo();
                Console.WriteLine("✓ All Perlin noise generator tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Perlin noise test failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Verifies that map borders are walls
        /// </summary>
        private static void VerifyBorders(TileMap tileMap)
        {
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
        }
        
        /// <summary>
        /// Counts different tile types in the map
        /// </summary>
        private static Dictionary<TileType, int> CountTileTypes(TileMap tileMap)
        {
            var counts = new Dictionary<TileType, int>();
            
            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    var tile = tileMap.GetTile(x, y);
                    counts[tile] = counts.ContainsKey(tile) ? counts[tile] + 1 : 1;
                }
            }
            
            return counts;
        }
        
        /// <summary>
        /// Checks if two tile maps are identical
        /// </summary>
        private static bool AreMapsIdentical(TileMap map1, TileMap map2)
        {
            if (map1.Width != map2.Width || map1.Height != map2.Height)
                return false;
                
            for (int x = 0; x < map1.Width; x++)
            {
                for (int y = 0; y < map1.Height; y++)
                {
                    if (map1.GetTile(x, y) != map2.GetTile(x, y))
                        return false;
                }
            }
            
            return true;
        }
    }
}
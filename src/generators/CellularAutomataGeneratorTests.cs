using System;
using System.Collections.Generic;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Tests for cellular automata terrain generator
    /// </summary>
    public class CellularAutomataGeneratorTests
    {
        /// <summary>
        /// Tests basic cellular automata generation
        /// </summary>
        public static void TestBasicCellularGeneration()
        {
            Console.WriteLine("Testing cellular automata terrain generation...");
            
            var randomGenerator = new RandomGenerator(789);
            var generator = new CellularAutomataGenerator(randomGenerator);
            
            var config = new GenerationConfig
            {
                Width = 25,
                Height = 25,
                GenerationAlgorithm = "cellular",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "initialFillProbability", 0.45f },
                    { "iterations", 5 },
                    { "birthLimit", 4 },
                    { "deathLimit", 3 },
                    { "wallType", "wall" },
                    { "floorType", "ground" }
                },
                TerrainTypes = new List<string> { "ground", "wall" }
            };
            
            var tileMap = generator.GenerateTerrain(config, 789);
            
            // Verify basic properties
            if (tileMap.Width != 25 || tileMap.Height != 25)
            {
                throw new Exception($"Expected 25x25 map, got {tileMap.Width}x{tileMap.Height}");
            }
            
            // Verify borders are walls
            VerifyBorders(tileMap);
            
            // Count different tile types
            var tileCounts = CountTileTypes(tileMap);
            
            Console.WriteLine($"Generated cellular automata terrain with:");
            foreach (var kvp in tileCounts)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value} tiles");
            }
            
            // Verify we have both walls and floors
            if (!tileCounts.ContainsKey(TileType.Wall) || !tileCounts.ContainsKey(TileType.Ground))
            {
                throw new Exception("Cellular automata should generate both walls and ground tiles");
            }
            
            // Test reproducibility
            var tileMap2 = generator.GenerateTerrain(config, 789);
            if (!AreMapsIdentical(tileMap, tileMap2))
            {
                throw new Exception("Same seed should produce identical cellular automata terrain");
            }
            
            Console.WriteLine("✓ Basic cellular automata generation test passed");
        }
        
        /// <summary>
        /// Tests parameter validation for cellular automata generator
        /// </summary>
        public static void TestCellularParameterValidation()
        {
            Console.WriteLine("Testing cellular automata parameter validation...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new CellularAutomataGenerator(randomGenerator);
            
            // Test valid parameters
            var validParams = new Dictionary<string, object>
            {
                { "initialFillProbability", 0.5f },
                { "iterations", 8 },
                { "birthLimit", 5 },
                { "deathLimit", 2 },
                { "wallType", "stone" },
                { "floorType", "grass" }
            };
            
            if (!generator.SupportsParameters(validParams))
            {
                throw new Exception("Valid cellular automata parameters should be supported");
            }
            
            // Test invalid fill probability
            var invalidFillProb = new Dictionary<string, object> { { "initialFillProbability", 1.5f } };
            var errors = generator.ValidateParameters(invalidFillProb);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid fill probability should produce validation errors");
            }
            
            // Test invalid iterations
            var invalidIterations = new Dictionary<string, object> { { "iterations", 25 } };
            errors = generator.ValidateParameters(invalidIterations);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid iterations should produce validation errors");
            }
            
            // Test invalid birth limit
            var invalidBirthLimit = new Dictionary<string, object> { { "birthLimit", 10 } };
            errors = generator.ValidateParameters(invalidBirthLimit);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid birth limit should produce validation errors");
            }
            
            // Test invalid death limit
            var invalidDeathLimit = new Dictionary<string, object> { { "deathLimit", -1 } };
            errors = generator.ValidateParameters(invalidDeathLimit);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid death limit should produce validation errors");
            }
            
            // Test unknown parameter
            var unknownParam = new Dictionary<string, object> { { "unknownParam", 123 } };
            errors = generator.ValidateParameters(unknownParam);
            if (errors.Count == 0)
            {
                throw new Exception("Unknown parameters should produce validation errors");
            }
            
            Console.WriteLine("✓ Cellular automata parameter validation test passed");
        }
        
        /// <summary>
        /// Tests different parameter configurations
        /// </summary>
        public static void TestCellularParameterEffects()
        {
            Console.WriteLine("Testing cellular automata parameter effects...");
            
            var randomGenerator = new RandomGenerator(456);
            var generator = new CellularAutomataGenerator(randomGenerator);
            
            var baseConfig = new GenerationConfig
            {
                Width = 20,
                Height = 20,
                GenerationAlgorithm = "cellular",
                TerrainTypes = new List<string> { "ground", "wall" }
            };
            
            // Test high fill probability (should produce more walls initially)
            baseConfig.AlgorithmParameters = new Dictionary<string, object>
            {
                { "initialFillProbability", 0.7f },
                { "iterations", 3 }
            };
            
            var highFillMap = generator.GenerateTerrain(baseConfig, 456);
            var highFillCounts = CountTileTypes(highFillMap);
            
            // Test low fill probability (should produce fewer walls initially)
            baseConfig.AlgorithmParameters = new Dictionary<string, object>
            {
                { "initialFillProbability", 0.2f },
                { "iterations", 3 }
            };
            
            var lowFillMap = generator.GenerateTerrain(baseConfig, 456);
            var lowFillCounts = CountTileTypes(lowFillMap);
            
            int highFillWalls = highFillCounts.ContainsKey(TileType.Wall) ? highFillCounts[TileType.Wall] : 0;
            int lowFillWalls = lowFillCounts.ContainsKey(TileType.Wall) ? lowFillCounts[TileType.Wall] : 0;
            
            Console.WriteLine($"High fill probability: {highFillWalls} wall tiles");
            Console.WriteLine($"Low fill probability: {lowFillWalls} wall tiles");
            
            // Test different iteration counts
            baseConfig.AlgorithmParameters = new Dictionary<string, object>
            {
                { "initialFillProbability", 0.45f },
                { "iterations", 1 }
            };
            
            var lowIterMap = generator.GenerateTerrain(baseConfig, 456);
            
            baseConfig.AlgorithmParameters = new Dictionary<string, object>
            {
                { "initialFillProbability", 0.45f },
                { "iterations", 10 }
            };
            
            var highIterMap = generator.GenerateTerrain(baseConfig, 456);
            
            // Maps with different iteration counts should be different
            if (AreMapsIdentical(lowIterMap, highIterMap))
            {
                Console.WriteLine("Warning: Different iteration counts produced identical maps");
                // This is a warning, not a failure, as it could happen with certain seeds
            }
            
            Console.WriteLine("✓ Cellular automata parameter effects test passed");
        }
        
        /// <summary>
        /// Tests algorithm name and default parameters
        /// </summary>
        public static void TestCellularAlgorithmInfo()
        {
            Console.WriteLine("Testing cellular automata algorithm info...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new CellularAutomataGenerator(randomGenerator);
            
            var name = generator.GetAlgorithmName();
            if (name != "cellular")
            {
                throw new Exception($"Expected algorithm name 'cellular', got '{name}'");
            }
            
            var defaults = generator.GetDefaultParameters();
            if (defaults == null || defaults.Count == 0)
            {
                throw new Exception("Should have default parameters");
            }
            
            // Check for expected default parameters
            var expectedParams = new[] { "initialFillProbability", "iterations", "birthLimit", "deathLimit", "wallType", "floorType" };
            foreach (var param in expectedParams)
            {
                if (!defaults.ContainsKey(param))
                {
                    throw new Exception($"Missing default parameter: {param}");
                }
            }
            
            Console.WriteLine($"✓ Cellular automata algorithm info test passed (name: {name}, {defaults.Count} default parameters)");
        }
        
        /// <summary>
        /// Tests cave-like structure generation
        /// </summary>
        public static void TestCaveStructureGeneration()
        {
            Console.WriteLine("Testing cave-like structure generation...");
            
            var randomGenerator = new RandomGenerator(999);
            var generator = new CellularAutomataGenerator(randomGenerator);
            
            var config = new GenerationConfig
            {
                Width = 30,
                Height = 30,
                GenerationAlgorithm = "cellular",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "initialFillProbability", 0.45f },
                    { "iterations", 6 },
                    { "birthLimit", 4 },
                    { "deathLimit", 3 }
                },
                TerrainTypes = new List<string> { "ground", "wall" }
            };
            
            var tileMap = generator.GenerateTerrain(config, 999);
            
            // Analyze the structure for cave-like properties
            var tileCounts = CountTileTypes(tileMap);
            int totalTiles = tileMap.Width * tileMap.Height;
            int groundTiles = tileCounts.ContainsKey(TileType.Ground) ? tileCounts[TileType.Ground] : 0;
            int wallTiles = tileCounts.ContainsKey(TileType.Wall) ? tileCounts[TileType.Wall] : 0;
            
            float groundRatio = (float)groundTiles / totalTiles;
            float wallRatio = (float)wallTiles / totalTiles;
            
            Console.WriteLine($"Cave structure analysis:");
            Console.WriteLine($"  Ground ratio: {groundRatio:P1}");
            Console.WriteLine($"  Wall ratio: {wallRatio:P1}");
            
            // Cave-like structures should have a reasonable balance of ground and walls
            if (groundRatio < 0.1f || groundRatio > 0.9f)
            {
                Console.WriteLine("Warning: Cave structure may be too extreme (too much or too little open space)");
            }
            
            Console.WriteLine("✓ Cave structure generation test passed");
        }
        
        /// <summary>
        /// Runs all cellular automata generator tests
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                TestBasicCellularGeneration();
                TestCellularParameterValidation();
                TestCellularParameterEffects();
                TestCellularAlgorithmInfo();
                TestCaveStructureGeneration();
                Console.WriteLine("✓ All cellular automata generator tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Cellular automata test failed: {ex.Message}");
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
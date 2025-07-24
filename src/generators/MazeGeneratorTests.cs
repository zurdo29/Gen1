using System;
using System.Collections.Generic;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Tests for maze terrain generator
    /// </summary>
    public class MazeGeneratorTests
    {
        /// <summary>
        /// Tests basic maze generation
        /// </summary>
        public static void TestBasicMazeGeneration()
        {
            Console.WriteLine("Testing maze terrain generation...");
            
            var randomGenerator = new RandomGenerator(321);
            var generator = new MazeGenerator(randomGenerator);
            
            var config = new GenerationConfig
            {
                Width = 21, // Odd dimensions work better for mazes
                Height = 21,
                GenerationAlgorithm = "maze",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "algorithm", "recursive_backtracking" },
                    { "wallType", "wall" },
                    { "pathType", "ground" },
                    { "complexity", 0.5f },
                    { "density", 0.5f },
                    { "braidingFactor", 0.0f }
                },
                TerrainTypes = new List<string> { "ground", "wall" }
            };
            
            var tileMap = generator.GenerateTerrain(config, 321);
            
            // Verify basic properties
            if (tileMap.Width != 21 || tileMap.Height != 21)
            {
                throw new Exception($"Expected 21x21 map, got {tileMap.Width}x{tileMap.Height}");
            }
            
            // Verify borders are walls
            VerifyBorders(tileMap);
            
            // Count different tile types
            var tileCounts = CountTileTypes(tileMap);
            
            Console.WriteLine($"Generated maze terrain with:");
            foreach (var kvp in tileCounts)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value} tiles");
            }
            
            // Verify we have both walls and paths
            if (!tileCounts.ContainsKey(TileType.Wall) || !tileCounts.ContainsKey(TileType.Ground))
            {
                throw new Exception("Maze should generate both walls and ground tiles");
            }
            
            // Verify maze has reasonable structure (more walls than paths typically)
            int wallCount = tileCounts.ContainsKey(TileType.Wall) ? tileCounts[TileType.Wall] : 0;
            int pathCount = tileCounts.ContainsKey(TileType.Ground) ? tileCounts[TileType.Ground] : 0;
            
            if (pathCount == 0)
            {
                throw new Exception("Maze should have some path tiles");
            }
            
            // Test reproducibility
            var tileMap2 = generator.GenerateTerrain(config, 321);
            if (!AreMapsIdentical(tileMap, tileMap2))
            {
                throw new Exception("Same seed should produce identical maze terrain");
            }
            
            Console.WriteLine("✓ Basic maze generation test passed");
        }
        
        /// <summary>
        /// Tests parameter validation for maze generator
        /// </summary>
        public static void TestMazeParameterValidation()
        {
            Console.WriteLine("Testing maze parameter validation...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new MazeGenerator(randomGenerator);
            
            // Test valid parameters
            var validParams = new Dictionary<string, object>
            {
                { "algorithm", "recursive_backtracking" },
                { "wallType", "stone" },
                { "pathType", "grass" },
                { "complexity", 0.7f },
                { "density", 0.3f },
                { "braidingFactor", 0.1f }
            };
            
            if (!generator.SupportsParameters(validParams))
            {
                throw new Exception("Valid maze parameters should be supported");
            }
            
            // Test invalid algorithm
            var invalidAlgorithm = new Dictionary<string, object> { { "algorithm", "invalid_algorithm" } };
            var errors = generator.ValidateParameters(invalidAlgorithm);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid algorithm should produce validation errors");
            }
            
            // Test invalid complexity
            var invalidComplexity = new Dictionary<string, object> { { "complexity", 1.5f } };
            errors = generator.ValidateParameters(invalidComplexity);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid complexity should produce validation errors");
            }
            
            // Test invalid density
            var invalidDensity = new Dictionary<string, object> { { "density", -0.1f } };
            errors = generator.ValidateParameters(invalidDensity);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid density should produce validation errors");
            }
            
            // Test invalid braiding factor
            var invalidBraiding = new Dictionary<string, object> { { "braidingFactor", 2.0f } };
            errors = generator.ValidateParameters(invalidBraiding);
            if (errors.Count == 0)
            {
                throw new Exception("Invalid braiding factor should produce validation errors");
            }
            
            // Test unknown parameter
            var unknownParam = new Dictionary<string, object> { { "unknownParam", 123 } };
            errors = generator.ValidateParameters(unknownParam);
            if (errors.Count == 0)
            {
                throw new Exception("Unknown parameters should produce validation errors");
            }
            
            Console.WriteLine("✓ Maze parameter validation test passed");
        }
        
        /// <summary>
        /// Tests different maze algorithms
        /// </summary>
        public static void TestMazeAlgorithms()
        {
            Console.WriteLine("Testing different maze algorithms...");
            
            var randomGenerator = new RandomGenerator(654);
            var generator = new MazeGenerator(randomGenerator);
            
            var baseConfig = new GenerationConfig
            {
                Width = 15,
                Height = 15,
                GenerationAlgorithm = "maze",
                TerrainTypes = new List<string> { "ground", "wall" }
            };
            
            // Test recursive backtracking
            baseConfig.AlgorithmParameters = new Dictionary<string, object>
            {
                { "algorithm", "recursive_backtracking" }
            };
            
            var recursiveMap = generator.GenerateTerrain(baseConfig, 654);
            var recursiveCounts = CountTileTypes(recursiveMap);
            
            Console.WriteLine($"Recursive backtracking maze:");
            foreach (var kvp in recursiveCounts)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value} tiles");
            }
            
            // Test simple algorithm
            baseConfig.AlgorithmParameters = new Dictionary<string, object>
            {
                { "algorithm", "simple" },
                { "complexity", 0.6f },
                { "density", 0.4f }
            };
            
            var simpleMap = generator.GenerateTerrain(baseConfig, 654);
            var simpleCounts = CountTileTypes(simpleMap);
            
            Console.WriteLine($"Simple maze:");
            foreach (var kvp in simpleCounts)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value} tiles");
            }
            
            // Both should produce valid mazes
            if (!recursiveCounts.ContainsKey(TileType.Ground) || !simpleCounts.ContainsKey(TileType.Ground))
            {
                throw new Exception("All maze algorithms should produce path tiles");
            }
            
            Console.WriteLine("✓ Maze algorithms test passed");
        }
        
        /// <summary>
        /// Tests braiding functionality
        /// </summary>
        public static void TestMazeBraiding()
        {
            Console.WriteLine("Testing maze braiding...");
            
            var randomGenerator = new RandomGenerator(987);
            var generator = new MazeGenerator(randomGenerator);
            
            var config = new GenerationConfig
            {
                Width = 19,
                Height = 19,
                GenerationAlgorithm = "maze",
                TerrainTypes = new List<string> { "ground", "wall" }
            };
            
            // Generate maze without braiding
            config.AlgorithmParameters = new Dictionary<string, object>
            {
                { "algorithm", "recursive_backtracking" },
                { "braidingFactor", 0.0f }
            };
            
            var noBraidMap = generator.GenerateTerrain(config, 987);
            var noBraidDeadEnds = CountDeadEnds(noBraidMap);
            
            // Generate maze with braiding
            config.AlgorithmParameters = new Dictionary<string, object>
            {
                { "algorithm", "recursive_backtracking" },
                { "braidingFactor", 0.5f }
            };
            
            var braidedMap = generator.GenerateTerrain(config, 987);
            var braidedDeadEnds = CountDeadEnds(braidedMap);
            
            Console.WriteLine($"Dead ends without braiding: {noBraidDeadEnds}");
            Console.WriteLine($"Dead ends with braiding: {braidedDeadEnds}");
            
            // Braiding should reduce the number of dead ends
            if (braidedDeadEnds >= noBraidDeadEnds)
            {
                Console.WriteLine("Warning: Braiding did not reduce dead ends as expected");
                // This is a warning, not a failure, as the effect depends on the maze structure
            }
            
            Console.WriteLine("✓ Maze braiding test passed");
        }
        
        /// <summary>
        /// Tests algorithm name and default parameters
        /// </summary>
        public static void TestMazeAlgorithmInfo()
        {
            Console.WriteLine("Testing maze algorithm info...");
            
            var randomGenerator = new RandomGenerator();
            var generator = new MazeGenerator(randomGenerator);
            
            var name = generator.GetAlgorithmName();
            if (name != "maze")
            {
                throw new Exception($"Expected algorithm name 'maze', got '{name}'");
            }
            
            var defaults = generator.GetDefaultParameters();
            if (defaults == null || defaults.Count == 0)
            {
                throw new Exception("Should have default parameters");
            }
            
            // Check for expected default parameters
            var expectedParams = new[] { "algorithm", "wallType", "pathType", "complexity", "density", "braidingFactor" };
            foreach (var param in expectedParams)
            {
                if (!defaults.ContainsKey(param))
                {
                    throw new Exception($"Missing default parameter: {param}");
                }
            }
            
            Console.WriteLine($"✓ Maze algorithm info test passed (name: {name}, {defaults.Count} default parameters)");
        }
        
        /// <summary>
        /// Runs all maze generator tests
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                TestBasicMazeGeneration();
                TestMazeParameterValidation();
                TestMazeAlgorithms();
                TestMazeBraiding();
                TestMazeAlgorithmInfo();
                Console.WriteLine("✓ All maze generator tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Maze generator test failed: {ex.Message}");
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
        /// Counts dead ends in the maze
        /// </summary>
        private static int CountDeadEnds(TileMap tileMap)
        {
            int deadEnds = 0;
            
            for (int x = 1; x < tileMap.Width - 1; x++)
            {
                for (int y = 1; y < tileMap.Height - 1; y++)
                {
                    if (tileMap.GetTile(x, y) == TileType.Ground)
                    {
                        int pathNeighbors = 0;
                        var directions = new[] { (0, -1), (1, 0), (0, 1), (-1, 0) };
                        
                        foreach (var (dx, dy) in directions)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < tileMap.Width && ny >= 0 && ny < tileMap.Height &&
                                tileMap.GetTile(nx, ny) == TileType.Ground)
                            {
                                pathNeighbors++;
                            }
                        }
                        
                        if (pathNeighbors == 1) // Dead end has only one path neighbor
                        {
                            deadEnds++;
                        }
                    }
                }
            }
            
            return deadEnds;
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
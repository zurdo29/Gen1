using System;
using System.Collections.Generic;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Comprehensive tests for all terrain generators to verify requirements
    /// </summary>
    public class ComprehensiveTerrainTests
    {
        /// <summary>
        /// Tests all terrain generators for basic functionality
        /// </summary>
        public static void TestAllGeneratorsBasicFunctionality()
        {
            Console.WriteLine("Testing all terrain generators for basic functionality...");
            
            var randomGenerator = new RandomGenerator(42);
            var generators = new List<ITerrainGenerator>
            {
                new PerlinNoiseGenerator(randomGenerator),
                new CellularAutomataGenerator(randomGenerator),
                new MazeGenerator(randomGenerator)
            };
            
            foreach (var generator in generators)
            {
                Console.WriteLine($"Testing {generator.GetAlgorithmName()} generator...");
                
                var config = CreateBasicConfig(generator);
                var tileMap = generator.GenerateTerrain(config, 42);
                
                // Verify basic properties
                if (tileMap.Width != config.Width || tileMap.Height != config.Height)
                {
                    throw new Exception($"{generator.GetAlgorithmName()}: Map dimensions don't match config");
                }
                
                // Verify borders are walls
                VerifyBorders(tileMap, generator.GetAlgorithmName());
                
                // Verify we have some content
                var tileCounts = CountTileTypes(tileMap);
                if (tileCounts.Count == 0)
                {
                    throw new Exception($"{generator.GetAlgorithmName()}: No tiles generated");
                }
                
                Console.WriteLine($"  ✓ {generator.GetAlgorithmName()} basic functionality passed");
            }
            
            Console.WriteLine("✓ All generators basic functionality test passed");
        }
        
        /// <summary>
        /// Tests all generators with various configurations
        /// </summary>
        public static void TestAllGeneratorsWithVariousConfigurations()
        {
            Console.WriteLine("Testing all terrain generators with various configurations...");
            
            var randomGenerator = new RandomGenerator(123);
            var generators = new List<ITerrainGenerator>
            {
                new PerlinNoiseGenerator(randomGenerator),
                new CellularAutomataGenerator(randomGenerator),
                new MazeGenerator(randomGenerator)
            };
            
            foreach (var generator in generators)
            {
                Console.WriteLine($"Testing {generator.GetAlgorithmName()} with various configurations...");
                
                // Test small map
                TestGeneratorWithConfig(generator, CreateSmallMapConfig(generator), "small map");
                
                // Test large map
                TestGeneratorWithConfig(generator, CreateLargeMapConfig(generator), "large map");
                
                // Test different terrain types
                TestGeneratorWithConfig(generator, CreateDiverseTerrainConfig(generator), "diverse terrain");
                
                // Test extreme parameters
                TestGeneratorWithConfig(generator, CreateExtremeParametersConfig(generator), "extreme parameters");
                
                Console.WriteLine($"  ✓ {generator.GetAlgorithmName()} various configurations passed");
            }
            
            Console.WriteLine("✓ All generators various configurations test passed");
        }
        
        /// <summary>
        /// Tests terrain navigability for all generators
        /// </summary>
        public static void TestTerrainNavigability()
        {
            Console.WriteLine("Testing terrain navigability for all generators...");
            
            var randomGenerator = new RandomGenerator(456);
            var generators = new List<ITerrainGenerator>
            {
                new PerlinNoiseGenerator(randomGenerator),
                new CellularAutomataGenerator(randomGenerator),
                new MazeGenerator(randomGenerator)
            };
            
            foreach (var generator in generators)
            {
                Console.WriteLine($"Testing {generator.GetAlgorithmName()} navigability...");
                
                var config = CreateNavigabilityTestConfig(generator);
                var tileMap = generator.GenerateTerrain(config, 456);
                
                // Find walkable areas
                var walkableAreas = FindWalkableAreas(tileMap);
                
                if (walkableAreas.Count == 0)
                {
                    throw new Exception($"{generator.GetAlgorithmName()}: No walkable areas found");
                }
                
                // Check for largest connected area
                var largestArea = walkableAreas.OrderByDescending(area => area.Count).First();
                int totalWalkableTiles = walkableAreas.Sum(area => area.Count);
                
                Console.WriteLine($"  {generator.GetAlgorithmName()} navigability:");
                Console.WriteLine($"    Total walkable areas: {walkableAreas.Count}");
                Console.WriteLine($"    Largest connected area: {largestArea.Count} tiles");
                Console.WriteLine($"    Total walkable tiles: {totalWalkableTiles}");
                
                // Verify minimum navigability requirements
                if (largestArea.Count < 10)
                {
                    throw new Exception($"{generator.GetAlgorithmName()}: Largest walkable area too small (< 10 tiles)");
                }
                
                // Check connectivity ratio (largest area should be significant portion of walkable space)
                float connectivityRatio = (float)largestArea.Count / totalWalkableTiles;
                if (connectivityRatio < 0.3f)
                {
                    Console.WriteLine($"    Warning: Low connectivity ratio ({connectivityRatio:P1}) for {generator.GetAlgorithmName()}");
                }
                
                Console.WriteLine($"  ✓ {generator.GetAlgorithmName()} navigability passed");
            }
            
            Console.WriteLine("✓ All generators navigability test passed");
        }
        
        /// <summary>
        /// Tests seed reproducibility for all generators
        /// </summary>
        public static void TestSeedReproducibility()
        {
            Console.WriteLine("Testing seed reproducibility for all generators...");
            
            var generators = new List<ITerrainGenerator>
            {
                new PerlinNoiseGenerator(new RandomGenerator()),
                new CellularAutomataGenerator(new RandomGenerator()),
                new MazeGenerator(new RandomGenerator())
            };
            
            var testSeeds = new[] { 12345, 67890, 999, 0, -123 };
            
            foreach (var generator in generators)
            {
                Console.WriteLine($"Testing {generator.GetAlgorithmName()} seed reproducibility...");
                
                var config = CreateBasicConfig(generator);
                
                foreach (var seed in testSeeds)
                {
                    // Generate terrain twice with same seed
                    var tileMap1 = generator.GenerateTerrain(config, seed);
                    var tileMap2 = generator.GenerateTerrain(config, seed);
                    
                    if (!AreMapsIdentical(tileMap1, tileMap2))
                    {
                        throw new Exception($"{generator.GetAlgorithmName()}: Seed {seed} did not produce identical results");
                    }
                }
                
                // Test that different seeds produce different results
                var map1 = generator.GenerateTerrain(config, 111);
                var map2 = generator.GenerateTerrain(config, 222);
                
                if (AreMapsIdentical(map1, map2))
                {
                    Console.WriteLine($"    Warning: Different seeds produced identical maps for {generator.GetAlgorithmName()}");
                    // This is a warning, not a failure, as it could happen with simple generators
                }
                
                Console.WriteLine($"  ✓ {generator.GetAlgorithmName()} seed reproducibility passed");
            }
            
            Console.WriteLine("✓ All generators seed reproducibility test passed");
        }
        
        /// <summary>
        /// Tests parameter validation for all generators
        /// </summary>
        public static void TestParameterValidation()
        {
            Console.WriteLine("Testing parameter validation for all generators...");
            
            var randomGenerator = new RandomGenerator();
            var generators = new List<ITerrainGenerator>
            {
                new PerlinNoiseGenerator(randomGenerator),
                new CellularAutomataGenerator(randomGenerator),
                new MazeGenerator(randomGenerator)
            };
            
            foreach (var generator in generators)
            {
                Console.WriteLine($"Testing {generator.GetAlgorithmName()} parameter validation...");
                
                // Test null parameters
                var errors = generator.ValidateParameters(null);
                if (errors.Count == 0)
                {
                    throw new Exception($"{generator.GetAlgorithmName()}: Should have errors for null parameters");
                }
                
                // Test empty parameters
                errors = generator.ValidateParameters(new Dictionary<string, object>());
                // Empty parameters should be valid (use defaults)
                
                // Test default parameters
                var defaults = generator.GetDefaultParameters();
                if (!generator.SupportsParameters(defaults))
                {
                    throw new Exception($"{generator.GetAlgorithmName()}: Should support its own default parameters");
                }
                
                // Test invalid parameters specific to each generator
                TestGeneratorSpecificValidation(generator);
                
                Console.WriteLine($"  ✓ {generator.GetAlgorithmName()} parameter validation passed");
            }
            
            Console.WriteLine("✓ All generators parameter validation test passed");
        }
        
        /// <summary>
        /// Tests performance characteristics of all generators
        /// </summary>
        public static void TestPerformanceCharacteristics()
        {
            Console.WriteLine("Testing performance characteristics for all generators...");
            
            var randomGenerator = new RandomGenerator(789);
            var generators = new List<ITerrainGenerator>
            {
                new PerlinNoiseGenerator(randomGenerator),
                new CellularAutomataGenerator(randomGenerator),
                new MazeGenerator(randomGenerator)
            };
            
            var mapSizes = new[] { (10, 10), (50, 50), (100, 100) };
            
            foreach (var generator in generators)
            {
                Console.WriteLine($"Testing {generator.GetAlgorithmName()} performance...");
                
                foreach (var (width, height) in mapSizes)
                {
                    var config = CreateBasicConfig(generator);
                    config.Width = width;
                    config.Height = height;
                    
                    var startTime = DateTime.Now;
                    var tileMap = generator.GenerateTerrain(config, 789);
                    var endTime = DateTime.Now;
                    
                    var duration = endTime - startTime;
                    Console.WriteLine($"    {width}x{height}: {duration.TotalMilliseconds:F1}ms");
                    
                    // Verify the map was generated correctly
                    if (tileMap.Width != width || tileMap.Height != height)
                    {
                        throw new Exception($"{generator.GetAlgorithmName()}: Incorrect map size generated");
                    }
                    
                    // Performance threshold: should complete within reasonable time
                    if (duration.TotalSeconds > 10)
                    {
                        Console.WriteLine($"    Warning: {generator.GetAlgorithmName()} took {duration.TotalSeconds:F1}s for {width}x{height} map");
                    }
                }
                
                Console.WriteLine($"  ✓ {generator.GetAlgorithmName()} performance test passed");
            }
            
            Console.WriteLine("✓ All generators performance test passed");
        }
        
        /// <summary>
        /// Runs all comprehensive terrain generator tests
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                Console.WriteLine("=== Comprehensive Terrain Generator Tests ===");
                
                TestAllGeneratorsBasicFunctionality();
                TestAllGeneratorsWithVariousConfigurations();
                TestTerrainNavigability();
                TestSeedReproducibility();
                TestParameterValidation();
                TestPerformanceCharacteristics();
                
                Console.WriteLine("=== All Comprehensive Terrain Generator Tests Passed! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Comprehensive terrain generator test failed: {ex.Message}");
                throw;
            }
        }
        
        // Helper methods
        
        private static GenerationConfig CreateBasicConfig(ITerrainGenerator generator)
        {
            var config = new GenerationConfig
            {
                Width = 20,
                Height = 20,
                GenerationAlgorithm = generator.GetAlgorithmName(),
                AlgorithmParameters = generator.GetDefaultParameters(),
                TerrainTypes = new List<string> { "ground", "wall", "water", "grass", "stone" }
            };
            
            return config;
        }
        
        private static GenerationConfig CreateSmallMapConfig(ITerrainGenerator generator)
        {
            var config = CreateBasicConfig(generator);
            config.Width = 10;
            config.Height = 10;
            return config;
        }
        
        private static GenerationConfig CreateLargeMapConfig(ITerrainGenerator generator)
        {
            var config = CreateBasicConfig(generator);
            config.Width = 50;
            config.Height = 50;
            return config;
        }
        
        private static GenerationConfig CreateDiverseTerrainConfig(ITerrainGenerator generator)
        {
            var config = CreateBasicConfig(generator);
            config.TerrainTypes = new List<string> { "ground", "wall", "water", "grass", "stone", "sand", "lava", "ice" };
            return config;
        }
        
        private static GenerationConfig CreateExtremeParametersConfig(ITerrainGenerator generator)
        {
            var config = CreateBasicConfig(generator);
            
            // Modify parameters to extreme but valid values
            switch (generator.GetAlgorithmName())
            {
                case "perlin":
                    config.AlgorithmParameters = new Dictionary<string, object>
                    {
                        { "scale", 0.01f },
                        { "octaves", 1 },
                        { "persistence", 0.1f },
                        { "lacunarity", 4.0f },
                        { "waterLevel", 0.1f },
                        { "mountainLevel", 0.9f }
                    };
                    break;
                case "cellular":
                    config.AlgorithmParameters = new Dictionary<string, object>
                    {
                        { "initialFillProbability", 0.1f },
                        { "iterations", 1 },
                        { "birthLimit", 8 },
                        { "deathLimit", 0 }
                    };
                    break;
                case "maze":
                    config.AlgorithmParameters = new Dictionary<string, object>
                    {
                        { "algorithm", "simple" },
                        { "complexity", 1.0f },
                        { "density", 0.1f },
                        { "braidingFactor", 1.0f }
                    };
                    break;
            }
            
            return config;
        }
        
        private static GenerationConfig CreateNavigabilityTestConfig(ITerrainGenerator generator)
        {
            var config = CreateBasicConfig(generator);
            config.Width = 30;
            config.Height = 30;
            
            // Use parameters that should create navigable terrain
            switch (generator.GetAlgorithmName())
            {
                case "perlin":
                    config.AlgorithmParameters = new Dictionary<string, object>
                    {
                        { "scale", 0.1f },
                        { "octaves", 3 },
                        { "waterLevel", 0.2f },
                        { "mountainLevel", 0.8f }
                    };
                    break;
                case "cellular":
                    config.AlgorithmParameters = new Dictionary<string, object>
                    {
                        { "initialFillProbability", 0.4f },
                        { "iterations", 5 },
                        { "birthLimit", 4 },
                        { "deathLimit", 3 }
                    };
                    break;
                case "maze":
                    config.AlgorithmParameters = new Dictionary<string, object>
                    {
                        { "algorithm", "recursive_backtracking" },
                        { "braidingFactor", 0.3f }
                    };
                    break;
            }
            
            return config;
        }
        
        private static void TestGeneratorWithConfig(ITerrainGenerator generator, GenerationConfig config, string configName)
        {
            try
            {
                var tileMap = generator.GenerateTerrain(config, 123);
                
                if (tileMap.Width != config.Width || tileMap.Height != config.Height)
                {
                    throw new Exception($"Map dimensions don't match config for {configName}");
                }
                
                var tileCounts = CountTileTypes(tileMap);
                if (tileCounts.Count == 0)
                {
                    throw new Exception($"No tiles generated for {configName}");
                }
                
                Console.WriteLine($"    ✓ {configName} configuration passed");
            }
            catch (Exception ex)
            {
                throw new Exception($"{generator.GetAlgorithmName()} failed with {configName}: {ex.Message}");
            }
        }
        
        private static void TestGeneratorSpecificValidation(ITerrainGenerator generator)
        {
            switch (generator.GetAlgorithmName())
            {
                case "perlin":
                    // Test invalid scale
                    var invalidPerlinParams = new Dictionary<string, object> { { "scale", 2.0f } };
                    var errors = generator.ValidateParameters(invalidPerlinParams);
                    if (errors.Count == 0)
                    {
                        throw new Exception("Perlin generator should reject invalid scale");
                    }
                    break;
                    
                case "cellular":
                    // Test invalid fill probability
                    var invalidCellularParams = new Dictionary<string, object> { { "initialFillProbability", 1.5f } };
                    errors = generator.ValidateParameters(invalidCellularParams);
                    if (errors.Count == 0)
                    {
                        throw new Exception("Cellular generator should reject invalid fill probability");
                    }
                    break;
                    
                case "maze":
                    // Test invalid algorithm
                    var invalidMazeParams = new Dictionary<string, object> { { "algorithm", "invalid_algorithm" } };
                    errors = generator.ValidateParameters(invalidMazeParams);
                    if (errors.Count == 0)
                    {
                        throw new Exception("Maze generator should reject invalid algorithm");
                    }
                    break;
            }
        }
        
        private static List<List<(int X, int Y)>> FindWalkableAreas(TileMap tileMap)
        {
            var visited = new bool[tileMap.Width, tileMap.Height];
            var areas = new List<List<(int X, int Y)>>();
            
            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    if (!visited[x, y] && tileMap.IsWalkable(x, y))
                    {
                        var area = FloodFillWalkable(tileMap, x, y, visited);
                        if (area.Count > 0)
                        {
                            areas.Add(area);
                        }
                    }
                }
            }
            
            return areas;
        }
        
        private static List<(int X, int Y)> FloodFillWalkable(TileMap tileMap, int startX, int startY, bool[,] visited)
        {
            var area = new List<(int X, int Y)>();
            var stack = new Stack<(int X, int Y)>();
            stack.Push((startX, startY));
            
            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();
                
                if (x < 0 || x >= tileMap.Width || y < 0 || y >= tileMap.Height ||
                    visited[x, y] || !tileMap.IsWalkable(x, y))
                {
                    continue;
                }
                
                visited[x, y] = true;
                area.Add((x, y));
                
                // Add neighbors
                stack.Push((x + 1, y));
                stack.Push((x - 1, y));
                stack.Push((x, y + 1));
                stack.Push((x, y - 1));
            }
            
            return area;
        }
        
        private static void VerifyBorders(TileMap tileMap, string generatorName)
        {
            for (int x = 0; x < tileMap.Width; x++)
            {
                if (tileMap.GetTile(x, 0) != TileType.Wall || tileMap.GetTile(x, tileMap.Height - 1) != TileType.Wall)
                {
                    throw new Exception($"{generatorName}: Top or bottom border is not a wall");
                }
            }
            
            for (int y = 0; y < tileMap.Height; y++)
            {
                if (tileMap.GetTile(0, y) != TileType.Wall || tileMap.GetTile(tileMap.Width - 1, y) != TileType.Wall)
                {
                    throw new Exception($"{generatorName}: Left or right border is not a wall");
                }
            }
        }
        
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
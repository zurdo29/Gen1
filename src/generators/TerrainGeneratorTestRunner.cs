using System;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Test runner for all terrain generator tests
    /// </summary>
    public class TerrainGeneratorTestRunner
    {
        /// <summary>
        /// Runs all terrain generator tests
        /// </summary>
        public static void RunAllTerrainGeneratorTests()
        {
            Console.WriteLine("=== Starting All Terrain Generator Tests ===");
            Console.WriteLine();
            
            try
            {
                // Run individual generator tests
                Console.WriteLine("--- Individual Generator Tests ---");
                
                Console.WriteLine("Running Perlin Noise Generator Tests...");
                PerlinNoiseGeneratorTests.RunAllTests();
                Console.WriteLine();
                
                Console.WriteLine("Running Cellular Automata Generator Tests...");
                CellularAutomataGeneratorTests.RunAllTests();
                Console.WriteLine();
                
                Console.WriteLine("Running Maze Generator Tests...");
                MazeGeneratorTests.RunAllTests();
                Console.WriteLine();
                
                Console.WriteLine("Running Basic Terrain Generator Tests...");
                TerrainGeneratorTests.RunAllTests();
                Console.WriteLine();
                
                // Run comprehensive tests
                Console.WriteLine("--- Comprehensive Tests ---");
                ComprehensiveTerrainTests.RunAllTests();
                Console.WriteLine();
                
                Console.WriteLine("=== ALL TERRAIN GENERATOR TESTS PASSED! ===");
                Console.WriteLine();
                Console.WriteLine("Summary:");
                Console.WriteLine("✓ Perlin Noise Generator - All tests passed");
                Console.WriteLine("✓ Cellular Automata Generator - All tests passed");
                Console.WriteLine("✓ Maze Generator - All tests passed");
                Console.WriteLine("✓ Basic Terrain Generator - All tests passed");
                Console.WriteLine("✓ Comprehensive Tests - All tests passed");
                Console.WriteLine();
                Console.WriteLine("Test Coverage:");
                Console.WriteLine("✓ Various configurations tested");
                Console.WriteLine("✓ Terrain navigability verified");
                Console.WriteLine("✓ Seed reproducibility confirmed");
                Console.WriteLine("✓ Parameter validation tested");
                Console.WriteLine("✓ Performance characteristics measured");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ TERRAIN GENERATOR TESTS FAILED: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        
        /// <summary>
        /// Runs only the comprehensive tests (main requirement for task 3.5)
        /// </summary>
        public static void RunComprehensiveTests()
        {
            Console.WriteLine("=== Running Comprehensive Terrain Generator Tests ===");
            Console.WriteLine();
            
            try
            {
                ComprehensiveTerrainTests.RunAllTests();
                
                Console.WriteLine();
                Console.WriteLine("=== COMPREHENSIVE TERRAIN GENERATOR TESTS PASSED! ===");
                Console.WriteLine();
                Console.WriteLine("Requirements Verified:");
                Console.WriteLine("✓ Each generator tested with various configurations");
                Console.WriteLine("✓ Terrain navigability verified for all generators");
                Console.WriteLine("✓ Seed reproducibility tested for all generators");
                Console.WriteLine("✓ Parameter validation tested");
                Console.WriteLine("✓ Performance characteristics measured");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ COMPREHENSIVE TERRAIN GENERATOR TESTS FAILED: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        
        /// <summary>
        /// Runs tests for a specific generator
        /// </summary>
        /// <param name="generatorName">Name of the generator to test</param>
        public static void RunSpecificGeneratorTests(string generatorName)
        {
            Console.WriteLine($"=== Running {generatorName} Generator Tests ===");
            Console.WriteLine();
            
            try
            {
                switch (generatorName.ToLower())
                {
                    case "perlin":
                        PerlinNoiseGeneratorTests.RunAllTests();
                        break;
                    case "cellular":
                        CellularAutomataGeneratorTests.RunAllTests();
                        break;
                    case "maze":
                        MazeGeneratorTests.RunAllTests();
                        break;
                    case "basic":
                        TerrainGeneratorTests.RunAllTests();
                        break;
                    default:
                        throw new ArgumentException($"Unknown generator: {generatorName}");
                }
                
                Console.WriteLine($"=== {generatorName} GENERATOR TESTS PASSED! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ {generatorName} GENERATOR TESTS FAILED: {ex.Message}");
                throw;
            }
        }
    }
}
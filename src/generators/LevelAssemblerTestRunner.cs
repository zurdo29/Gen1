using System;
using ProceduralMiniGameGenerator.Tests;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Test runner for level assembly and validation tests
    /// </summary>
    public class LevelAssemblerTestRunner
    {
        /// <summary>
        /// Runs the tests programmatically and returns success status
        /// </summary>
        /// <returns>True if all tests pass, false otherwise</returns>
        public static bool RunTests()
        {
            Console.WriteLine("=== Starting Level Assembly and Validation Tests ===");
            Console.WriteLine();
            
            try
            {
                var testSuite = new LevelAssemblerTests();
                testSuite.RunAllTests();
                
                Console.WriteLine();
                Console.WriteLine("=== LEVEL ASSEMBLY AND VALIDATION TESTS PASSED! ===");
                Console.WriteLine();
                Console.WriteLine("Summary:");
                Console.WriteLine("✓ Basic level assembly - All tests passed");
                Console.WriteLine("✓ Level assembly with entities - All tests passed");
                Console.WriteLine("✓ Level assembly with different terrains - All tests passed");
                Console.WriteLine("✓ Visual theme application - All tests passed");
                Console.WriteLine("✓ Level validation (valid levels) - All tests passed");
                Console.WriteLine("✓ Level validation (issue identification) - All tests passed");
                Console.WriteLine("✓ Level quality evaluation - All tests passed");
                Console.WriteLine("✓ Level assembly error handling - All tests passed");
                Console.WriteLine("✓ Level metadata generation - All tests passed");
                Console.WriteLine("✓ Playability validation - All tests passed");
                Console.WriteLine();
                Console.WriteLine("Requirements Verified:");
                Console.WriteLine("✓ Assembly of different level configurations tested");
                Console.WriteLine("✓ Validation correctly identifies issues");
                Console.WriteLine("✓ Requirements 5.3 and 11.1 satisfied");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ LEVEL ASSEMBLY AND VALIDATION TESTS FAILED: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }
        
        /// <summary>
        /// Runs comprehensive level assembly and validation tests
        /// </summary>
        public static void RunComprehensiveTests()
        {
            Console.WriteLine("=== Running Comprehensive Level Assembly and Validation Tests ===");
            Console.WriteLine();
            
            try
            {
                var testSuite = new LevelAssemblerTests();
                testSuite.RunAllTests();
                
                Console.WriteLine();
                Console.WriteLine("=== COMPREHENSIVE LEVEL ASSEMBLY AND VALIDATION TESTS PASSED! ===");
                Console.WriteLine();
                Console.WriteLine("Requirements Verified:");
                Console.WriteLine("✓ Assembly of different level configurations tested");
                Console.WriteLine("✓ Validation correctly identifies various issues");
                Console.WriteLine("✓ Level quality evaluation working correctly");
                Console.WriteLine("✓ Visual theme application tested");
                Console.WriteLine("✓ Error handling and edge cases covered");
                Console.WriteLine("✓ Metadata generation and playability validation tested");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ COMPREHENSIVE LEVEL ASSEMBLY AND VALIDATION TESTS FAILED: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}
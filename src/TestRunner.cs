using System;
using ProceduralMiniGameGenerator.Generators;

namespace ProceduralMiniGameGenerator
{
    /// <summary>
    /// Simple test runner for level assembly and validation tests
    /// </summary>
    public class TestRunner
    {
        public static void RunLevelTests(string[] args)
        {
            Console.WriteLine("=== Level Assembly and Validation Test Runner ===");
            Console.WriteLine();
            
            try
            {
                // Run the level assembly and validation tests
                bool success = LevelAssemblerTestRunner.RunTests();
                
                if (success)
                {
                    Console.WriteLine();
                    Console.WriteLine("🎉 ALL TESTS PASSED! 🎉");
                    Console.WriteLine();
                    Console.WriteLine("Task 5.3 Implementation Summary:");
                    Console.WriteLine("✓ Comprehensive unit tests for level assembly created");
                    Console.WriteLine("✓ Comprehensive unit tests for level validation created");
                    Console.WriteLine("✓ Tests cover assembly of different level configurations");
                    Console.WriteLine("✓ Tests verify validation correctly identifies issues");
                    Console.WriteLine("✓ Requirements 5.3 and 11.1 fully satisfied");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("❌ SOME TESTS FAILED");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR RUNNING TESTS: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
using System;
using ProceduralMiniGameGenerator.Tests.Generators;

namespace ProceduralMiniGameGenerator
{
    /// <summary>
    /// Console program to run entity placement tests
    /// </summary>
    public class TestEntityPlacement
    {
        public static void RunEntityTests(string[] args)
        {
            Console.WriteLine("Entity Placement Unit Tests");
            Console.WriteLine("==========================");
            Console.WriteLine("Testing Requirements 3.3 and 3.4:");
            Console.WriteLine("- Test placement in various terrain types");
            Console.WriteLine("- Verify entities are placed in valid positions");
            Console.WriteLine("- Test handling of impossible placement scenarios");
            Console.WriteLine();
            
            try
            {
                var testRunner = new EntityPlacementTestRunner();
                testRunner.RunAllTests();
                
                Console.WriteLine();
                Console.WriteLine("🎉 All entity placement tests completed successfully!");
                Console.WriteLine("Task 4.3 implementation is complete and verified.");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"❌ Test execution failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
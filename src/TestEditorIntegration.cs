using System;
using ProceduralMiniGameGenerator.Editor;

namespace ProceduralMiniGameGenerator
{
    /// <summary>
    /// Simple test runner for editor integration
    /// </summary>
    class TestEditorIntegration
    {
        static void RunEditorTests(string[] args)
        {
            Console.WriteLine("Testing Editor Integration Components");
            Console.WriteLine("====================================\n");

            try
            {
                // Test editor integration
                EditorIntegrationTest.RunAllTests();
                
                Console.WriteLine("\n✓ All editor integration tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Tests failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
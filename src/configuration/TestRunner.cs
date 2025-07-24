using System;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Test runner for configuration parser
    /// </summary>
    public class TestRunner
    {
        public static void RunConfigurationTests()
        {
            Console.WriteLine("Configuration Parser Test Runner");
            Console.WriteLine("================================\n");

            try
            {
                // Run comprehensive tests
                ComprehensiveTest.RunComprehensiveTest();
                
                Console.WriteLine("\n" + new string('-', 40));
                Console.WriteLine("Additional validation tests:");
                Console.WriteLine(new string('-', 40));
                
                // Run additional validation
                ConfigurationParserValidation.ValidateImplementation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nTest execution failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
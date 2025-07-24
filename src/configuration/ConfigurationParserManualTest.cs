using System;
using System.IO;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Configuration.Tests;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Manual test runner for ConfigurationParser
    /// </summary>
    public class ConfigurationParserManualTest
    {
        public static void RunTests()
        {
            Console.WriteLine("Running ConfigurationParser Manual Tests");
            Console.WriteLine("========================================\n");

            try
            {
                var test = new ConfigurationParserTest();
                test.RunAllTests();
                
                Console.WriteLine("\n✅ All manual tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Manual test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
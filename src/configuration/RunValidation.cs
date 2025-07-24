using System;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Simple program to run configuration parser validation
    /// </summary>
    public class RunValidation
    {
        public static void RunValidationTests()
        {
            try
            {
                ConfigurationParserValidation.ValidateImplementation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation failed with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
using System;
using System.IO;
using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Simple test program for ConfigurationParser
    /// </summary>
    public class TestConfigurationParser
    {
        public static void RunTests()
        {
            Console.WriteLine("Testing ConfigurationParser Implementation");
            Console.WriteLine("=========================================\n");

            var parser = new ConfigurationParser();

            // Test 1: Parse example configuration file
            TestParseExampleConfig(parser);

            // Test 2: Test default configuration
            TestDefaultConfig(parser);

            // Test 3: Test validation
            TestValidation(parser);

            // Test 4: Test error handling
            TestErrorHandling(parser);

            // Test 5: Test parsing from string
            TestParseFromString(parser);

            Console.WriteLine("\n✅ All tests completed!");
        }

        private static void TestParseExampleConfig(ConfigurationParser parser)
        {
            Console.WriteLine("Test 1: Parsing example configuration file");
            try
            {
                var config = parser.ParseConfig("example-config.json");
                Console.WriteLine($"✓ Successfully parsed config: {config.Width}x{config.Height}, Algorithm: {config.GenerationAlgorithm}");
                Console.WriteLine($"  Entities: {config.Entities.Count}, Theme: {config.VisualTheme.ThemeName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to parse example config: {ex.Message}");
            }
            Console.WriteLine();
        }

        private static void TestDefaultConfig(ConfigurationParser parser)
        {
            Console.WriteLine("Test 2: Getting default configuration");
            try
            {
                var config = parser.GetDefaultConfig();
                Console.WriteLine($"✓ Default config created: {config.Width}x{config.Height}, Algorithm: {config.GenerationAlgorithm}");
                Console.WriteLine($"  Entities: {config.Entities.Count}, Theme: {config.VisualTheme.ThemeName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to get default config: {ex.Message}");
            }
            Console.WriteLine();
        }

        private static void TestValidation(ConfigurationParser parser)
        {
            Console.WriteLine("Test 3: Testing validation");
            try
            {
                var validConfig = parser.GetDefaultConfig();
                var isValid = parser.ValidateConfig(validConfig, out var errors);
                Console.WriteLine($"✓ Valid config validation: {isValid}, Errors: {errors.Count}");

                // Test invalid config
                var invalidConfig = new GenerationConfig
                {
                    Width = -10,
                    Height = 5000,
                    GenerationAlgorithm = "invalid"
                };
                var isInvalid = parser.ValidateConfig(invalidConfig, out var invalidErrors);
                Console.WriteLine($"✓ Invalid config validation: {!isInvalid}, Errors: {invalidErrors.Count}");
                if (invalidErrors.Count > 0)
                {
                    Console.WriteLine($"  First error: {invalidErrors[0]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Validation test failed: {ex.Message}");
            }
            Console.WriteLine();
        }

        private static void TestErrorHandling(ConfigurationParser parser)
        {
            Console.WriteLine("Test 4: Testing error handling");
            
            // Test missing file
            try
            {
                parser.ParseConfig("non-existent-file.json");
                Console.WriteLine("✗ Should have thrown exception for missing file");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("✓ Correctly handled missing file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected exception for missing file: {ex.Message}");
            }

            // Test null path
            try
            {
                parser.ParseConfig(null);
                Console.WriteLine("✗ Should have thrown exception for null path");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ Correctly handled null path");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected exception for null path: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static void TestParseFromString(ConfigurationParser parser)
        {
            Console.WriteLine("Test 5: Testing parse from string");
            try
            {
                var jsonString = @"{
                    ""width"": 100,
                    ""height"": 80,
                    ""generationAlgorithm"": ""cellular"",
                    ""entities"": [
                        {
                            ""type"": ""Enemy"",
                            ""count"": 5,
                            ""placementStrategy"": ""random""
                        }
                    ]
                }";

                var config = parser.ParseConfigFromString(jsonString);
                Console.WriteLine($"✓ Successfully parsed from string: {config.Width}x{config.Height}, Algorithm: {config.GenerationAlgorithm}");
                Console.WriteLine($"  Entities: {config.Entities.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to parse from string: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}
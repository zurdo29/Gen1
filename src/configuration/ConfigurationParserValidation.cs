using System;
using System.IO;
using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Validation test for ConfigurationParser implementation
    /// </summary>
    public class ConfigurationParserValidation
    {
        public static void ValidateImplementation()
        {
            Console.WriteLine("Validating ConfigurationParser Implementation");
            Console.WriteLine("============================================\n");

            var parser = new ConfigurationParser();
            var allTestsPassed = true;

            // Requirement 1.2: Read JSON files into configuration objects
            allTestsPassed &= TestParseJsonFile(parser);

            // Requirement 1.3: Error handling for invalid configurations
            allTestsPassed &= TestErrorHandling(parser);

            // Requirement 1.4: Default value fallbacks
            allTestsPassed &= TestDefaultValueFallbacks(parser);

            // Additional validation tests
            allTestsPassed &= TestValidationFunctionality(parser);
            allTestsPassed &= TestDefaultConfiguration(parser);

            Console.WriteLine("\n" + new string('=', 50));
            if (allTestsPassed)
            {
                Console.WriteLine("✅ ALL TESTS PASSED - Configuration Parser is fully implemented!");
            }
            else
            {
                Console.WriteLine("❌ SOME TESTS FAILED - Implementation needs fixes");
            }
            Console.WriteLine(new string('=', 50));
        }

        private static bool TestParseJsonFile(ConfigurationParser parser)
        {
            Console.WriteLine("Test 1: Parse JSON files into configuration objects (Requirement 1.2)");
            try
            {
                // Test with example config file
                if (File.Exists("example-config.json"))
                {
                    var config = parser.ParseConfig("example-config.json");
                    Console.WriteLine($"✓ Successfully parsed example-config.json");
                    Console.WriteLine($"  - Dimensions: {config.Width}x{config.Height}");
                    Console.WriteLine($"  - Algorithm: {config.GenerationAlgorithm}");
                    Console.WriteLine($"  - Entities: {config.Entities.Count}");
                    Console.WriteLine($"  - Theme: {config.VisualTheme.ThemeName}");
                }

                // Test parsing from string
                var testJson = @"{
                    ""width"": 100,
                    ""height"": 80,
                    ""generationAlgorithm"": ""cellular"",
                    ""seed"": 42,
                    ""entities"": [
                        {
                            ""type"": ""Enemy"",
                            ""count"": 3,
                            ""placementStrategy"": ""random""
                        }
                    ]
                }";

                var configFromString = parser.ParseConfigFromString(testJson);
                Console.WriteLine($"✓ Successfully parsed JSON from string");
                Console.WriteLine($"  - Dimensions: {configFromString.Width}x{configFromString.Height}");
                Console.WriteLine($"  - Algorithm: {configFromString.GenerationAlgorithm}");
                Console.WriteLine($"  - Seed: {configFromString.Seed}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to parse JSON: {ex.Message}");
                return false;
            }
        }

        private static bool TestErrorHandling(ConfigurationParser parser)
        {
            Console.WriteLine("\nTest 2: Error handling for invalid configurations (Requirement 1.3)");
            var allErrorTestsPassed = true;

            // Test 1: Missing file
            try
            {
                parser.ParseConfig("non-existent-file.json");
                Console.WriteLine("✗ Should have thrown exception for missing file");
                allErrorTestsPassed = false;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("✓ Correctly handles missing files");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected exception for missing file: {ex.GetType().Name}");
                allErrorTestsPassed = false;
            }

            // Test 2: Invalid JSON
            try
            {
                var invalidJson = @"{ ""width"": 100, ""height"": ";
                parser.ParseConfigFromString(invalidJson);
                Console.WriteLine("✗ Should have thrown exception for invalid JSON");
                allErrorTestsPassed = false;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid JSON"))
            {
                Console.WriteLine("✓ Correctly handles invalid JSON format");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected exception for invalid JSON: {ex.GetType().Name} - {ex.Message}");
                allErrorTestsPassed = false;
            }

            // Test 3: Null inputs
            try
            {
                parser.ParseConfig(null);
                Console.WriteLine("✗ Should have thrown exception for null path");
                allErrorTestsPassed = false;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ Correctly handles null file path");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected exception for null path: {ex.GetType().Name}");
                allErrorTestsPassed = false;
            }

            try
            {
                parser.ParseConfigFromString(null);
                Console.WriteLine("✗ Should have thrown exception for null JSON content");
                allErrorTestsPassed = false;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ Correctly handles null JSON content");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected exception for null JSON: {ex.GetType().Name}");
                allErrorTestsPassed = false;
            }

            // Test 4: Empty inputs
            try
            {
                parser.ParseConfig("");
                Console.WriteLine("✗ Should have thrown exception for empty path");
                allErrorTestsPassed = false;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ Correctly handles empty file path");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected exception for empty path: {ex.GetType().Name}");
                allErrorTestsPassed = false;
            }

            return allErrorTestsPassed;
        }

        private static bool TestDefaultValueFallbacks(ConfigurationParser parser)
        {
            Console.WriteLine("\nTest 3: Default value fallbacks (Requirement 1.4)");
            try
            {
                // Test with incomplete JSON that should trigger defaults
                var incompleteJson = @"{
                    ""width"": 5,
                    ""height"": 2000,
                    ""generationAlgorithm"": ""invalid_algorithm""
                }";

                var config = parser.ParseConfigFromString(incompleteJson);

                // Check that invalid values were corrected to defaults
                var hasCorrectDefaults = true;
                if (config.Width != 50)
                {
                    Console.WriteLine($"✗ Width should default to 50, got {config.Width}");
                    hasCorrectDefaults = false;
                }
                else
                {
                    Console.WriteLine("✓ Invalid width corrected to default (50)");
                }

                if (config.Height != 50)
                {
                    Console.WriteLine($"✗ Height should default to 50, got {config.Height}");
                    hasCorrectDefaults = false;
                }
                else
                {
                    Console.WriteLine("✓ Invalid height corrected to default (50)");
                }

                if (config.GenerationAlgorithm != "perlin")
                {
                    Console.WriteLine($"✗ Algorithm should default to 'perlin', got '{config.GenerationAlgorithm}'");
                    hasCorrectDefaults = false;
                }
                else
                {
                    Console.WriteLine("✓ Invalid algorithm corrected to default ('perlin')");
                }

                // Check that missing objects are initialized
                if (config.Entities == null)
                {
                    Console.WriteLine("✗ Entities should be initialized to empty list");
                    hasCorrectDefaults = false;
                }
                else
                {
                    Console.WriteLine("✓ Missing entities initialized to empty list");
                }

                if (config.VisualTheme == null)
                {
                    Console.WriteLine("✗ VisualTheme should be initialized");
                    hasCorrectDefaults = false;
                }
                else
                {
                    Console.WriteLine("✓ Missing visual theme initialized");
                }

                if (config.Gameplay == null)
                {
                    Console.WriteLine("✗ Gameplay should be initialized");
                    hasCorrectDefaults = false;
                }
                else
                {
                    Console.WriteLine("✓ Missing gameplay config initialized");
                }

                return hasCorrectDefaults;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to test default fallbacks: {ex.Message}");
                return false;
            }
        }

        private static bool TestValidationFunctionality(ConfigurationParser parser)
        {
            Console.WriteLine("\nTest 4: Configuration validation functionality");
            try
            {
                // Test valid configuration
                var validConfig = parser.GetDefaultConfig();
                var isValid = parser.ValidateConfig(validConfig, out var errors);
                
                if (!isValid || errors.Count > 0)
                {
                    Console.WriteLine($"✗ Default config should be valid, got {errors.Count} errors");
                    if (errors.Count > 0)
                    {
                        Console.WriteLine($"  First error: {errors[0]}");
                    }
                    return false;
                }
                Console.WriteLine("✓ Valid configuration passes validation");

                // Test invalid configuration
                var invalidConfig = new GenerationConfig
                {
                    Width = -10,
                    Height = 5000,
                    GenerationAlgorithm = "invalid_algorithm"
                };

                var isInvalid = parser.ValidateConfig(invalidConfig, out var invalidErrors);
                if (isInvalid || invalidErrors.Count == 0)
                {
                    Console.WriteLine("✗ Invalid config should fail validation");
                    return false;
                }
                Console.WriteLine($"✓ Invalid configuration correctly fails validation ({invalidErrors.Count} errors)");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to test validation: {ex.Message}");
                return false;
            }
        }

        private static bool TestDefaultConfiguration(ConfigurationParser parser)
        {
            Console.WriteLine("\nTest 5: Default configuration generation");
            try
            {
                var defaultConfig = parser.GetDefaultConfig();
                
                if (defaultConfig == null)
                {
                    Console.WriteLine("✗ Default config should not be null");
                    return false;
                }

                // Verify default values are reasonable
                var hasValidDefaults = true;
                
                if (defaultConfig.Width <= 0 || defaultConfig.Height <= 0)
                {
                    Console.WriteLine("✗ Default dimensions should be positive");
                    hasValidDefaults = false;
                }

                if (string.IsNullOrEmpty(defaultConfig.GenerationAlgorithm))
                {
                    Console.WriteLine("✗ Default algorithm should not be empty");
                    hasValidDefaults = false;
                }

                if (defaultConfig.Entities == null)
                {
                    Console.WriteLine("✗ Default entities should not be null");
                    hasValidDefaults = false;
                }

                if (defaultConfig.VisualTheme == null)
                {
                    Console.WriteLine("✗ Default visual theme should not be null");
                    hasValidDefaults = false;
                }

                if (defaultConfig.Gameplay == null)
                {
                    Console.WriteLine("✗ Default gameplay should not be null");
                    hasValidDefaults = false;
                }

                if (hasValidDefaults && defaultConfig != null)
                {
                    Console.WriteLine("✓ Default configuration has valid structure and values");
                    Console.WriteLine($"  - Dimensions: {defaultConfig.Width}x{defaultConfig.Height}");
                    Console.WriteLine($"  - Algorithm: {defaultConfig.GenerationAlgorithm}");
                    Console.WriteLine($"  - Entities: {defaultConfig.Entities?.Count ?? 0}");
                }

                return hasValidDefaults;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to test default configuration: {ex.Message}");
                return false;
            }
        }
    }
}
using System;
using System.IO;
using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Configuration
{
    /// <summary>
    /// Comprehensive test to verify all requirements are met
    /// </summary>
    public class ComprehensiveTest
    {
        public static void RunComprehensiveTest()
        {
            Console.WriteLine("Running Comprehensive Configuration Parser Test");
            Console.WriteLine("==============================================\n");

            var parser = new ConfigurationParser();
            var testResults = new List<(string Test, bool Passed, string Details)>();

            // Test all requirements from task 2.2
            testResults.Add(TestRequirement1_2(parser));
            testResults.Add(TestRequirement1_3(parser));
            testResults.Add(TestRequirement1_4(parser));

            // Additional comprehensive tests
            testResults.Add(TestComplexConfiguration(parser));
            testResults.Add(TestEdgeCases(parser));

            // Print results
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("TEST RESULTS SUMMARY");
            Console.WriteLine(new string('=', 60));

            int passed = 0;
            int total = testResults.Count;

            foreach (var (test, testPassed, details) in testResults)
            {
                var status = testPassed ? "✅ PASS" : "❌ FAIL";
                Console.WriteLine($"{status} - {test}");
                if (!string.IsNullOrEmpty(details))
                {
                    Console.WriteLine($"      {details}");
                }
                if (testPassed) passed++;
            }

            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"OVERALL RESULT: {passed}/{total} tests passed");
            
            if (passed == total)
            {
                Console.WriteLine("🎉 ALL REQUIREMENTS SUCCESSFULLY IMPLEMENTED!");
            }
            else
            {
                Console.WriteLine("⚠️  Some requirements need attention");
            }
            Console.WriteLine(new string('=', 60));
        }

        private static (string, bool, string) TestRequirement1_2(ConfigurationParser parser)
        {
            // Requirement 1.2: Read JSON files into configuration objects
            try
            {
                // Create a test JSON file
                var testJson = @"{
                    ""width"": 80,
                    ""height"": 60,
                    ""seed"": 12345,
                    ""generationAlgorithm"": ""perlin"",
                    ""algorithmParameters"": {
                        ""scale"": 0.1,
                        ""octaves"": 4
                    },
                    ""terrainTypes"": [""ground"", ""wall"", ""water""],
                    ""entities"": [
                        {
                            ""type"": ""Enemy"",
                            ""count"": 5,
                            ""minDistance"": 2.0,
                            ""placementStrategy"": ""random""
                        },
                        {
                            ""type"": ""Item"",
                            ""count"": 3,
                            ""minDistance"": 1.0,
                            ""placementStrategy"": ""spread""
                        }
                    ],
                    ""visualTheme"": {
                        ""themeName"": ""forest"",
                        ""colorPalette"": {
                            ""ground"": ""#8B4513"",
                            ""wall"": ""#654321""
                        }
                    },
                    ""gameplay"": {
                        ""playerSpeed"": 5.0,
                        ""playerHealth"": 100,
                        ""difficulty"": ""normal""
                    }
                }";

                var testFilePath = Path.Combine(Path.GetTempPath(), "test-config-1-2.json");
                File.WriteAllText(testFilePath, testJson);

                // Test parsing from file
                var configFromFile = parser.ParseConfig(testFilePath);
                
                // Test parsing from string
                var configFromString = parser.ParseConfigFromString(testJson);

                // Verify all parameters are read correctly
                var checks = new List<bool>
                {
                    configFromFile.Width == 80,
                    configFromFile.Height == 60,
                    configFromFile.Seed == 12345,
                    configFromFile.GenerationAlgorithm == "perlin",
                    configFromFile.TerrainTypes.Count == 3,
                    configFromFile.Entities.Count == 2,
                    configFromFile.Entities[0].Type == EntityType.Enemy,
                    configFromFile.Entities[0].Count == 5,
                    configFromFile.Entities[1].Type == EntityType.Item,
                    configFromFile.Entities[1].Count == 3,
                    configFromFile.VisualTheme.ThemeName == "forest",
                    configFromFile.Gameplay.PlayerSpeed == 5.0f,
                    configFromFile.Gameplay.PlayerHealth == 100,
                    configFromFile.Gameplay.Difficulty == "normal"
                };

                // Cleanup
                File.Delete(testFilePath);

                var allPassed = checks.TrueForAll(x => x);
                return ("Requirement 1.2: Parse JSON into configuration objects", 
                       allPassed, 
                       allPassed ? "All parameters correctly parsed" : "Some parameters not parsed correctly");
            }
            catch (Exception ex)
            {
                return ("Requirement 1.2: Parse JSON into configuration objects", 
                       false, 
                       $"Exception: {ex.Message}");
            }
        }

        private static (string, bool, string) TestRequirement1_3(ConfigurationParser parser)
        {
            // Requirement 1.3: Error handling for invalid configurations
            var errorTests = new List<(string Description, bool Passed)>();

            // Test 1: Invalid JSON format
            try
            {
                parser.ParseConfigFromString(@"{ ""width"": 100, ""height"": ");
                errorTests.Add(("Invalid JSON format", false));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid JSON"))
            {
                errorTests.Add(("Invalid JSON format", true));
            }
            catch
            {
                errorTests.Add(("Invalid JSON format", false));
            }

            // Test 2: Missing file
            try
            {
                parser.ParseConfig("non-existent-file-xyz.json");
                errorTests.Add(("Missing file", false));
            }
            catch (FileNotFoundException)
            {
                errorTests.Add(("Missing file", true));
            }
            catch
            {
                errorTests.Add(("Missing file", false));
            }

            // Test 3: Null inputs
            try
            {
                parser.ParseConfig(null);
                errorTests.Add(("Null file path", false));
            }
            catch (ArgumentException)
            {
                errorTests.Add(("Null file path", true));
            }
            catch
            {
                errorTests.Add(("Null file path", false));
            }

            try
            {
                parser.ParseConfigFromString(null);
                errorTests.Add(("Null JSON content", false));
            }
            catch (ArgumentException)
            {
                errorTests.Add(("Null JSON content", true));
            }
            catch
            {
                errorTests.Add(("Null JSON content", false));
            }

            // Test 4: Validation errors
            var invalidConfig = new GenerationConfig
            {
                Width = -10,
                Height = 5000,
                GenerationAlgorithm = "invalid_algorithm"
            };

            var validationPassed = !parser.ValidateConfig(invalidConfig, out var errors) && errors.Count > 0;
            errorTests.Add(("Configuration validation", validationPassed));

            var allErrorTestsPassed = errorTests.TrueForAll(t => t.Passed);
            var failedTests = errorTests.FindAll(t => !t.Passed);
            var details = allErrorTestsPassed ? "All error cases handled correctly" : 
                         $"Failed: {string.Join(", ", failedTests.ConvertAll(t => t.Description))}";

            return ("Requirement 1.3: Error handling for invalid configurations", 
                   allErrorTestsPassed, 
                   details);
        }

        private static (string, bool, string) TestRequirement1_4(ConfigurationParser parser)
        {
            // Requirement 1.4: Default value fallbacks
            try
            {
                // Test with incomplete/invalid JSON
                var incompleteJson = @"{
                    ""width"": 5,
                    ""height"": 2000,
                    ""generationAlgorithm"": ""invalid_algorithm""
                }";

                var config = parser.ParseConfigFromString(incompleteJson);

                var defaultTests = new List<(string Property, bool Correct)>
                {
                    ("Width corrected to default", config.Width == 50),
                    ("Height corrected to default", config.Height == 50),
                    ("Algorithm corrected to default", config.GenerationAlgorithm == "perlin"),
                    ("Entities initialized", config.Entities != null),
                    ("VisualTheme initialized", config.VisualTheme != null),
                    ("Gameplay initialized", config.Gameplay != null),
                    ("AlgorithmParameters initialized", config.AlgorithmParameters != null)
                };

                var allDefaultsCorrect = defaultTests.TrueForAll(t => t.Correct);
                var failedDefaults = defaultTests.FindAll(t => !t.Correct);
                var details = allDefaultsCorrect ? "All defaults applied correctly" : 
                             $"Failed: {string.Join(", ", failedDefaults.ConvertAll(t => t.Property))}";

                return ("Requirement 1.4: Default value fallbacks", 
                       allDefaultsCorrect, 
                       details);
            }
            catch (Exception ex)
            {
                return ("Requirement 1.4: Default value fallbacks", 
                       false, 
                       $"Exception: {ex.Message}");
            }
        }

        private static (string, bool, string) TestComplexConfiguration(ConfigurationParser parser)
        {
            try
            {
                // Test with the example configuration file if it exists
                if (File.Exists("example-config.json"))
                {
                    var config = parser.ParseConfig("example-config.json");
                    var isValid = parser.ValidateConfig(config, out var errors);
                    
                    return ("Complex configuration parsing", 
                           isValid && errors.Count == 0, 
                           isValid ? "Example config parsed and validated successfully" : 
                                   $"Validation failed with {errors.Count} errors");
                }
                else
                {
                    return ("Complex configuration parsing", 
                           true, 
                           "Skipped - example-config.json not found");
                }
            }
            catch (Exception ex)
            {
                return ("Complex configuration parsing", 
                       false, 
                       $"Exception: {ex.Message}");
            }
        }

        private static (string, bool, string) TestEdgeCases(ConfigurationParser parser)
        {
            try
            {
                var edgeTests = new List<(string Test, bool Passed)>();

                // Test empty JSON object
                try
                {
                    var emptyConfig = parser.ParseConfigFromString("{}");
                    var isValid = parser.ValidateConfig(emptyConfig, out var errors);
                    edgeTests.Add(("Empty JSON object", emptyConfig != null));
                }
                catch
                {
                    edgeTests.Add(("Empty JSON object", false));
                }

                // Test default configuration validity
                try
                {
                    var defaultConfig = parser.GetDefaultConfig();
                    var isValid = parser.ValidateConfig(defaultConfig, out var errors);
                    edgeTests.Add(("Default config validity", isValid && errors.Count == 0));
                }
                catch
                {
                    edgeTests.Add(("Default config validity", false));
                }

                // Test JSON with comments (should be handled by JsonSerializerOptions)
                try
                {
                    var jsonWithComments = @"{
                        // This is a comment
                        ""width"": 100,
                        ""height"": 100 // Another comment
                    }";
                    var config = parser.ParseConfigFromString(jsonWithComments);
                    edgeTests.Add(("JSON with comments", config.Width == 100));
                }
                catch
                {
                    edgeTests.Add(("JSON with comments", false));
                }

                var allEdgeTestsPassed = edgeTests.TrueForAll(t => t.Passed);
                var failedEdgeTests = edgeTests.FindAll(t => !t.Passed);
                var details = allEdgeTestsPassed ? "All edge cases handled correctly" : 
                             $"Failed: {string.Join(", ", failedEdgeTests.ConvertAll(t => t.Test))}";

                return ("Edge case handling", allEdgeTestsPassed, details);
            }
            catch (Exception ex)
            {
                return ("Edge case handling", false, $"Exception: {ex.Message}");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Comprehensive test suite for configuration model classes
    /// </summary>
    public static class ConfigurationTest
    {
        /// <summary>
        /// Runs all configuration tests and reports results
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Configuration Model Classes Test Suite ===\n");

            var testResults = new List<TestResult>();

            // Test individual configuration classes
            testResults.Add(TestGenerationConfigValidation());
            testResults.Add(TestEntityConfigValidation());
            testResults.Add(TestVisualThemeConfigValidation());
            testResults.Add(TestGameplayConfigValidation());
            
            // Test comprehensive validation
            testResults.Add(TestConfigurationValidator());
            testResults.Add(TestDefaultConfigurationCreation());
            testResults.Add(TestCrossReferenceValidation());

            // Report summary
            var passed = testResults.Count(r => r.Passed);
            var failed = testResults.Count(r => !r.Passed);
            
            Console.WriteLine($"\n=== Test Summary ===");
            Console.WriteLine($"Total Tests: {testResults.Count}");
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {failed}");
            
            if (failed > 0)
            {
                Console.WriteLine("\nFailed Tests:");
                foreach (var result in testResults.Where(r => !r.Passed))
                {
                    Console.WriteLine($"  - {result.TestName}: {result.ErrorMessage}");
                }
            }
            else
            {
                Console.WriteLine("\nAll tests passed successfully!");
            }
        }

        private static TestResult TestGenerationConfigValidation()
        {
            var testName = "GenerationConfig Validation";
            Console.WriteLine($"Testing {testName}...");

            try
            {
                // Test valid configuration
                var validConfig = new GenerationConfig
                {
                    Width = 50,
                    Height = 50,
                    GenerationAlgorithm = "perlin",
                    TerrainTypes = new List<string> { "ground", "wall" },
                    Entities = new List<EntityConfig>
                    {
                        new EntityConfig { Type = EntityType.Player, Count = 1, PlacementStrategy = "center" }
                    }
                };

                var errors = validConfig.Validate();
                if (errors.Count > 0)
                {
                    return new TestResult(testName, false, $"Valid config produced errors: {string.Join(", ", errors)}");
                }

                // Test invalid configuration
                var invalidConfig = new GenerationConfig
                {
                    Width = 5, // Too small
                    Height = 2000, // Too large
                    GenerationAlgorithm = "invalid_algorithm",
                    TerrainTypes = new List<string> { "invalid_terrain" }
                };

                errors = invalidConfig.Validate();
                if (errors.Count == 0)
                {
                    return new TestResult(testName, false, "Invalid config should have produced errors");
                }

                // Test default application
                var warnings = invalidConfig.ApplyDefaults();
                if (warnings.Count == 0)
                {
                    return new TestResult(testName, false, "ApplyDefaults should have produced warnings");
                }

                Console.WriteLine($"  ✓ Valid config: {errors.Count} errors");
                Console.WriteLine($"  ✓ Invalid config: {invalidConfig.Validate().Count} errors");
                Console.WriteLine($"  ✓ Default application: {warnings.Count} warnings");

                return new TestResult(testName, true);
            }
            catch (Exception ex)
            {
                return new TestResult(testName, false, ex.Message);
            }
        }

        private static TestResult TestEntityConfigValidation()
        {
            var testName = "EntityConfig Validation";
            Console.WriteLine($"Testing {testName}...");

            try
            {
                // Test valid entity configurations
                var validConfigs = new[]
                {
                    new EntityConfig { Type = EntityType.Player, Count = 1, PlacementStrategy = "center" },
                    new EntityConfig { Type = EntityType.Enemy, Count = 5, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = 3, PlacementStrategy = "spread" },
                    new EntityConfig { Type = EntityType.Exit, Count = 1, PlacementStrategy = "far_from_player" }
                };

                foreach (var config in validConfigs)
                {
                    var errors = config.Validate();
                    if (errors.Count > 0)
                    {
                        return new TestResult(testName, false, $"Valid {config.Type} config produced errors: {string.Join(", ", errors)}");
                    }
                }

                // Test invalid configurations
                var invalidConfig = new EntityConfig
                {
                    Count = -1,
                    MinDistance = 150.0f,
                    MaxDistanceFromPlayer = 1.0f,
                    PlacementStrategy = "invalid_strategy"
                };

                var invalidErrors = invalidConfig.Validate();
                if (invalidErrors.Count == 0)
                {
                    return new TestResult(testName, false, "Invalid entity config should have produced errors");
                }

                Console.WriteLine($"  ✓ Valid entity configs: All passed");
                Console.WriteLine($"  ✓ Invalid entity config: {invalidErrors.Count} errors");

                return new TestResult(testName, true);
            }
            catch (Exception ex)
            {
                return new TestResult(testName, false, ex.Message);
            }
        }

        private static TestResult TestVisualThemeConfigValidation()
        {
            var testName = "VisualThemeConfig Validation";
            Console.WriteLine($"Testing {testName}...");

            try
            {
                // Test valid theme
                var validTheme = new VisualThemeConfig
                {
                    ThemeName = "forest",
                    ColorPalette = new Dictionary<string, string>
                    {
                        { "ground", "#8B4513" },
                        { "water", "blue" },
                        { "wall", "#FF0000" }
                    },
                    TileSprites = new Dictionary<string, string>
                    {
                        { "ground", "sprites/ground.png" },
                        { "wall", "sprites/wall.png" }
                    }
                };

                var errors = validTheme.Validate();
                if (errors.Count > 0)
                {
                    return new TestResult(testName, false, $"Valid theme config produced errors: {string.Join(", ", errors)}");
                }

                // Test invalid theme
                var invalidTheme = new VisualThemeConfig
                {
                    ThemeName = "", // Empty name
                    ColorPalette = new Dictionary<string, string>
                    {
                        { "ground", "invalid_color" },
                        { "", "#FF0000" } // Empty key
                    }
                };

                var invalidErrors = invalidTheme.Validate();
                if (invalidErrors.Count == 0)
                {
                    return new TestResult(testName, false, "Invalid theme config should have produced errors");
                }

                Console.WriteLine($"  ✓ Valid theme config: {errors.Count} errors");
                Console.WriteLine($"  ✓ Invalid theme config: {invalidErrors.Count} errors");

                return new TestResult(testName, true);
            }
            catch (Exception ex)
            {
                return new TestResult(testName, false, ex.Message);
            }
        }

        private static TestResult TestGameplayConfigValidation()
        {
            var testName = "GameplayConfig Validation";
            Console.WriteLine($"Testing {testName}...");

            try
            {
                // Test valid gameplay config
                var validGameplay = new GameplayConfig
                {
                    PlayerSpeed = 5.0f,
                    PlayerHealth = 100,
                    Difficulty = "normal",
                    TimeLimit = 300.0f,
                    VictoryConditions = new List<string> { "reach_exit", "collect_all_items" }
                };

                var errors = validGameplay.Validate();
                if (errors.Count > 0)
                {
                    return new TestResult(testName, false, $"Valid gameplay config produced errors: {string.Join(", ", errors)}");
                }

                // Test invalid gameplay config
                var invalidGameplay = new GameplayConfig
                {
                    PlayerSpeed = 0.0f, // Too low
                    PlayerHealth = 0, // Too low
                    Difficulty = "impossible", // Invalid
                    TimeLimit = -10.0f, // Negative
                    VictoryConditions = new List<string> { "invalid_condition" }
                };

                var invalidErrors = invalidGameplay.Validate();
                if (invalidErrors.Count == 0)
                {
                    return new TestResult(testName, false, "Invalid gameplay config should have produced errors");
                }

                Console.WriteLine($"  ✓ Valid gameplay config: {errors.Count} errors");
                Console.WriteLine($"  ✓ Invalid gameplay config: {invalidErrors.Count} errors");

                return new TestResult(testName, true);
            }
            catch (Exception ex)
            {
                return new TestResult(testName, false, ex.Message);
            }
        }

        private static TestResult TestConfigurationValidator()
        {
            var testName = "ConfigurationValidator";
            Console.WriteLine($"Testing {testName}...");

            try
            {
                // Test null configuration
                var nullResult = ConfigurationValidator.ValidateConfiguration(null);
                if (nullResult.IsValid || nullResult.Errors.Count == 0)
                {
                    return new TestResult(testName, false, "Null configuration should produce errors");
                }

                // Test valid configuration
                var validConfig = ConfigurationValidator.CreateDefaultConfiguration();
                var validResult = ConfigurationValidator.ValidateConfiguration(validConfig);
                
                if (!validResult.IsValid)
                {
                    return new TestResult(testName, false, $"Default configuration should be valid. Errors: {string.Join(", ", validResult.Errors)}");
                }

                Console.WriteLine($"  ✓ Null config validation: {nullResult.Errors.Count} errors");
                Console.WriteLine($"  ✓ Valid config validation: {validResult.Errors.Count} errors, {validResult.Warnings.Count} warnings");

                return new TestResult(testName, true);
            }
            catch (Exception ex)
            {
                return new TestResult(testName, false, ex.Message);
            }
        }

        private static TestResult TestDefaultConfigurationCreation()
        {
            var testName = "Default Configuration Creation";
            Console.WriteLine($"Testing {testName}...");

            try
            {
                var defaultConfig = ConfigurationValidator.CreateDefaultConfiguration();
                
                // Verify all required fields are populated
                if (defaultConfig.Width <= 0 || defaultConfig.Height <= 0)
                {
                    return new TestResult(testName, false, "Default config has invalid dimensions");
                }

                if (string.IsNullOrEmpty(defaultConfig.GenerationAlgorithm))
                {
                    return new TestResult(testName, false, "Default config missing generation algorithm");
                }

                if (defaultConfig.Entities == null || defaultConfig.Entities.Count == 0)
                {
                    return new TestResult(testName, false, "Default config missing entities");
                }

                if (defaultConfig.VisualTheme == null)
                {
                    return new TestResult(testName, false, "Default config missing visual theme");
                }

                if (defaultConfig.Gameplay == null)
                {
                    return new TestResult(testName, false, "Default config missing gameplay config");
                }

                // Validate the default configuration
                var errors = defaultConfig.Validate();
                if (errors.Count > 0)
                {
                    return new TestResult(testName, false, $"Default config is invalid: {string.Join(", ", errors)}");
                }

                Console.WriteLine($"  ✓ Default config created with {defaultConfig.Entities.Count} entities");
                Console.WriteLine($"  ✓ Default config validation: {errors.Count} errors");

                return new TestResult(testName, true);
            }
            catch (Exception ex)
            {
                return new TestResult(testName, false, ex.Message);
            }
        }

        private static TestResult TestCrossReferenceValidation()
        {
            var testName = "Cross-Reference Validation";
            Console.WriteLine($"Testing {testName}...");

            try
            {
                // Create config with mismatched references
                var config = new GenerationConfig
                {
                    Width = 50,
                    Height = 50,
                    GenerationAlgorithm = "perlin",
                    TerrainTypes = new List<string> { "ground", "wall", "lava" }, // lava not in theme
                    Entities = new List<EntityConfig>
                    {
                        new EntityConfig { Type = EntityType.Enemy, Count = 3, PlacementStrategy = "random" },
                        new EntityConfig { Type = EntityType.NPC, Count = 1, PlacementStrategy = "center" } // NPC not in theme
                    },
                    VisualTheme = new VisualThemeConfig
                    {
                        ThemeName = "test",
                        TileSprites = new Dictionary<string, string>
                        {
                            { "ground", "ground.png" },
                            { "wall", "wall.png" }
                            // Missing lava sprite
                        },
                        EntitySprites = new Dictionary<string, string>
                        {
                            { "enemy", "enemy.png" }
                            // Missing NPC sprite
                        }
                    },
                    Gameplay = new GameplayConfig
                    {
                        VictoryConditions = new List<string> { "collect_all_items" } // No items configured
                    }
                };

                var result = ConfigurationValidator.ValidateConfiguration(config);
                
                // Should have warnings about missing sprites and error about victory condition
                if (result.Warnings.Count == 0)
                {
                    return new TestResult(testName, false, "Should have warnings about missing sprite references");
                }

                if (!result.Errors.Any(e => e.Contains("collect_all_items")))
                {
                    return new TestResult(testName, false, "Should have error about collect_all_items victory condition without items");
                }

                Console.WriteLine($"  ✓ Cross-reference validation: {result.Errors.Count} errors, {result.Warnings.Count} warnings");

                return new TestResult(testName, true);
            }
            catch (Exception ex)
            {
                return new TestResult(testName, false, ex.Message);
            }
        }

        private class TestResult
        {
            public string TestName { get; }
            public bool Passed { get; }
            public string ErrorMessage { get; }

            public TestResult(string testName, bool passed, string errorMessage = "")
            {
                TestName = testName;
                Passed = passed;
                ErrorMessage = errorMessage;
            }
        }
    }
}
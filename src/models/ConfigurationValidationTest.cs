using System;
using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Simple test class to verify configuration validation
    /// </summary>
    public static class ConfigurationValidationTest
    {
        /// <summary>
        /// Tests the validation logic for all configuration classes
        /// </summary>
        public static void RunValidationTests()
        {
            Console.WriteLine("Running Configuration Validation Tests...\n");

            TestGenerationConfig();
            TestEntityConfig();
            TestVisualThemeConfig();
            TestGameplayConfig();

            Console.WriteLine("All validation tests completed.");
        }

        private static void TestGenerationConfig()
        {
            Console.WriteLine("Testing GenerationConfig validation:");

            // Test valid configuration
            var validConfig = new GenerationConfig
            {
                Width = 50,
                Height = 50,
                GenerationAlgorithm = "perlin",
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 3 }
                }
            };

            var errors = validConfig.Validate();
            Console.WriteLine($"Valid config errors: {errors.Count}");

            // Test invalid configuration
            var invalidConfig = new GenerationConfig
            {
                Width = 5, // Too small
                Height = 2000, // Too large
                GenerationAlgorithm = "invalid_algorithm"
            };

            errors = invalidConfig.Validate();
            Console.WriteLine($"Invalid config errors: {errors.Count}");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }

            Console.WriteLine();
        }

        private static void TestEntityConfig()
        {
            Console.WriteLine("Testing EntityConfig validation:");

            // Test valid entity config
            var validEntity = new EntityConfig
            {
                Type = EntityType.Enemy,
                Count = 5,
                MinDistance = 2.0f,
                PlacementStrategy = "random"
            };

            var errors = validEntity.Validate();
            Console.WriteLine($"Valid entity config errors: {errors.Count}");

            // Test invalid entity config
            var invalidEntity = new EntityConfig
            {
                Count = -1, // Invalid count
                MinDistance = 150.0f, // Too large
                MaxDistanceFromPlayer = 1.0f, // Smaller than min distance
                PlacementStrategy = "invalid_strategy"
            };

            errors = invalidEntity.Validate();
            Console.WriteLine($"Invalid entity config errors: {errors.Count}");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }

            Console.WriteLine();
        }

        private static void TestVisualThemeConfig()
        {
            Console.WriteLine("Testing VisualThemeConfig validation:");

            // Test valid theme config
            var validTheme = new VisualThemeConfig
            {
                ThemeName = "forest",
                ColorPalette = new Dictionary<string, string>
                {
                    { "ground", "#8B4513" },
                    { "water", "blue" }
                }
            };

            var errors = validTheme.Validate();
            Console.WriteLine($"Valid theme config errors: {errors.Count}");

            // Test invalid theme config
            var invalidTheme = new VisualThemeConfig
            {
                ThemeName = "", // Empty name
                ColorPalette = new Dictionary<string, string>
                {
                    { "ground", "invalid_color" },
                    { "", "#FF0000" } // Empty key
                }
            };

            errors = invalidTheme.Validate();
            Console.WriteLine($"Invalid theme config errors: {errors.Count}");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }

            Console.WriteLine();
        }

        private static void TestGameplayConfig()
        {
            Console.WriteLine("Testing GameplayConfig validation:");

            // Test valid gameplay config
            var validGameplay = new GameplayConfig
            {
                PlayerSpeed = 5.0f,
                PlayerHealth = 100,
                Difficulty = "normal",
                VictoryConditions = new List<string> { "reach_exit" }
            };

            var errors = validGameplay.Validate();
            Console.WriteLine($"Valid gameplay config errors: {errors.Count}");

            // Test invalid gameplay config
            var invalidGameplay = new GameplayConfig
            {
                PlayerSpeed = 0.0f, // Too low
                PlayerHealth = 0, // Too low
                Difficulty = "impossible", // Invalid difficulty
                TimeLimit = -10.0f, // Negative time
                VictoryConditions = new List<string> { "invalid_condition" }
            };

            errors = invalidGameplay.Validate();
            Console.WriteLine($"Invalid gameplay config errors: {errors.Count}");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }

            Console.WriteLine();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Configuration.Tests
{
    /// <summary>
    /// Test class for ConfigurationParser
    /// </summary>
    public class ConfigurationParserTest
    {
        private readonly ConfigurationParser _parser;
        private readonly string _testConfigPath;
        private readonly string _invalidConfigPath;

        public ConfigurationParserTest()
        {
            _parser = new ConfigurationParser();
            _testConfigPath = Path.Combine(Path.GetTempPath(), "test-config.json");
            _invalidConfigPath = Path.Combine(Path.GetTempPath(), "invalid-config.json");
        }

        /// <summary>
        /// Tests parsing a valid configuration file
        /// </summary>
        public void TestParseValidConfig()
        {
            // Arrange
            var validJson = @"{
                ""width"": 80,
                ""height"": 60,
                ""seed"": 12345,
                ""generationAlgorithm"": ""perlin"",
                ""algorithmParameters"": {
                    ""scale"": 0.1,
                    ""octaves"": 4
                },
                ""entities"": [
                    {
                        ""type"": ""Enemy"",
                        ""count"": 3,
                        ""minDistance"": 2.0,
                        ""placementStrategy"": ""random""
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

            File.WriteAllText(_testConfigPath, validJson);

            // Act
            var config = _parser.ParseConfig(_testConfigPath);

            // Assert
            AssertEqual(80, config.Width, "Width should be 80");
            AssertEqual(60, config.Height, "Height should be 60");
            AssertEqual(12345, config.Seed, "Seed should be 12345");
            AssertEqual("perlin", config.GenerationAlgorithm, "Algorithm should be perlin");
            AssertTrue(config.AlgorithmParameters.ContainsKey("scale"), "Should contain scale parameter");
            AssertEqual(1, config.Entities.Count, "Should have 1 entity configuration");
            AssertEqual(EntityType.Enemy, config.Entities[0].Type, "First entity should be Enemy");
            AssertEqual("forest", config.VisualTheme.ThemeName, "Theme name should be forest");
            AssertEqual(5.0f, config.Gameplay.PlayerSpeed, "Player speed should be 5.0");

            Console.WriteLine("✓ TestParseValidConfig passed");
        }

        /// <summary>
        /// Tests parsing configuration from string
        /// </summary>
        public void TestParseConfigFromString()
        {
            // Arrange
            var jsonString = @"{
                ""width"": 100,
                ""height"": 100,
                ""generationAlgorithm"": ""cellular""
            }";

            // Act
            var config = _parser.ParseConfigFromString(jsonString);

            // Assert
            AssertEqual(100, config.Width, "Width should be 100");
            AssertEqual(100, config.Height, "Height should be 100");
            AssertEqual("cellular", config.GenerationAlgorithm, "Algorithm should be cellular");

            Console.WriteLine("✓ TestParseConfigFromString passed");
        }

        /// <summary>
        /// Tests handling of invalid JSON
        /// </summary>
        public void TestParseInvalidJson()
        {
            // Arrange
            var invalidJson = @"{
                ""width"": 80,
                ""height"": 60,
                ""invalidProperty"": 
            }"; // Missing value and closing brace

            // Act & Assert
            try
            {
                _parser.ParseConfigFromString(invalidJson);
                AssertFail("Should have thrown InvalidOperationException for invalid JSON");
            }
            catch (InvalidOperationException ex)
            {
                AssertTrue(ex.Message.Contains("Invalid JSON format"), "Exception should mention invalid JSON format");
                Console.WriteLine("✓ TestParseInvalidJson passed");
            }
        }

        /// <summary>
        /// Tests handling of missing file
        /// </summary>
        public void TestParseMissingFile()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-config.json");

            // Act & Assert
            try
            {
                _parser.ParseConfig(nonExistentPath);
                AssertFail("Should have thrown FileNotFoundException for missing file");
            }
            catch (FileNotFoundException ex)
            {
                AssertTrue(ex.Message.Contains("Configuration file not found"), "Exception should mention file not found");
                Console.WriteLine("✓ TestParseMissingFile passed");
            }
        }

        /// <summary>
        /// Tests validation of valid configuration
        /// </summary>
        public void TestValidateValidConfig()
        {
            // Arrange
            var config = _parser.GetDefaultConfig();

            // Act
            var isValid = _parser.ValidateConfig(config, out var errors);

            // Assert
            AssertTrue(isValid, "Default config should be valid");
            AssertEqual(0, errors.Count, "Should have no validation errors");

            Console.WriteLine("✓ TestValidateValidConfig passed");
        }

        /// <summary>
        /// Tests validation of invalid configuration
        /// </summary>
        public void TestValidateInvalidConfig()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = -10, // Invalid width
                Height = 5000, // Invalid height
                GenerationAlgorithm = "invalid_algorithm", // Invalid algorithm
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = -5, // Invalid count
                        PlacementStrategy = "invalid_strategy" // Invalid strategy
                    }
                }
            };

            // Act
            var isValid = _parser.ValidateConfig(config, out var errors);

            // Assert
            AssertFalse(isValid, "Invalid config should not be valid");
            AssertTrue(errors.Count > 0, "Should have validation errors");
            AssertTrue(errors.Any(e => e.Contains("Width")), "Should have width validation error");
            AssertTrue(errors.Any(e => e.Contains("Height")), "Should have height validation error");
            AssertTrue(errors.Any(e => e.Contains("algorithm")), "Should have algorithm validation error");

            Console.WriteLine("✓ TestValidateInvalidConfig passed");
        }

        /// <summary>
        /// Tests getting default configuration
        /// </summary>
        public void TestGetDefaultConfig()
        {
            // Act
            var config = _parser.GetDefaultConfig();

            // Assert
            AssertNotNull(config, "Default config should not be null");
            AssertTrue(config.Width > 0, "Default width should be positive");
            AssertTrue(config.Height > 0, "Default height should be positive");
            AssertNotNull(config.GenerationAlgorithm, "Default algorithm should not be null");
            AssertNotNull(config.Entities, "Default entities should not be null");
            AssertNotNull(config.VisualTheme, "Default visual theme should not be null");
            AssertNotNull(config.Gameplay, "Default gameplay should not be null");

            Console.WriteLine("✓ TestGetDefaultConfig passed");
        }

        /// <summary>
        /// Tests applying defaults to incomplete configuration
        /// </summary>
        public void TestApplyDefaults()
        {
            // Arrange
            var incompleteJson = @"{
                ""width"": 5,
                ""generationAlgorithm"": ""invalid_algorithm""
            }";

            // Act
            var config = _parser.ParseConfigFromString(incompleteJson);

            // Assert
            AssertEqual(50, config.Width, "Width should be corrected to default value");
            AssertEqual("perlin", config.GenerationAlgorithm, "Algorithm should be corrected to default");
            AssertNotNull(config.Entities, "Entities should be initialized");
            AssertNotNull(config.VisualTheme, "Visual theme should be initialized");
            AssertNotNull(config.Gameplay, "Gameplay should be initialized");

            Console.WriteLine("✓ TestApplyDefaults passed");
        }

        /// <summary>
        /// Tests error handling for null or empty inputs
        /// </summary>
        public void TestErrorHandling()
        {
            // Test null path
            try
            {
                _parser.ParseConfig(null);
                AssertFail("Should throw ArgumentException for null path");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ Null path handling passed");
            }

            // Test empty path
            try
            {
                _parser.ParseConfig("");
                AssertFail("Should throw ArgumentException for empty path");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ Empty path handling passed");
            }

            // Test null JSON content
            try
            {
                _parser.ParseConfigFromString(null);
                AssertFail("Should throw ArgumentException for null JSON content");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ Null JSON content handling passed");
            }

            // Test empty JSON content
            try
            {
                _parser.ParseConfigFromString("");
                AssertFail("Should throw ArgumentException for empty JSON content");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✓ Empty JSON content handling passed");
            }

            Console.WriteLine("✓ TestErrorHandling passed");
        }

        /// <summary>
        /// Runs all tests
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("Running ConfigurationParser tests...\n");

            try
            {
                TestParseValidConfig();
                TestParseConfigFromString();
                TestParseInvalidJson();
                TestParseMissingFile();
                TestValidateValidConfig();
                TestValidateInvalidConfig();
                TestGetDefaultConfig();
                TestApplyDefaults();
                TestErrorHandling();

                Console.WriteLine("\n✅ All ConfigurationParser tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test failed: {ex.Message}");
                throw;
            }
            finally
            {
                // Cleanup
                if (File.Exists(_testConfigPath))
                    File.Delete(_testConfigPath);
                if (File.Exists(_invalidConfigPath))
                    File.Delete(_invalidConfigPath);
            }
        }

        // Helper assertion methods
        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception($"{message}. Expected: {expected}, Actual: {actual}");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition)
                throw new Exception(message);
        }

        private static void AssertNotNull(object obj, string message)
        {
            if (obj == null)
                throw new Exception(message);
        }

        private static void AssertFail(string message)
        {
            throw new Exception(message);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Configuration.Tests
{
    /// <summary>
    /// Unit tests for the configuration system
    /// Tests requirements 1.2, 1.3, and 1.4 from the specification
    /// </summary>
    public class ConfigurationSystemTests : IDisposable
    {
        private readonly ConfigurationParser _parser;
        private readonly List<string> _tempFiles;

        public ConfigurationSystemTests()
        {
            _parser = new ConfigurationParser();
            _tempFiles = new List<string>();
        }

        public void Dispose()
        {
            // Clean up temporary files
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        #region Valid Configuration Parsing Tests

        [Fact]
        public void ParseConfig_ValidJsonFile_ShouldParseCorrectly()
        {
            // Arrange
            var validJson = @"{
                ""width"": 80,
                ""height"": 60,
                ""seed"": 12345,
                ""generationAlgorithm"": ""perlin"",
                ""algorithmParameters"": {
                    ""scale"": 0.1,
                    ""octaves"": 4,
                    ""persistence"": 0.5
                },
                ""terrainTypes"": [""ground"", ""wall"", ""water""],
                ""entities"": [
                    {
                        ""type"": ""Enemy"",
                        ""count"": 5,
                        ""minDistance"": 2.0,
                        ""maxDistanceFromPlayer"": 50.0,
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
                        ""wall"": ""#654321"",
                        ""water"": ""#4169E1""
                    }
                },
                ""gameplay"": {
                    ""playerSpeed"": 5.0,
                    ""playerHealth"": 100,
                    ""difficulty"": ""normal"",
                    ""timeLimit"": 300.0,
                    ""victoryConditions"": [""reach_exit""]
                }
            }";

            var testFile = CreateTempFile(validJson);

            // Act
            var config = _parser.ParseConfig(testFile);

            // Assert
            Assert.Equal(80, config.Width);
            Assert.Equal(60, config.Height);
            Assert.Equal(12345, config.Seed);
            Assert.Equal("perlin", config.GenerationAlgorithm);
            Assert.Contains("scale", config.AlgorithmParameters.Keys);
            Assert.Equal(0.1, Convert.ToDouble(config.AlgorithmParameters["scale"]));
            Assert.Equal(3, config.TerrainTypes.Count);
            Assert.Contains("ground", config.TerrainTypes);
            Assert.Contains("wall", config.TerrainTypes);
            Assert.Contains("water", config.TerrainTypes);
            Assert.Equal(2, config.Entities.Count);
            Assert.Equal(EntityType.Enemy, config.Entities[0].Type);
            Assert.Equal(5, config.Entities[0].Count);
            Assert.Equal(EntityType.Item, config.Entities[1].Type);
            Assert.Equal(3, config.Entities[1].Count);
            Assert.Equal("forest", config.VisualTheme.ThemeName);
            Assert.Equal(3, config.VisualTheme.ColorPalette.Count);
            Assert.Equal(5.0f, config.Gameplay.PlayerSpeed);
            Assert.Equal(100, config.Gameplay.PlayerHealth);
            Assert.Equal("normal", config.Gameplay.Difficulty);
        }

        [Fact]
        public void ParseConfigFromString_ValidJson_ShouldParseCorrectly()
        {
            // Arrange
            var jsonString = @"{
                ""width"": 100,
                ""height"": 100,
                ""generationAlgorithm"": ""cellular"",
                ""entities"": [
                    {
                        ""type"": ""Enemy"",
                        ""count"": 3,
                        ""placementStrategy"": ""clustered""
                    }
                ]
            }";

            // Act
            var config = _parser.ParseConfigFromString(jsonString);

            // Assert
            Assert.Equal(100, config.Width);
            Assert.Equal(100, config.Height);
            Assert.Equal("cellular", config.GenerationAlgorithm);
            Assert.Single(config.Entities);
            Assert.Equal(EntityType.Enemy, config.Entities[0].Type);
            Assert.Equal(3, config.Entities[0].Count);
            Assert.Equal("clustered", config.Entities[0].PlacementStrategy);
        }

        [Fact]
        public void ParseConfig_JsonWithComments_ShouldParseCorrectly()
        {
            // Arrange
            var jsonWithComments = @"{
                // Configuration for test level
                ""width"": 50,
                ""height"": 50, // Square level
                ""generationAlgorithm"": ""perlin""
            }";

            // Act
            var config = _parser.ParseConfigFromString(jsonWithComments);

            // Assert
            Assert.Equal(50, config.Width);
            Assert.Equal(50, config.Height);
            Assert.Equal("perlin", config.GenerationAlgorithm);
        }

        [Fact]
        public void ParseConfig_JsonWithTrailingCommas_ShouldParseCorrectly()
        {
            // Arrange
            var jsonWithTrailingCommas = @"{
                ""width"": 50,
                ""height"": 50,
                ""terrainTypes"": [""ground"", ""wall"",],
            }";

            // Act
            var config = _parser.ParseConfigFromString(jsonWithTrailingCommas);

            // Assert
            Assert.Equal(50, config.Width);
            Assert.Equal(50, config.Height);
            Assert.Equal(2, config.TerrainTypes.Count);
        }

        #endregion

        #region Invalid Configuration Handling Tests

        [Fact]
        public void ParseConfig_NullFilePath_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _parser.ParseConfig(null));
            Assert.Contains("JSON file path cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ParseConfig_EmptyFilePath_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _parser.ParseConfig(""));
            Assert.Contains("JSON file path cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ParseConfig_NonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-config-xyz.json");

            // Act & Assert
            var exception = Assert.Throws<FileNotFoundException>(() => _parser.ParseConfig(nonExistentPath));
            Assert.Contains("Configuration file not found", exception.Message);
        }

        [Fact]
        public void ParseConfigFromString_NullJsonContent_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _parser.ParseConfigFromString(null));
            Assert.Contains("JSON content cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ParseConfigFromString_EmptyJsonContent_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _parser.ParseConfigFromString(""));
            Assert.Contains("JSON content cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ParseConfigFromString_InvalidJson_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var invalidJson = @"{
                ""width"": 80,
                ""height"": 60,
                ""invalidProperty"": 
            }"; // Missing value and closing brace

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _parser.ParseConfigFromString(invalidJson));
            Assert.Contains("Invalid JSON format", exception.Message);
        }

        [Fact]
        public void ParseConfigFromString_MalformedJson_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var malformedJson = @"{ width: 50, height: 50 }"; // Missing quotes around property names

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _parser.ParseConfigFromString(malformedJson));
            Assert.Contains("Invalid JSON format", exception.Message);
        }

        [Fact]
        public void ValidateConfig_NullConfig_ShouldReturnFalseWithErrors()
        {
            // Act
            var isValid = _parser.ValidateConfig(null, out var errors);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(errors);
            Assert.Contains("Configuration cannot be null", errors);
        }

        [Fact]
        public void ValidateConfig_InvalidConfig_ShouldReturnFalseWithSpecificErrors()
        {
            // Arrange
            var invalidConfig = new GenerationConfig
            {
                Width = -10, // Invalid width
                Height = 5000, // Invalid height
                GenerationAlgorithm = "invalid_algorithm", // Invalid algorithm
                TerrainTypes = new List<string> { "invalid_terrain" }, // Invalid terrain type
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = -5, // Invalid count
                        MinDistance = 150.0f, // Too large
                        PlacementStrategy = "invalid_strategy" // Invalid strategy
                    }
                }
            };

            // Act
            var isValid = _parser.ValidateConfig(invalidConfig, out var errors);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("Width"));
            Assert.Contains(errors, e => e.Contains("Height"));
            Assert.Contains(errors, e => e.Contains("algorithm"));
            Assert.Contains(errors, e => e.Contains("terrain"));
            Assert.Contains(errors, e => e.Contains("count") || e.Contains("Count"));
            Assert.Contains(errors, e => e.Contains("strategy"));
        }

        #endregion

        #region Default Value Application Tests

        [Fact]
        public void ParseConfigFromString_IncompleteConfig_ShouldApplyDefaults()
        {
            // Arrange
            var incompleteJson = @"{
                ""width"": 5,
                ""height"": 2000,
                ""generationAlgorithm"": ""invalid_algorithm""
            }";

            // Act
            var config = _parser.ParseConfigFromString(incompleteJson);

            // Assert - Defaults should be applied for invalid values
            Assert.Equal(50, config.Width); // Default width applied
            Assert.Equal(50, config.Height); // Default height applied
            Assert.Equal("perlin", config.GenerationAlgorithm); // Default algorithm applied
            Assert.NotNull(config.Entities); // Default entities list created
            Assert.NotNull(config.VisualTheme); // Default visual theme created
            Assert.NotNull(config.Gameplay); // Default gameplay config created
            Assert.NotNull(config.AlgorithmParameters); // Default algorithm parameters created
        }

        [Fact]
        public void ParseConfigFromString_EmptyJsonObject_ShouldApplyAllDefaults()
        {
            // Arrange
            var emptyJson = "{}";

            // Act
            var config = _parser.ParseConfigFromString(emptyJson);

            // Assert - All defaults should be applied
            Assert.Equal(50, config.Width);
            Assert.Equal(50, config.Height);
            Assert.Equal("perlin", config.GenerationAlgorithm);
            Assert.NotNull(config.Entities);
            Assert.NotNull(config.VisualTheme);
            Assert.NotNull(config.Gameplay);
            Assert.NotNull(config.AlgorithmParameters);
            Assert.NotNull(config.TerrainTypes);
        }

        [Fact]
        public void GetDefaultConfig_ShouldReturnValidConfiguration()
        {
            // Act
            var config = _parser.GetDefaultConfig();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Width > 0);
            Assert.True(config.Height > 0);
            Assert.NotNull(config.GenerationAlgorithm);
            Assert.NotEmpty(config.GenerationAlgorithm);
            Assert.NotNull(config.Entities);
            Assert.NotEmpty(config.Entities);
            Assert.NotNull(config.VisualTheme);
            Assert.NotNull(config.Gameplay);
            Assert.NotNull(config.AlgorithmParameters);
            Assert.NotNull(config.TerrainTypes);
            Assert.NotEmpty(config.TerrainTypes);

            // Verify default config is valid
            var isValid = _parser.ValidateConfig(config, out var errors);
            Assert.True(isValid);
            Assert.Empty(errors);
        }

        [Fact]
        public void GetDefaultConfig_ShouldContainExpectedDefaults()
        {
            // Act
            var config = _parser.GetDefaultConfig();

            // Assert specific default values
            Assert.Equal(50, config.Width);
            Assert.Equal(50, config.Height);
            Assert.Equal("perlin", config.GenerationAlgorithm);
            Assert.Contains("ground", config.TerrainTypes);
            Assert.Contains("wall", config.TerrainTypes);
            Assert.Contains("water", config.TerrainTypes);
            Assert.Contains(config.Entities, e => e.Type == EntityType.Enemy);
            Assert.Contains(config.Entities, e => e.Type == EntityType.Item);
            Assert.Contains(config.Entities, e => e.Type == EntityType.Exit);
            Assert.Equal("default", config.VisualTheme.ThemeName);
            Assert.Equal("normal", config.Gameplay.Difficulty);
            Assert.Equal(5.0f, config.Gameplay.PlayerSpeed);
            Assert.Equal(100, config.Gameplay.PlayerHealth);
        }

        [Fact]
        public void ApplyDefaults_ShouldGenerateWarnings()
        {
            // Arrange
            var configWithInvalidValues = new GenerationConfig
            {
                Width = 5, // Too small
                Height = 2000, // Too large
                GenerationAlgorithm = "invalid_algorithm",
                Entities = null,
                VisualTheme = null,
                Gameplay = null,
                AlgorithmParameters = null
            };

            // Act
            var warnings = configWithInvalidValues.ApplyDefaults();

            // Assert
            Assert.NotEmpty(warnings);
            Assert.Contains(warnings, w => w.Contains("Width"));
            Assert.Contains(warnings, w => w.Contains("Height"));
            Assert.Contains(warnings, w => w.Contains("algorithm"));
            Assert.Contains(warnings, w => w.Contains("Entities"));
            Assert.Contains(warnings, w => w.Contains("Visual theme"));
            Assert.Contains(warnings, w => w.Contains("Gameplay"));
            Assert.Contains(warnings, w => w.Contains("Algorithm parameters"));
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void ValidateConfig_ValidConfig_ShouldReturnTrueWithNoErrors()
        {
            // Arrange
            var validConfig = _parser.GetDefaultConfig();

            // Act
            var isValid = _parser.ValidateConfig(validConfig, out var errors);

            // Assert
            Assert.True(isValid);
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateConfig_ConfigWithValidationException_ShouldHandleGracefully()
        {
            // Arrange - Create a config that might cause validation exceptions
            var problematicConfig = new GenerationConfig
            {
                Width = 50,
                Height = 50,
                GenerationAlgorithm = "perlin",
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = 1,
                        MinDistance = float.MaxValue, // Extreme value that might cause issues
                        MaxDistanceFromPlayer = 0.0f, // Inconsistent with MinDistance
                        PlacementStrategy = "random"
                    }
                }
            };

            // Act
            var isValid = _parser.ValidateConfig(problematicConfig, out var errors);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(errors);
        }

        #endregion

        #region Helper Methods

        private string CreateTempFile(string content)
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.json");
            File.WriteAllText(tempFile, content);
            _tempFiles.Add(tempFile);
            return tempFile;
        }

        #endregion
    }
}
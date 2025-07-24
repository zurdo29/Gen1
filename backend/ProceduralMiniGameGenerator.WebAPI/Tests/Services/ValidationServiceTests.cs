using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    public class ValidationServiceTests
    {
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ValidationService _validationService;

        public ValidationServiceTests()
        {
            _mockLogger = new Mock<ILoggerService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _validationService = new ValidationService(_mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithValidConfig_ReturnsValid()
        {
            // Arrange
            var config = new
            {
                width = 50,
                height = 50,
                seed = 12345,
                generationAlgorithm = "perlin",
                algorithmParameters = new { },
                terrainTypes = new[] { "ground", "wall", "water" },
                entities = new[]
                {
                    new
                    {
                        type = "Player",
                        count = 1,
                        minDistance = 0,
                        maxDistanceFromPlayer = 0,
                        properties = new { },
                        placementStrategy = "center"
                    }
                },
                visualTheme = new
                {
                    themeName = "default",
                    colorPalette = new { ground = "#8B4513" },
                    tileSprites = new { },
                    entitySprites = new { },
                    effectSettings = new { }
                },
                gameplay = new
                {
                    playerSpeed = 5.0,
                    playerHealth = 100,
                    difficulty = "normal",
                    timeLimit = 300,
                    victoryConditions = new[] { "reach_exit" },
                    mechanics = new { }
                }
            };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithMissingWidth_ReturnsInvalid()
        {
            // Arrange
            var config = new
            {
                height = 50,
                seed = 12345,
                generationAlgorithm = "perlin",
                terrainTypes = new[] { "ground" }
            };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "width" && e.Code == "MISSING_WIDTH");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithInvalidWidth_ReturnsInvalid()
        {
            // Arrange
            var config = new
            {
                width = 5, // Too small
                height = 50,
                seed = 12345,
                generationAlgorithm = "perlin",
                terrainTypes = new[] { "ground" }
            };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "width" && e.Code == "WIDTH_TOO_SMALL");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithInvalidHeight_ReturnsInvalid()
        {
            // Arrange
            var config = new
            {
                width = 50,
                height = 1500, // Too large
                seed = 12345,
                generationAlgorithm = "perlin",
                terrainTypes = new[] { "ground" }
            };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "height" && e.Code == "HEIGHT_TOO_LARGE");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithInvalidAlgorithm_ReturnsInvalid()
        {
            // Arrange
            var config = new
            {
                width = 50,
                height = 50,
                seed = 12345,
                generationAlgorithm = "invalid-algorithm",
                terrainTypes = new[] { "ground" }
            };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "generationAlgorithm" && e.Code == "INVALID_ALGORITHM");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithCrossFieldValidation_DetectsEntityDensityIssues()
        {
            // Arrange
            var config = new
            {
                width = 10,
                height = 10,
                seed = 12345,
                generationAlgorithm = "perlin",
                terrainTypes = new[] { "ground" },
                entities = new[]
                {
                    new
                    {
                        type = "Player",
                        count = 1,
                        minDistance = 0,
                        maxDistanceFromPlayer = 0,
                        properties = new { },
                        placementStrategy = "center"
                    },
                    new
                    {
                        type = "Enemy",
                        count = 100, // Too many for small level
                        minDistance = 1,
                        maxDistanceFromPlayer = 10,
                        properties = new { },
                        placementStrategy = "random"
                    }
                }
            };

            var options = new ValidationOptions { CrossFieldValidation = true };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config, options);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "entities" && e.Code == "ENTITY_DENSITY_TOO_HIGH");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithMissingPlayerEntity_ReturnsInvalid()
        {
            // Arrange
            var config = new
            {
                width = 50,
                height = 50,
                seed = 12345,
                generationAlgorithm = "perlin",
                terrainTypes = new[] { "ground" },
                entities = new[]
                {
                    new
                    {
                        type = "Enemy",
                        count = 5,
                        minDistance = 1,
                        maxDistanceFromPlayer = 10,
                        properties = new { },
                        placementStrategy = "random"
                    }
                }
            };

            var options = new ValidationOptions { CrossFieldValidation = true };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config, options);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "entities" && e.Code == "MISSING_PLAYER_ENTITY");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithPerformanceCheck_DetectsLargeLevels()
        {
            // Arrange
            var config = new
            {
                width = 500,
                height = 500, // Very large level
                seed = 12345,
                generationAlgorithm = "perlin",
                terrainTypes = new[] { "ground" }
            };

            var options = new ValidationOptions 
            { 
                PerformanceCheck = true,
                MaxLevelArea = 100000 
            };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config, options);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "width,height" && e.Code == "LEVEL_SIZE_TOO_LARGE");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithStrictMode_ConvertsWarningsToErrors()
        {
            // Arrange
            var config = new
            {
                width = 300,
                height = 300, // Large but not too large
                seed = 12345,
                generationAlgorithm = "perlin",
                terrainTypes = new[] { "ground" }
            };

            var options = new ValidationOptions 
            { 
                Strict = true,
                PerformanceCheck = true,
                MaxLevelArea = 100000 
            };

            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(config, options);

            // Assert
            Assert.False(result.IsValid); // Should be invalid in strict mode
            Assert.NotEmpty(result.Errors);
            Assert.Empty(result.Warnings); // Warnings should be converted to errors
        }

        [Fact]
        public async Task ValidateEntityConfigAsync_WithValidEntity_ReturnsValid()
        {
            // Arrange
            var entity = new
            {
                type = "Player",
                count = 1,
                minDistance = 0,
                maxDistanceFromPlayer = 0,
                properties = new { },
                placementStrategy = "center"
            };

            // Act
            var result = await _validationService.ValidateEntityConfigAsync(entity);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateEntityConfigAsync_WithInvalidType_ReturnsInvalid()
        {
            // Arrange
            var entity = new
            {
                type = "InvalidType",
                count = 1,
                minDistance = 0,
                maxDistanceFromPlayer = 0,
                properties = new { },
                placementStrategy = "center"
            };

            // Act
            var result = await _validationService.ValidateEntityConfigAsync(entity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "type" && e.Code == "INVALID_ENTITY_TYPE");
        }

        [Fact]
        public async Task ValidateEntityConfigAsync_WithNegativeCount_ReturnsInvalid()
        {
            // Arrange
            var entity = new
            {
                type = "Enemy",
                count = -5,
                minDistance = 0,
                maxDistanceFromPlayer = 10,
                properties = new { },
                placementStrategy = "random"
            };

            // Act
            var result = await _validationService.ValidateEntityConfigAsync(entity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "count" && e.Code == "NEGATIVE_COUNT");
        }

        [Fact]
        public async Task ValidateEntityConfigAsync_WithHighCount_ReturnsWarning()
        {
            // Arrange
            var entity = new
            {
                type = "Enemy",
                count = 150, // High count
                minDistance = 1,
                maxDistanceFromPlayer = 10,
                properties = new { },
                placementStrategy = "random"
            };

            // Act
            var result = await _validationService.ValidateEntityConfigAsync(entity);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Field == "count");
        }

        [Fact]
        public async Task ValidateEntityConfigAsync_WithInvalidDistanceConstraint_ReturnsInvalid()
        {
            // Arrange
            var entity = new
            {
                type = "Enemy",
                count = 5,
                minDistance = 15,
                maxDistanceFromPlayer = 10, // Min > Max
                properties = new { },
                placementStrategy = "random"
            };

            // Act
            var result = await _validationService.ValidateEntityConfigAsync(entity);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "minDistance" && e.Code == "INVALID_DISTANCE_CONSTRAINT");
        }

        [Fact]
        public async Task ValidateExportRequestAsync_WithValidRequest_ReturnsValid()
        {
            // Arrange
            var request = new ExportRequest
            {
                Level = new { width = 50, height = 50, terrain = new { }, entities = new[] { new { } } },
                Format = "json",
                Options = new Dictionary<string, object>()
            };

            // Act
            var result = await _validationService.ValidateExportRequestAsync(request);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateExportRequestAsync_WithInvalidFormat_ReturnsInvalid()
        {
            // Arrange
            var request = new ExportRequest
            {
                Level = new { },
                Format = "invalid-format",
                Options = new Dictionary<string, object>()
            };

            // Act
            var result = await _validationService.ValidateExportRequestAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "format" && e.Code == "INVALID_FORMAT");
        }

        [Fact]
        public async Task ValidateExportRequestAsync_WithMissingLevel_ReturnsInvalid()
        {
            // Arrange
            var request = new ExportRequest
            {
                Level = null,
                Format = "json",
                Options = new Dictionary<string, object>()
            };

            // Act
            var result = await _validationService.ValidateExportRequestAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "level" && e.Code == "MISSING_LEVEL");
        }

        [Fact]
        public async Task ValidateBatchGenerationRequestAsync_WithValidRequest_ReturnsValid()
        {
            // Arrange
            var request = new BatchGenerationRequest
            {
                BaseConfig = new
                {
                    width = 50,
                    height = 50,
                    seed = 12345,
                    generationAlgorithm = "perlin",
                    terrainTypes = new[] { "ground" }
                },
                Count = 5,
                Variations = new List<object>()
            };

            // Act
            var result = await _validationService.ValidateBatchGenerationRequestAsync(request);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateBatchGenerationRequestAsync_WithInvalidCount_ReturnsInvalid()
        {
            // Arrange
            var request = new BatchGenerationRequest
            {
                BaseConfig = new { },
                Count = 0, // Invalid count
                Variations = new List<object>()
            };

            // Act
            var result = await _validationService.ValidateBatchGenerationRequestAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "count" && e.Code == "INVALID_COUNT");
        }

        [Fact]
        public async Task ValidateBatchGenerationRequestAsync_WithHighCount_ReturnsInvalid()
        {
            // Arrange
            var request = new BatchGenerationRequest
            {
                BaseConfig = new { },
                Count = 100, // Too high
                Variations = new List<object>()
            };

            // Act
            var result = await _validationService.ValidateBatchGenerationRequestAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Field == "count" && e.Code == "COUNT_TOO_HIGH");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_WithNullConfig_HandlesGracefully()
        {
            // Act
            var result = await _validationService.ValidateGenerationConfigAsync(null);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Code == "INVALID_FORMAT");
        }

        [Fact]
        public async Task ValidateGenerationConfigAsync_LogsValidationProcess()
        {
            // Arrange
            var config = new { width = 50, height = 50 };

            // Act
            await _validationService.ValidateGenerationConfigAsync(config);

            // Assert
            _mockLogger.Verify(
                x => x.LogAsync(
                    Microsoft.Extensions.Logging.LogLevel.Debug,
                    "Starting generation config validation",
                    It.IsAny<object>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.LogAsync(
                    Microsoft.Extensions.Logging.LogLevel.Debug,
                    "Completed generation config validation",
                    It.IsAny<object>()),
                Times.Once);
        }
    }
}
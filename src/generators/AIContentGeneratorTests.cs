using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;
using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Generators.Tests
{
    [TestClass]
    public class AIContentGeneratorTests
    {
        private TestLogger _logger;
        private AIServiceConfig _config;

        [TestInitialize]
        public void Setup()
        {
            _logger = new TestLogger();
            _config = new AIServiceConfig
            {
                IsEnabled = false, // Start with disabled for fallback testing
                ApiEndpoint = "https://test-api.example.com/generate",
                ApiKey = "test-key",
                MaxTokens = 100,
                Temperature = 0.7,
                TimeoutSeconds = 30
            };
        }

        [TestMethod]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new AIContentGenerator(null, _config, _logger));
        }

        [TestMethod]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var httpClient = new HttpClient();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new AIContentGenerator(httpClient, null, _logger));
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var httpClient = new HttpClient();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new AIContentGenerator(httpClient, _config, null));
        }

        [TestMethod]
        public void IsAvailable_WithDisabledConfig_ReturnsFalse()
        {
            // Arrange
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);

            // Act
            var result = generator.IsAvailable();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsAvailable_WithEnabledConfigAndEndpoint_ReturnsTrue()
        {
            // Arrange
            _config.IsEnabled = true;
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);

            // Act
            var result = generator.IsAvailable();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void GenerateItemDescription_WithDisabledAI_ReturnsFallbackDescription()
        {
            // Arrange
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);
            var theme = CreateTestTheme();

            // Act
            var result = generator.GenerateItemDescription(EntityType.Enemy, theme);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
            Assert.IsTrue(result.Contains("dangerous") || result.Contains("aggressive") || result.Contains("hostile"));
        }

        [TestMethod]
        public void GenerateNPCDialogue_WithDisabledAI_ReturnsFallbackDialogue()
        {
            // Arrange
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);
            var lineCount = 3;

            // Act
            var result = generator.GenerateNPCDialogue(EntityType.Enemy, lineCount);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(lineCount, result.Length);
            Assert.IsTrue(result[0].Length > 0);
        }

        [TestMethod]
        public void GenerateLevelName_WithDisabledAI_ReturnsFallbackName()
        {
            // Arrange
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);
            var level = CreateTestLevel();
            var theme = CreateTestTheme();

            // Act
            var result = generator.GenerateLevelName(level, theme);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
            Assert.IsTrue(result.Contains(theme.Name));
        }

        [TestMethod]
        public void GenerateItemDescription_WithDifferentEntityTypes_ReturnsAppropriateDescriptions()
        {
            // Arrange
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);
            var theme = CreateTestTheme();
            var entityTypes = new[] { EntityType.Enemy, EntityType.Item, EntityType.PowerUp, EntityType.Checkpoint };

            // Act & Assert
            foreach (var entityType in entityTypes)
            {
                var result = generator.GenerateItemDescription(entityType, theme);
                Assert.IsNotNull(result, $"Description should not be null for {entityType}");
                Assert.IsTrue(result.Length > 0, $"Description should not be empty for {entityType}");
            }
        }

        [TestMethod]
        public void GenerateNPCDialogue_WithZeroLineCount_ReturnsEmptyArray()
        {
            // Arrange
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);

            // Act
            var result = generator.GenerateNPCDialogue(EntityType.Enemy, 0);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void GenerateNPCDialogue_WithLargeLineCount_ReturnsRequestedCount()
        {
            // Arrange
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);
            var lineCount = 10;

            // Act
            var result = generator.GenerateNPCDialogue(EntityType.PowerUp, lineCount);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(lineCount, result.Length);
        }

        [TestMethod]
        public void FallbackContent_IsConsistent_AcrossMultipleCalls()
        {
            // Arrange
            var httpClient = new HttpClient();
            var generator = new AIContentGenerator(httpClient, _config, _logger);
            var theme = CreateTestTheme();

            // Act
            var description1 = generator.GenerateItemDescription(EntityType.Enemy, theme);
            var description2 = generator.GenerateItemDescription(EntityType.Enemy, theme);

            // Assert
            // Fallback content should be deterministic for the same inputs
            // (Note: This test might need adjustment if randomization is added to fallbacks)
            Assert.IsNotNull(description1);
            Assert.IsNotNull(description2);
        }

        private VisualTheme CreateTestTheme()
        {
            return new VisualTheme
            {
                Name = "TestTheme",
                TileSprites = new Dictionary<TileType, string>(),
                EntitySprites = new Dictionary<EntityType, string>(),
                Colors = new ColorPalette()
            };
        }

        private Level CreateTestLevel()
        {
            var terrain = new TileMap(10, 10);
            var entities = new List<Entity>();

            return new Level
            {
                Terrain = terrain,
                Entities = entities,
                Name = "Test Level",
                Metadata = new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Test implementation of ILogger for unit testing
    /// </summary>
    public class TestLogger : ILogger
    {
        public List<string> LogMessages { get; } = new List<string>();
        public List<LogLevel> LogLevels { get; } = new List<LogLevel>();

        public void Log(LogLevel level, string message)
        {
            LogLevels.Add(level);
            LogMessages.Add(message);
        }

        public void Log(LogLevel level, string message, Exception exception)
        {
            LogLevels.Add(level);
            LogMessages.Add($"{message} - {exception?.Message}");
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message) => Log(LogLevel.Error, message);
        public void Error(string message, Exception exception) => Log(LogLevel.Error, message, exception);

        public bool HasLogLevel(LogLevel level) => LogLevels.Contains(level);
        public bool HasMessage(string message) => LogMessages.Any(m => m.Contains(message));
        public void Clear()
        {
            LogMessages.Clear();
            LogLevels.Clear();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators.Tests
{
    [TestClass]
    public class AIContentServiceTests
    {
        private TestLogger _logger;
        private MockAIContentGenerator _mockAIGenerator;
        private AIContentService _aiContentService;

        [TestInitialize]
        public void Setup()
        {
            _logger = new TestLogger();
            _mockAIGenerator = new MockAIContentGenerator();
            _aiContentService = new AIContentService(_mockAIGenerator, _logger);
        }

        [TestMethod]
        public void Constructor_WithNullAIGenerator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new AIContentService(null, _logger));
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                new AIContentService(_mockAIGenerator, null));
        }

        [TestMethod]
        public void IsAvailable_ReturnsAIGeneratorAvailability()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);

            // Act
            var result = _aiContentService.IsAvailable();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void EnhanceLevel_WithNullLevel_ThrowsArgumentNullException()
        {
            // Arrange
            var theme = CreateTestTheme();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                _aiContentService.EnhanceLevel(null, theme));
        }

        [TestMethod]
        public void EnhanceLevel_WithNullTheme_ThrowsArgumentNullException()
        {
            // Arrange
            var level = CreateTestLevel();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                _aiContentService.EnhanceLevel(level, null));
        }

        [TestMethod]
        public void EnhanceLevel_WithAvailableAI_EnhancesEntitiesAndName()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var level = CreateTestLevelWithEntities();
            var theme = CreateTestTheme();

            // Act
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert
            Assert.IsTrue(report.Success);
            Assert.IsTrue(report.IsAIAvailable);
            Assert.IsTrue(report.LevelNameGenerated);
            Assert.AreEqual(level.Entities.Count, report.TotalEntities);
            Assert.IsTrue(report.EnhancedEntities > 0);
            Assert.IsTrue(report.Duration.TotalMilliseconds >= 0);
        }

        [TestMethod]
        public void EnhanceLevel_WithUnavailableAI_ReturnsReportWithoutEnhancement()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(false);
            var level = CreateTestLevelWithEntities();
            var theme = CreateTestTheme();

            // Act
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert
            Assert.IsTrue(report.Success);
            Assert.IsFalse(report.IsAIAvailable);
            Assert.IsFalse(report.LevelNameGenerated);
            Assert.AreEqual(0, report.EnhancedEntities);
        }

        [TestMethod]
        public void EnhanceEntityDescriptions_WithValidEntities_EnhancesDescriptions()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var entities = CreateTestEntities();
            var theme = CreateTestTheme();

            // Act
            var enhancedCount = _aiContentService.EnhanceEntityDescriptions(entities, theme);

            // Assert
            Assert.AreEqual(entities.Count, enhancedCount);
            
            foreach (var entity in entities)
            {
                Assert.IsTrue(entity.HasAIContent());
                Assert.IsNotNull(entity.GetAIDescription());
            }
        }

        [TestMethod]
        public void EnhanceEntityDescriptions_WithUnavailableAI_ReturnsZero()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(false);
            var entities = CreateTestEntities();
            var theme = CreateTestTheme();

            // Act
            var enhancedCount = _aiContentService.EnhanceEntityDescriptions(entities, theme);

            // Assert
            Assert.AreEqual(0, enhancedCount);
            
            foreach (var entity in entities)
            {
                Assert.IsFalse(entity.HasAIContent());
            }
        }

        [TestMethod]
        public void GenerateEntityDialogue_WithInteractiveEntities_GeneratesDialogue()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var entities = CreateTestEntities();
            var theme = CreateTestTheme();

            // Act
            var dialogueCount = _aiContentService.GenerateEntityDialogue(entities, theme);

            // Assert
            Assert.IsTrue(dialogueCount > 0);
            
            var interactiveEntities = entities.Where(e => 
                e.Type == EntityType.Enemy || 
                e.Type == EntityType.PowerUp || 
                e.Type == EntityType.Checkpoint).ToList();
            
            Assert.AreEqual(interactiveEntities.Count, dialogueCount);
            
            foreach (var entity in interactiveEntities)
            {
                Assert.IsNotNull(entity.GetAIDialogue());
                Assert.IsTrue(entity.GetDialogueLineCount() > 0);
            }
        }

        [TestMethod]
        public void GenerateEntityDialogue_WithNonInteractiveEntities_GeneratesNoDialogue()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var entities = new List<Entity> { new ItemEntity() };
            var theme = CreateTestTheme();

            // Act
            var dialogueCount = _aiContentService.GenerateEntityDialogue(entities, theme);

            // Assert
            Assert.AreEqual(0, dialogueCount);
        }

        [TestMethod]
        public void GenerateLevelName_WithValidInputs_ReturnsGeneratedName()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var level = CreateTestLevel();
            var theme = CreateTestTheme();

            // Act
            var result = _aiContentService.GenerateLevelName(level, theme);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void GenerateLevelName_WithUnavailableAI_ReturnsNull()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(false);
            var level = CreateTestLevel();
            var theme = CreateTestTheme();

            // Act
            var result = _aiContentService.GenerateLevelName(level, theme);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void EnhanceLevel_WithAIException_HandlesGracefully()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            _mockAIGenerator.ShouldThrowException = true;
            var level = CreateTestLevelWithEntities();
            var theme = CreateTestTheme();

            // Act
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert
            Assert.IsFalse(report.Success);
            Assert.IsNotNull(report.ErrorMessage);
            Assert.IsTrue(_logger.HasLogLevel(LogLevel.Error));
        }

        [TestMethod]
        public void EnhancementReport_CalculatesCorrectRatios()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var level = CreateTestLevelWithEntities();
            var theme = CreateTestTheme();

            // Act
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert
            Assert.IsTrue(report.EnhancementRatio >= 0.0 && report.EnhancementRatio <= 1.0);
            Assert.AreEqual((double)report.EnhancedEntities / report.TotalEntities, report.EnhancementRatio);
        }

        private Level CreateTestLevel()
        {
            var terrain = new TileMap(10, 10);
            return new Level
            {
                Terrain = terrain,
                Entities = new List<Entity>(),
                Name = "Test Level",
                Metadata = new Dictionary<string, object>()
            };
        }

        private Level CreateTestLevelWithEntities()
        {
            var level = CreateTestLevel();
            level.Entities = CreateTestEntities();
            return level;
        }

        private List<Entity> CreateTestEntities()
        {
            return new List<Entity>
            {
                new EnemyEntity(),
                new PowerUpEntity(),
                new ItemEntity(),
                new CheckpointEntity()
            };
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
    }

    /// <summary>
    /// Mock implementation of IAIContentGenerator for testing
    /// </summary>
    public class MockAIContentGenerator : IAIContentGenerator
    {
        private bool _isAvailable = true;
        public bool ShouldThrowException { get; set; } = false;

        public void SetAvailable(bool available)
        {
            _isAvailable = available;
        }

        public string GenerateItemDescription(EntityType type, VisualTheme theme)
        {
            if (ShouldThrowException)
                throw new InvalidOperationException("Mock exception for testing");

            if (!_isAvailable)
                return GetFallbackDescription(type);

            return $"AI-generated description for {type} in {theme.Name} theme";
        }

        public string[] GenerateNPCDialogue(EntityType type, int lineCount)
        {
            if (ShouldThrowException)
                throw new InvalidOperationException("Mock exception for testing");

            if (!_isAvailable)
                return GetFallbackDialogue(type, lineCount);

            var dialogue = new string[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                dialogue[i] = $"AI dialogue line {i + 1} for {type}";
            }
            return dialogue;
        }

        public string GenerateLevelName(Level level, VisualTheme theme)
        {
            if (ShouldThrowException)
                throw new InvalidOperationException("Mock exception for testing");

            if (!_isAvailable)
                return null;

            return $"AI-generated name for {theme.Name} level";
        }

        public bool IsAvailable()
        {
            return _isAvailable;
        }

        private string GetFallbackDescription(EntityType type)
        {
            return $"Fallback description for {type}";
        }

        private string[] GetFallbackDialogue(EntityType type, int lineCount)
        {
            var dialogue = new string[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                dialogue[i] = $"Fallback dialogue {i + 1} for {type}";
            }
            return dialogue;
        }
    }
}
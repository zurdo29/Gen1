using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators.Tests
{
    public class AIContentServiceTests
    {
        private readonly TestLogger _logger;
        private readonly MockAIContentGenerator _mockAIGenerator;
        private readonly AIContentService _aiContentService;

        public AIContentServiceTests()
        {
            _logger = new TestLogger();
            _mockAIGenerator = new MockAIContentGenerator();
            _aiContentService = new AIContentService(_mockAIGenerator, _logger);
        }

        [Fact]
        public void Constructor_WithNullAIGenerator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AIContentService(null, _logger));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AIContentService(_mockAIGenerator, null));
        }

        [Fact]
        public void IsAvailable_ReturnsAIGeneratorAvailability()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);

            // Act
            var result = _aiContentService.IsAvailable();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EnhanceLevel_WithNullLevel_ThrowsArgumentNullException()
        {
            // Arrange
            var theme = CreateTestTheme();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _aiContentService.EnhanceLevel(null, theme));
        }

        [Fact]
        public void EnhanceLevel_WithNullTheme_ThrowsArgumentNullException()
        {
            // Arrange
            var level = CreateTestLevel();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _aiContentService.EnhanceLevel(level, null));
        }

        [Fact]
        public void EnhanceLevel_WithAvailableAI_EnhancesEntitiesAndName()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var level = CreateTestLevelWithEntities();
            var theme = CreateTestTheme();

            // Act
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert
            Assert.True(report.Success);
            Assert.True(report.IsAIAvailable);
            Assert.True(report.LevelNameGenerated);
            Assert.Equal(level.Entities.Count, report.TotalEntities);
            Assert.True(report.EnhancedEntities > 0);
            Assert.True(report.Duration.TotalMilliseconds >= 0);
        }

        [Fact]
        public void EnhanceLevel_WithUnavailableAI_ReturnsReportWithoutEnhancement()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(false);
            var level = CreateTestLevelWithEntities();
            var theme = CreateTestTheme();

            // Act
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert
            Assert.True(report.Success);
            Assert.False(report.IsAIAvailable);
            Assert.False(report.LevelNameGenerated);
            Assert.Equal(0, report.EnhancedEntities);
        }

        [Fact]
        public void EnhanceEntityDescriptions_WithValidEntities_EnhancesDescriptions()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var entities = CreateTestEntities();
            var theme = CreateTestTheme();

            // Act
            var enhancedCount = _aiContentService.EnhanceEntityDescriptions(entities, theme);

            // Assert
            Assert.Equal(entities.Count, enhancedCount);
            
            foreach (var entity in entities)
            {
                Assert.True(entity.HasAIContent());
                Assert.NotNull(entity.GetAIDescription());
            }
        }

        [Fact]
        public void EnhanceEntityDescriptions_WithUnavailableAI_ReturnsZero()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(false);
            var entities = CreateTestEntities();
            var theme = CreateTestTheme();

            // Act
            var enhancedCount = _aiContentService.EnhanceEntityDescriptions(entities, theme);

            // Assert
            Assert.Equal(0, enhancedCount);
            
            foreach (var entity in entities)
            {
                Assert.False(entity.HasAIContent());
            }
        }

        [Fact]
        public void GenerateEntityDialogue_WithInteractiveEntities_GeneratesDialogue()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var entities = CreateTestEntities();
            var theme = CreateTestTheme();

            // Act
            var dialogueCount = _aiContentService.GenerateEntityDialogue(entities, theme);

            // Assert
            Assert.True(dialogueCount > 0);
            
            var interactiveEntities = entities.Where(e => 
                e.Type == EntityType.Enemy || 
                e.Type == EntityType.PowerUp || 
                e.Type == EntityType.Checkpoint).ToList();
            
            Assert.Equal(interactiveEntities.Count, dialogueCount);
            
            foreach (var entity in interactiveEntities)
            {
                Assert.NotNull(entity.GetAIDialogue());
                Assert.True(entity.GetDialogueLineCount() > 0);
            }
        }

        [Fact]
        public void GenerateEntityDialogue_WithNonInteractiveEntities_GeneratesNoDialogue()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var entities = new List<Entity> { new ItemEntity() };
            var theme = CreateTestTheme();

            // Act
            var dialogueCount = _aiContentService.GenerateEntityDialogue(entities, theme);

            // Assert
            Assert.Equal(0, dialogueCount);
        }

        [Fact]
        public void GenerateLevelName_WithValidInputs_ReturnsGeneratedName()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var level = CreateTestLevel();
            var theme = CreateTestTheme();

            // Act
            var result = _aiContentService.GenerateLevelName(level, theme);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public void GenerateLevelName_WithUnavailableAI_ReturnsNull()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(false);
            var level = CreateTestLevel();
            var theme = CreateTestTheme();

            // Act
            var result = _aiContentService.GenerateLevelName(level, theme);

            // Assert
            Assert.Null(result);
        }

        [Fact]
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
            Assert.False(report.Success);
            Assert.NotNull(report.ErrorMessage);
            Assert.True(_logger.HasLogLevel(LogLevel.Error));
        }

        [Fact]
        public void EnhancementReport_CalculatesCorrectRatios()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var level = CreateTestLevelWithEntities();
            var theme = CreateTestTheme();

            // Act
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert
            Assert.True(report.EnhancementRatio >= 0.0 && report.EnhancementRatio <= 1.0);
            Assert.Equal((double)report.EnhancedEntities / report.TotalEntities, report.EnhancementRatio);
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
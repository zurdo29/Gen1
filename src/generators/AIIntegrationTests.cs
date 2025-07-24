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
    public class AIIntegrationTests
    {
        private TestLogger _logger;
        private AIServiceConfig _config;
        private MockAIContentGenerator _mockAIGenerator;
        private AIContentService _aiContentService;
        private AIEnhancedLevelAssembler _enhancedAssembler;
        private LevelAssembler _baseLevelAssembler;

        [TestInitialize]
        public void Setup()
        {
            _logger = new TestLogger();
            _config = new AIServiceConfig { IsEnabled = true, ApiEndpoint = "https://test.api.com" };
            _mockAIGenerator = new MockAIContentGenerator();
            _aiContentService = new AIContentService(_mockAIGenerator, _logger);
            _baseLevelAssembler = new LevelAssembler();
            _enhancedAssembler = new AIEnhancedLevelAssembler(_baseLevelAssembler, _mockAIGenerator, _logger);
        }

        [TestMethod]
        public void FullAIWorkflow_WithAvailableAI_EnhancesAllContent()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var terrain = CreateTestTerrain();
            var entities = CreateTestEntities();
            var config = CreateTestGenerationConfig();

            // Act - Assemble level with AI enhancement
            var level = _enhancedAssembler.AssembleLevel(terrain, entities, config);

            // Assert - Verify level structure
            Assert.IsNotNull(level);
            Assert.IsNotNull(level.Terrain);
            Assert.IsNotNull(level.Entities);
            Assert.IsTrue(level.Entities.Count > 0);

            // Verify AI enhancements
            var entitiesWithDescriptions = level.Entities.Count(e => e.HasAIContent());
            Assert.IsTrue(entitiesWithDescriptions > 0, "Some entities should have AI-generated content");

            // Verify metadata contains AI information
            Assert.IsTrue(level.Metadata.ContainsKey("AIEnhancedEntities"));
            Assert.IsTrue(level.Metadata.ContainsKey("AIEnhancementTimestamp"));
        }

        [TestMethod]
        public void FullAIWorkflow_WithUnavailableAI_UsesFallbackContent()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(false);
            var terrain = CreateTestTerrain();
            var entities = CreateTestEntities();
            var config = CreateTestGenerationConfig();

            // Act
            var level = _enhancedAssembler.AssembleLevel(terrain, entities, config);

            // Assert - Level should still be created
            Assert.IsNotNull(level);
            Assert.IsNotNull(level.Terrain);
            Assert.IsNotNull(level.Entities);

            // Verify no AI enhancements were applied
            var entitiesWithAIContent = level.Entities.Count(e => e.HasAIContent());
            Assert.AreEqual(0, entitiesWithAIContent, "No entities should have AI content when AI is unavailable");
        }

        [TestMethod]
        public void AIContentService_EndToEndWorkflow_ProducesConsistentResults()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var level = CreateCompleteTestLevel();
            var theme = CreateTestTheme();

            // Act - Run full enhancement
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert - Verify report
            Assert.IsTrue(report.Success);
            Assert.IsTrue(report.IsAIAvailable);
            Assert.IsTrue(report.LevelNameGenerated);
            Assert.IsTrue(report.EnhancedEntities > 0);
            Assert.IsTrue(report.Duration.TotalMilliseconds >= 0);

            // Verify level was actually enhanced
            Assert.IsTrue(level.Metadata.ContainsKey("OriginalName"));
            Assert.IsTrue(level.Metadata.ContainsKey("AIGeneratedName"));

            // Verify entities were enhanced
            var enhancedEntities = level.Entities.Where(e => e.HasAIContent()).ToList();
            Assert.AreEqual(report.EnhancedEntities, enhancedEntities.Count);

            // Verify interactive entities have dialogue
            var interactiveEntities = level.Entities.Where(e => 
                e.Type == EntityType.Enemy || 
                e.Type == EntityType.PowerUp || 
                e.Type == EntityType.Checkpoint).ToList();
            
            var entitiesWithDialogue = interactiveEntities.Where(e => e.GetAIDialogue() != null).ToList();
            Assert.AreEqual(report.EntitiesWithDialogue, entitiesWithDialogue.Count);
        }

        [TestMethod]
        public void AIFactory_CreatesWorkingInstances()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://test.api.com",
                MaxTokens = 100,
                Temperature = 0.5
            };

            // Act
            var aiGenerator = AIContentGeneratorFactory.Create(config, _logger);

            // Assert
            Assert.IsNotNull(aiGenerator);
            Assert.IsTrue(aiGenerator.IsAvailable());
        }

        [TestMethod]
        public void AIFactory_WithDefaultConfig_CreatesDisabledInstance()
        {
            // Act
            var aiGenerator = AIContentGeneratorFactory.CreateDefault(_logger);

            // Assert
            Assert.IsNotNull(aiGenerator);
            Assert.IsFalse(aiGenerator.IsAvailable());
        }

        [TestMethod]
        public void EntityExtensions_WorkCorrectlyWithAIContent()
        {
            // Arrange
            var entity = new EnemyEntity();
            var description = "AI-generated enemy description";
            var dialogue = new[] { "Prepare for battle!", "You shall not pass!", "This is my domain!" };

            // Act
            entity.SetAIDescription(description);
            entity.SetAIDialogue(dialogue);

            // Assert
            Assert.IsTrue(entity.HasAIContent());
            Assert.AreEqual(description, entity.GetAIDescription());
            CollectionAssert.AreEqual(dialogue, entity.GetAIDialogue());
            Assert.AreEqual(3, entity.GetDialogueLineCount());

            var randomLine = entity.GetRandomDialogueLine();
            Assert.IsNotNull(randomLine);
            CollectionAssert.Contains(dialogue, randomLine);

            var summary = entity.GetAIContentSummary();
            Assert.IsTrue(summary.HasDescription);
            Assert.IsTrue(summary.HasDialogue);
            Assert.AreEqual(3, summary.DialogueLineCount);
            Assert.AreEqual(EntityType.Enemy, summary.EntityType);
            Assert.IsTrue(summary.HasAnyContent);
        }

        [TestMethod]
        public void AIDemo_RunsSuccessfully()
        {
            // Arrange
            var demo = new AIContentGenerationDemo(_aiContentService, _logger);
            _mockAIGenerator.SetAvailable(true);

            // Act
            var results = demo.RunDemo();

            // Assert
            Assert.IsTrue(results.Success);
            Assert.IsTrue(results.IsAIAvailable);
            Assert.IsNotNull(results.SampleLevel);
            Assert.IsNotNull(results.SampleTheme);
            Assert.IsTrue(results.Messages.Count > 0);
            Assert.IsTrue(results.Duration.TotalMilliseconds >= 0);
        }

        [TestMethod]
        public void AIDemo_WithUnavailableAI_ShowsFallbackBehavior()
        {
            // Arrange
            var demo = new AIContentGenerationDemo(_aiContentService, _logger);
            _mockAIGenerator.SetAvailable(false);

            // Act
            var results = demo.RunDemo();

            // Assert
            Assert.IsTrue(results.Success);
            Assert.IsFalse(results.IsAIAvailable);
            Assert.IsTrue(results.Messages.Any(m => m.Contains("not available")));
        }

        [TestMethod]
        public void ErrorHandling_AIServiceFailure_HandledGracefully()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            _mockAIGenerator.ShouldThrowException = true;
            var level = CreateCompleteTestLevel();
            var theme = CreateTestTheme();

            // Act
            var report = _aiContentService.EnhanceLevel(level, theme);

            // Assert
            Assert.IsFalse(report.Success);
            Assert.IsNotNull(report.ErrorMessage);
            Assert.IsTrue(_logger.HasLogLevel(LogLevel.Error));
        }

        [TestMethod]
        public void PerformanceTest_AIEnhancement_CompletesInReasonableTime()
        {
            // Arrange
            _mockAIGenerator.SetAvailable(true);
            var level = CreateLargeTestLevel(100); // 100 entities
            var theme = CreateTestTheme();

            // Act
            var startTime = DateTime.UtcNow;
            var report = _aiContentService.EnhanceLevel(level, theme);
            var endTime = DateTime.UtcNow;

            // Assert
            var duration = endTime - startTime;
            Assert.IsTrue(duration.TotalSeconds < 5, "AI enhancement should complete within 5 seconds for 100 entities");
            Assert.IsTrue(report.Success);
            Assert.IsTrue(report.EnhancedEntities > 0);
        }

        private TileMap CreateTestTerrain()
        {
            return new TileMap(20, 20);
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

        private GenerationConfig CreateTestGenerationConfig()
        {
            return new GenerationConfig
            {
                Width = 20,
                Height = 20,
                Seed = 12345,
                GenerationAlgorithm = "test",
                AlgorithmParameters = new Dictionary<string, object>(),
                Entities = new List<EntityConfig>(),
                VisualTheme = new VisualThemeConfig { ThemeName = "Test Theme" },
                Gameplay = new GameplayConfig()
            };
        }

        private Level CreateCompleteTestLevel()
        {
            var terrain = CreateTestTerrain();
            var entities = CreateTestEntities();

            return new Level
            {
                Terrain = terrain,
                Entities = entities,
                Name = "Test Level",
                Metadata = new Dictionary<string, object>()
            };
        }

        private Level CreateLargeTestLevel(int entityCount)
        {
            var terrain = new TileMap(50, 50);
            var entities = new List<Entity>();

            for (int i = 0; i < entityCount; i++)
            {
                var entityType = (EntityType)(i % 4); // Cycle through entity types
                Entity entity = entityType switch
                {
                    EntityType.Enemy => new EnemyEntity(),
                    EntityType.PowerUp => new PowerUpEntity(),
                    EntityType.Item => new ItemEntity(),
                    EntityType.Checkpoint => new CheckpointEntity(),
                    _ => new EnemyEntity()
                };
                entities.Add(entity);
            }

            return new Level
            {
                Terrain = terrain,
                Entities = entities,
                Name = "Large Test Level",
                Metadata = new Dictionary<string, object>()
            };
        }

        private VisualTheme CreateTestTheme()
        {
            return new VisualTheme
            {
                Name = "Integration Test Theme",
                TileSprites = new Dictionary<TileType, string>(),
                EntitySprites = new Dictionary<EntityType, string>(),
                Colors = new ColorPalette()
            };
        }
    }
}
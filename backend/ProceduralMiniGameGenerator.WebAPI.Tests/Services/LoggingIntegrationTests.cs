using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    /// <summary>
    /// Tests for logging integration throughout the generation pipeline
    /// </summary>
    public class LoggingIntegrationTests
    {
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IPluginLoader> _mockPluginLoader;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IConfigurationParser> _mockConfigParser;
        private readonly Mock<ITerrainGenerator> _mockTerrainGenerator;
        private readonly Mock<IEntityPlacer> _mockEntityPlacer;
        private readonly Mock<ILevelAssembler> _mockLevelAssembler;
        private readonly string _testSessionId = "test-session-123";
        
        public LoggingIntegrationTests()
        {
            _mockLoggerService = new Mock<ILoggerService>();
            _mockPluginLoader = new Mock<IPluginLoader>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockConfigParser = new Mock<IConfigurationParser>();
            _mockTerrainGenerator = new Mock<ITerrainGenerator>();
            _mockEntityPlacer = new Mock<IEntityPlacer>();
            _mockLevelAssembler = new Mock<ILevelAssembler>();
        }
        
        [Fact]
        public async Task LoggingGenerationOrchestrator_GenerateLevelAsync_LogsCompletePipeline()
        {
            // Arrange
            var testConfig = CreateTestConfig();
            var testTerrain = CreateTestTerrain();
            var testEntities = CreateTestEntities();
            var testLevel = CreateTestLevel();
            
            SetupMocks(testConfig, testTerrain, testEntities, testLevel);
            
            var orchestrator = new LoggingGenerationOrchestrator(
                _mockLoggerService.Object,
                _mockPluginLoader.Object,
                _mockServiceProvider.Object);
            
            // Act
            var result = await orchestrator.GenerateLevelAsync(testConfig, _testSessionId);
            
            // Assert
            Assert.Equal(testLevel, result);
            
            // Verify pipeline logging
            _mockLoggerService.Verify(x => x.LogAsync(
                LogLevel.Information,
                "Starting complete level generation pipeline",
                It.IsAny<object>()), Times.Once);
            
            _mockLoggerService.Verify(x => x.LogPerformanceAsync(
                "CompleteLevelGeneration",
                It.IsAny<TimeSpan>(),
                It.IsAny<object>()), Times.Once);
            
            _mockLoggerService.Verify(x => x.LogAsync(
                LogLevel.Information,
                "Complete level generation pipeline completed successfully",
                It.IsAny<object>()), Times.Once);
        }
        
        private void SetupMocks(GenerationConfig config, TileMap terrain, List<Entity> entities, Level level)
        {
            // Setup configuration parser
            var validationErrors = new List<string>();
            _mockConfigParser.Setup(x => x.ValidateConfig(config, out validationErrors))
                .Returns(true);
            
            // Setup terrain generator
            _mockTerrainGenerator.Setup(x => x.GetAlgorithmName())
                .Returns(config.GenerationAlgorithm);
            _mockTerrainGenerator.Setup(x => x.ValidateParameters(It.IsAny<Dictionary<string, object>>()))
                .Returns(new List<string>());
            _mockTerrainGenerator.Setup(x => x.GenerateTerrain(config, config.Seed))
                .Returns(terrain);
            
            // Setup entity placer
            _mockEntityPlacer.Setup(x => x.PlaceEntities(terrain, config, config.Seed))
                .Returns(entities);
            
            // Setup level assembler
            _mockLevelAssembler.Setup(x => x.AssembleLevel(terrain, entities, config))
                .Returns(level);
            
            // Setup plugin loader
            _mockPluginLoader.Setup(x => x.LoadPluginsAsync<ITerrainGenerator>())
                .ReturnsAsync(new List<ITerrainGenerator> { _mockTerrainGenerator.Object });
            
            // Setup service provider
            _mockServiceProvider.Setup(x => x.GetRequiredService<IConfigurationParser>())
                .Returns(_mockConfigParser.Object);
            _mockServiceProvider.Setup(x => x.GetRequiredService<IEntityPlacer>())
                .Returns(_mockEntityPlacer.Object);
            _mockServiceProvider.Setup(x => x.GetRequiredService<ILevelAssembler>())
                .Returns(_mockLevelAssembler.Object);
        }
        
        private GenerationConfig CreateTestConfig()
        {
            return new GenerationConfig
            {
                Width = 50,
                Height = 50,
                Seed = 12345,
                GenerationAlgorithm = "test",
                AlgorithmParameters = new Dictionary<string, object>(),
                TerrainTypes = new List<string> { "ground", "wall" },
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = 3,
                        MinDistance = 2.0f,
                        MaxDistanceFromPlayer = 50.0f,
                        PlacementStrategy = "random"
                    }
                }
            };
        }
        
        private TileMap CreateTestTerrain()
        {
            var terrain = new TileMap(10, 10);
            // Initialize with some test data
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    terrain.SetTile(x, y, TileType.Ground);
                }
            }
            return terrain;
        }
        
        private List<Entity> CreateTestEntities()
        {
            return new List<Entity>
            {
                new Entity
                {
                    Type = EntityType.Player,
                    Position = new System.Numerics.Vector2(5, 5)
                },
                new Entity
                {
                    Type = EntityType.Enemy,
                    Position = new System.Numerics.Vector2(8, 8)
                }
            };
        }
        
        private Level CreateTestLevel()
        {
            return new Level
            {
                Name = "Test Level",
                Terrain = CreateTestTerrain(),
                Entities = CreateTestEntities(),
                Metadata = new Dictionary<string, object>()
            };
        }
        

    }
}
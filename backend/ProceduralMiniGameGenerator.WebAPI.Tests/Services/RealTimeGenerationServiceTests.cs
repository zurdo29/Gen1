using Microsoft.AspNetCore.SignalR;
using Moq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Hubs;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    public class RealTimeGenerationServiceTests
    {
        private readonly Mock<IHubContext<GenerationHub, IGenerationHubClient>> _mockHubContext;
        private readonly Mock<IGenerationService> _mockGenerationService;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IGenerationHubClient> _mockHubClient;
        private readonly Mock<IGroupManager> _mockGroupManager;
        private readonly RealTimeGenerationService _service;

        public RealTimeGenerationServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<GenerationHub, IGenerationHubClient>>();
            _mockGenerationService = new Mock<IGenerationService>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockHubClient = new Mock<IGenerationHubClient>();
            _mockGroupManager = new Mock<IGroupManager>();

            _mockHubContext.Setup(x => x.Clients.Group(It.IsAny<string>()))
                          .Returns(_mockHubClient.Object);
            _mockHubContext.Setup(x => x.Groups)
                          .Returns(_mockGroupManager.Object);

            _service = new RealTimeGenerationService(
                _mockHubContext.Object,
                _mockGenerationService.Object,
                _mockLoggerService.Object);
        }

        [Fact]
        public async Task RequestDebouncedPreview_ValidConfig_StartsGeneration()
        {
            // Arrange
            var sessionId = "test-session-123";
            var config = CreateValidConfig();
            var debounceMs = 100; // Short debounce for testing

            var mockLevel = CreateMockLevel();
            var mockRequest = new WebGenerationRequest
            {
                Config = config,
                SessionId = sessionId,
                IncludePreview = true,
                UseBackgroundProcessing = false
            };

            _mockGenerationService
                .Setup(x => x.ValidateConfiguration(config))
                .Returns(ValidationResult.Success());

            _mockGenerationService
                .Setup(x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()))
                .ReturnsAsync(mockLevel);

            // Act
            await _service.RequestDebouncedPreview(sessionId, config, debounceMs);

            // Wait for debounce and generation to complete
            await Task.Delay(debounceMs + 500);

            // Assert
            _mockHubClient.Verify(
                x => x.GenerationProgress(sessionId, 0, "Preview generation pending..."),
                Times.Once);

            _mockHubClient.Verify(
                x => x.PreviewGenerated(sessionId, mockLevel),
                Times.Once);

            _mockGenerationService.Verify(
                x => x.ValidateConfiguration(config),
                Times.Once);

            _mockGenerationService.Verify(
                x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()),
                Times.Once);
        }

        [Fact]
        public async Task RequestDebouncedPreview_InvalidConfig_SendsValidationError()
        {
            // Arrange
            var sessionId = "test-session-123";
            var config = CreateValidConfig();
            var debounceMs = 100;

            var validationResult = ValidationResult.Failure(new List<string> { "Invalid width" });

            _mockGenerationService
                .Setup(x => x.ValidateConfiguration(config))
                .Returns(validationResult);

            // Act
            await _service.RequestDebouncedPreview(sessionId, config, debounceMs);

            // Wait for debounce and validation to complete
            await Task.Delay(debounceMs + 200);

            // Assert
            _mockHubClient.Verify(
                x => x.ValidationResult(sessionId, validationResult),
                Times.Once);

            _mockGenerationService.Verify(
                x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()),
                Times.Never);
        }

        [Fact]
        public async Task RequestDebouncedPreview_MultipleRequests_CancelsPrevious()
        {
            // Arrange
            var sessionId = "test-session-123";
            var config1 = CreateValidConfig();
            var config2 = CreateValidConfig();
            config2.Width = 30; // Different config
            var debounceMs = 200;

            _mockGenerationService
                .Setup(x => x.ValidateConfiguration(It.IsAny<GenerationConfig>()))
                .Returns(ValidationResult.Success());

            _mockGenerationService
                .Setup(x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()))
                .ReturnsAsync(CreateMockLevel());

            // Act
            var task1 = _service.RequestDebouncedPreview(sessionId, config1, debounceMs);
            await Task.Delay(50); // Small delay
            var task2 = _service.RequestDebouncedPreview(sessionId, config2, debounceMs);

            await Task.WhenAll(task1, task2);
            await Task.Delay(debounceMs + 300); // Wait for completion

            // Assert
            // Should only generate once (for the second config)
            _mockGenerationService.Verify(
                x => x.GenerateLevelAsync(It.Is<WebGenerationRequest>(r => r.Config.Width == 30)),
                Times.Once);

            _mockGenerationService.Verify(
                x => x.GenerateLevelAsync(It.Is<WebGenerationRequest>(r => r.Config.Width == 20)),
                Times.Never);
        }

        [Fact]
        public async Task CancelPendingPreview_ActiveGeneration_CancelsSuccessfully()
        {
            // Arrange
            var sessionId = "test-session-123";
            var config = CreateValidConfig();
            var debounceMs = 500; // Longer debounce to allow cancellation

            _mockGenerationService
                .Setup(x => x.ValidateConfiguration(config))
                .Returns(ValidationResult.Success());

            // Act
            var generationTask = _service.RequestDebouncedPreview(sessionId, config, debounceMs);
            await Task.Delay(100); // Let it start
            await _service.CancelPendingPreview(sessionId);
            
            // Wait for the generation task to complete (should be cancelled)
            await generationTask;

            // Assert
            _mockGenerationService.Verify(
                x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPreviewStatus_NewSession_ReturnsIdleStatus()
        {
            // Arrange
            var sessionId = "new-session-123";

            // Act
            var status = await _service.GetPreviewStatus(sessionId);

            // Assert
            Assert.Equal(sessionId, status.SessionId);
            Assert.Equal("idle", status.Status);
            Assert.Equal(0, status.Progress);
            Assert.Null(status.LastUpdated);
        }

        [Fact]
        public async Task GetPreviewStatus_ActiveSession_ReturnsCurrentStatus()
        {
            // Arrange
            var sessionId = "active-session-123";
            var config = CreateValidConfig();

            _mockGenerationService
                .Setup(x => x.ValidateConfiguration(config))
                .Returns(ValidationResult.Success());

            _mockGenerationService
                .Setup(x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()))
                .Returns(async () =>
                {
                    await Task.Delay(1000); // Simulate long generation
                    return CreateMockLevel();
                });

            // Start generation
            var generationTask = _service.RequestDebouncedPreview(sessionId, config, 100);
            await Task.Delay(200); // Let it start generating

            // Act
            var status = await _service.GetPreviewStatus(sessionId);

            // Assert
            Assert.Equal(sessionId, status.SessionId);
            Assert.Equal("generating", status.Status);
            Assert.True(status.Progress >= 0);
            Assert.NotNull(status.LastUpdated);

            // Cleanup
            await _service.CancelPendingPreview(sessionId);
        }

        [Fact]
        public async Task RequestDebouncedPreview_GenerationThrowsException_SendsError()
        {
            // Arrange
            var sessionId = "test-session-123";
            var config = CreateValidConfig();
            var debounceMs = 100;
            var errorMessage = "Generation failed";

            _mockGenerationService
                .Setup(x => x.ValidateConfiguration(config))
                .Returns(ValidationResult.Success());

            _mockGenerationService
                .Setup(x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            await _service.RequestDebouncedPreview(sessionId, config, debounceMs);

            // Wait for debounce and generation to complete
            await Task.Delay(debounceMs + 300);

            // Assert
            _mockHubClient.Verify(
                x => x.GenerationError(sessionId, errorMessage),
                Times.Once);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task RequestDebouncedPreview_DifferentDebounceValues_RespectsDebounce(int debounceMs)
        {
            // Arrange
            var sessionId = "test-session-123";
            var config = CreateValidConfig();

            _mockGenerationService
                .Setup(x => x.ValidateConfiguration(config))
                .Returns(ValidationResult.Success());

            _mockGenerationService
                .Setup(x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()))
                .ReturnsAsync(CreateMockLevel());

            var startTime = DateTime.UtcNow;

            // Act
            await _service.RequestDebouncedPreview(sessionId, config, debounceMs);

            // Wait for completion
            await Task.Delay(debounceMs + 300);

            // Assert
            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Assert.True(elapsedMs >= debounceMs, $"Expected at least {debounceMs}ms delay, but got {elapsedMs}ms");

            _mockGenerationService.Verify(
                x => x.GenerateLevelAsync(It.IsAny<WebGenerationRequest>()),
                Times.Once);
        }

        private static GenerationConfig CreateValidConfig()
        {
            return new GenerationConfig
            {
                Width = 20,
                Height = 20,
                Seed = 12345,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>(),
                TerrainTypes = new List<string> { "grass", "stone", "water" },
                Entities = new List<EntityConfig>(),
                VisualTheme = new VisualThemeConfig
                {
                    ThemeName = "default",
                    ColorPalette = new Dictionary<string, string>(),
                    TileSprites = new Dictionary<string, string>(),
                    EntitySprites = new Dictionary<string, string>(),
                    EffectSettings = new Dictionary<string, object>()
                },
                Gameplay = new GameplayConfig
                {
                    PlayerSpeed = 5.0f,
                    PlayerHealth = 100,
                    Difficulty = "normal",
                    TimeLimit = 300,
                    VictoryConditions = new List<string> { "reach_exit" },
                    Mechanics = new Dictionary<string, object>()
                }
            };
        }

        private static Level CreateMockLevel()
        {
            return new Level
            {
                Name = "Test Level",
                Width = 20,
                Height = 20,
                Terrain = new TileMap
                {
                    Width = 20,
                    Height = 20,
                    Tiles = new Tile[20, 20]
                },
                Entities = new List<Entity>(),
                Metadata = new LevelMetadata
                {
                    GeneratedAt = DateTime.UtcNow,
                    GenerationTime = TimeSpan.FromSeconds(1),
                    Version = "1.0"
                }
            };
        }
    }
}
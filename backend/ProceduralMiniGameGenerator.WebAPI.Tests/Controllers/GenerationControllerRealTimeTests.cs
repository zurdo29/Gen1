using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Controllers;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Controllers
{
    public class GenerationControllerRealTimeTests
    {
        private readonly Mock<IGenerationService> _mockGenerationService;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IRealTimeGenerationService> _mockRealTimeService;
        private readonly GenerationController _controller;

        public GenerationControllerRealTimeTests()
        {
            _mockGenerationService = new Mock<IGenerationService>();
            _mockLoggerService = new Mock<ILoggerService>();
            _mockRealTimeService = new Mock<IRealTimeGenerationService>();
            
            _controller = new GenerationController(
                _mockGenerationService.Object,
                _mockLoggerService.Object,
                _mockRealTimeService.Object);
        }

        [Fact]
        public async Task RequestPreview_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new PreviewRequest
            {
                SessionId = "test-session-123",
                Config = CreateValidConfig(),
                DebounceMs = 500
            };

            _mockRealTimeService
                .Setup(x => x.RequestDebouncedPreview(request.SessionId, request.Config, request.DebounceMs))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RequestPreview(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PreviewRequestResponse>(okResult.Value);
            
            Assert.Equal(request.SessionId, response.SessionId);
            Assert.Equal("requested", response.Status);
            Assert.Contains("Preview generation requested", response.Message);

            _mockRealTimeService.Verify(
                x => x.RequestDebouncedPreview(request.SessionId, request.Config, request.DebounceMs),
                Times.Once);
        }

        [Fact]
        public async Task RequestPreview_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var request = new PreviewRequest
            {
                SessionId = "",
                Config = CreateValidConfig(),
                DebounceMs = 500
            };

            // Act
            var result = await _controller.RequestPreview(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var error = badRequestResult.Value;
            Assert.NotNull(error);

            _mockRealTimeService.Verify(
                x => x.RequestDebouncedPreview(It.IsAny<string>(), It.IsAny<GenerationConfig>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task RequestPreview_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new PreviewRequest
            {
                SessionId = "test-session-123",
                Config = CreateValidConfig(),
                DebounceMs = 500
            };

            _mockRealTimeService
                .Setup(x => x.RequestDebouncedPreview(It.IsAny<string>(), It.IsAny<GenerationConfig>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.RequestPreview(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetPreviewStatus_ExistingSession_ReturnsStatus()
        {
            // Arrange
            var sessionId = "test-session-123";
            var expectedStatus = new PreviewStatus
            {
                SessionId = sessionId,
                Status = "generating",
                Progress = 50,
                Message = "Generating terrain...",
                LastUpdated = DateTime.UtcNow
            };

            _mockRealTimeService
                .Setup(x => x.GetPreviewStatus(sessionId))
                .ReturnsAsync(expectedStatus);

            // Act
            var result = await _controller.GetPreviewStatus(sessionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var status = Assert.IsType<PreviewStatus>(okResult.Value);
            
            Assert.Equal(expectedStatus.SessionId, status.SessionId);
            Assert.Equal(expectedStatus.Status, status.Status);
            Assert.Equal(expectedStatus.Progress, status.Progress);
            Assert.Equal(expectedStatus.Message, status.Message);
        }

        [Fact]
        public async Task GetPreviewStatus_NonExistentSession_ReturnsNotFound()
        {
            // Arrange
            var sessionId = "non-existent-session";
            var idleStatus = new PreviewStatus
            {
                SessionId = sessionId,
                Status = "idle",
                LastUpdated = null
            };

            _mockRealTimeService
                .Setup(x => x.GetPreviewStatus(sessionId))
                .ReturnsAsync(idleStatus);

            // Act
            var result = await _controller.GetPreviewStatus(sessionId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task CancelPreview_ValidSessionId_ReturnsOk()
        {
            // Arrange
            var sessionId = "test-session-123";

            _mockRealTimeService
                .Setup(x => x.CancelPendingPreview(sessionId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CancelPreview(sessionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            _mockRealTimeService.Verify(
                x => x.CancelPendingPreview(sessionId),
                Times.Once);
        }

        [Fact]
        public async Task CancelPreview_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var sessionId = "test-session-123";

            _mockRealTimeService
                .Setup(x => x.CancelPendingPreview(sessionId))
                .ThrowsAsync(new Exception("Cancel failed"));

            // Act
            var result = await _controller.CancelPreview(sessionId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(2000)]
        public async Task RequestPreview_DifferentDebounceValues_PassesCorrectValue(int debounceMs)
        {
            // Arrange
            var request = new PreviewRequest
            {
                SessionId = "test-session-123",
                Config = CreateValidConfig(),
                DebounceMs = debounceMs
            };

            _mockRealTimeService
                .Setup(x => x.RequestDebouncedPreview(request.SessionId, request.Config, debounceMs))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RequestPreview(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _mockRealTimeService.Verify(
                x => x.RequestDebouncedPreview(request.SessionId, request.Config, debounceMs),
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
    }
}
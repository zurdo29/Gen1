using Xunit;
using Moq;
using ProceduralMiniGameGenerator.WebAPI.Services;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    public class SocialPreviewServiceTests
    {
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly SocialPreviewService _socialPreviewService;

        public SocialPreviewServiceTests()
        {
            _mockLogger = new Mock<ILoggerService>();
            _socialPreviewService = new SocialPreviewService(_mockLogger.Object);
        }

        [Fact]
        public async Task GeneratePreviewImageAsync_ValidConfig_ReturnsDataUrl()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = 50,
                Height = 50,
                GenerationAlgorithm = "perlin",
                Seed = 12345
            };

            // Act
            var result = await _socialPreviewService.GeneratePreviewImageAsync(config);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("data:image/png;base64,", result);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.LogAsync(
                    Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generating social media preview image",
                    It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_ValidConfig_ReturnsDataUrl()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = 25,
                Height = 25,
                GenerationAlgorithm = "maze",
                Seed = 54321
            };

            // Act
            var result = await _socialPreviewService.GenerateThumbnailAsync(config);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("data:image/png;base64,", result);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.LogAsync(
                    Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generating thumbnail preview",
                    It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GeneratePreviewImageAsync_CustomDimensions_ReturnsDataUrl()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = 100,
                Height = 100,
                GenerationAlgorithm = "cellular",
                Seed = 98765
            };

            // Act
            var result = await _socialPreviewService.GeneratePreviewImageAsync(config, 800, 400);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("data:image/png;base64,", result);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_CustomSize_ReturnsDataUrl()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = 75,
                Height = 75,
                GenerationAlgorithm = "perlin",
                Seed = 11111
            };

            // Act
            var result = await _socialPreviewService.GenerateThumbnailAsync(config, 150);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("data:image/png;base64,", result);
        }

        [Fact]
        public async Task GeneratePreviewImageAsync_NullConfig_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _socialPreviewService.GeneratePreviewImageAsync(null));
        }

        [Fact]
        public async Task GenerateThumbnailAsync_NullConfig_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _socialPreviewService.GenerateThumbnailAsync(null));
        }
    }
}
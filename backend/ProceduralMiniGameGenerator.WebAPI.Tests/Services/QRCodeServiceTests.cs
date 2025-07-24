using Xunit;
using Moq;
using ProceduralMiniGameGenerator.WebAPI.Services;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    public class QRCodeServiceTests
    {
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly QRCodeService _qrCodeService;

        public QRCodeServiceTests()
        {
            _mockLogger = new Mock<ILoggerService>();
            _qrCodeService = new QRCodeService(_mockLogger.Object);
        }

        [Fact]
        public async Task GenerateQRCodeDataUrlAsync_ValidUrl_ReturnsDataUrl()
        {
            // Arrange
            var testUrl = "https://example.com/share/abc123";

            // Act
            var result = await _qrCodeService.GenerateQRCodeDataUrlAsync(testUrl);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("data:image/png;base64,", result);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.LogAsync(
                    Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generating QR code data URL",
                    It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateQRCodeBytesAsync_ValidUrl_ReturnsByteArray()
        {
            // Arrange
            var testUrl = "https://example.com/share/abc123";

            // Act
            var result = await _qrCodeService.GenerateQRCodeBytesAsync(testUrl);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.LogAsync(
                    Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generating QR code bytes",
                    It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateQRCodeDataUrlAsync_CustomSize_ReturnsDataUrl()
        {
            // Arrange
            var testUrl = "https://example.com/share/abc123";
            var customSize = 400;

            // Act
            var result = await _qrCodeService.GenerateQRCodeDataUrlAsync(testUrl, customSize);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("data:image/png;base64,", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task GenerateQRCodeDataUrlAsync_InvalidInput_ThrowsException(string invalidUrl)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _qrCodeService.GenerateQRCodeDataUrlAsync(invalidUrl));
        }
    }
}
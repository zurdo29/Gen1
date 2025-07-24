using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProceduralMiniGameGenerator.WebAPI.Services;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    /// <summary>
    /// Unit tests for LoggerService functionality
    /// </summary>
    public class LoggerServiceTests
    {
        private readonly Mock<ILogger<LoggerService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly LoggerService _loggerService;
        
        public LoggerServiceTests()
        {
            _mockLogger = new Mock<ILogger<LoggerService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup default configuration values
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x["Default"]).Returns("2000");
            mockSection.Setup(x => x["Generation"]).Returns("5000");
            _mockConfiguration.Setup(x => x.GetSection("Logging:PerformanceThresholds")).Returns(mockSection.Object);
            
            _loggerService = new LoggerService(_mockLogger.Object, _mockConfiguration.Object);
        }
        
        [Fact]
        public async Task LogAsync_WithValidParameters_LogsMessage()
        {
            // Arrange
            var message = "Test message";
            var context = new { TestProperty = "TestValue" };
            
            // Act
            await _loggerService.LogAsync(LogLevel.Information, message, context);
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task LogGenerationAsync_WithValidParameters_LogsGenerationStep()
        {
            // Arrange
            var configId = "test-config-123";
            var step = "TerrainGeneration";
            var duration = TimeSpan.FromMilliseconds(1500);
            var metadata = new { TerrainType = "Perlin" };
            
            // Act
            await _loggerService.LogGenerationAsync(configId, step, duration, metadata);
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(step) && v.ToString()!.Contains("1500")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task LogErrorAsync_WithException_LogsErrorWithContext()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");
            var context = "Test context";
            var additionalData = new { UserId = "user123" };
            
            // Act
            await _loggerService.LogErrorAsync(exception, context, additionalData);
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task LogPerformanceAsync_WithSlowOperation_LogsWarning()
        {
            // Arrange
            var operation = "SlowGeneration";
            var duration = TimeSpan.FromMilliseconds(6000); // Above threshold
            var metrics = new { MemoryUsed = "500MB" };
            
            // Setup configuration to return lower threshold
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x["Generation"]).Returns("5000");
            _mockConfiguration.Setup(x => x.GetSection("Logging:PerformanceThresholds")).Returns(mockSection.Object);
            
            // Act
            await _loggerService.LogPerformanceAsync(operation, duration, metrics);
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(operation) && v.ToString()!.Contains("6000")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task LogRequestAsync_WithSuccessfulRequest_LogsInformation()
        {
            // Arrange
            var requestId = "req-123";
            var method = "GET";
            var path = "/api/generation";
            var statusCode = 200;
            var duration = TimeSpan.FromMilliseconds(150);
            
            // Act
            await _loggerService.LogRequestAsync(requestId, method, path, statusCode, duration);
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(method) && v.ToString()!.Contains(path) && v.ToString()!.Contains("200")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task LogRequestAsync_WithErrorRequest_LogsWarning()
        {
            // Arrange
            var requestId = "req-456";
            var method = "POST";
            var path = "/api/generation";
            var statusCode = 400;
            var duration = TimeSpan.FromMilliseconds(50);
            
            // Act
            await _loggerService.LogRequestAsync(requestId, method, path, statusCode, duration);
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(method) && v.ToString()!.Contains(path) && v.ToString()!.Contains("400")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public void CreateScoped_WithScopeAndContext_ReturnsNewInstance()
        {
            // Arrange
            var scope = "TestScope";
            var context = new { ScopeId = "scope123" };
            
            // Act
            var scopedLogger = _loggerService.CreateScoped(scope, context);
            
            // Assert
            Assert.NotNull(scopedLogger);
            Assert.NotSame(_loggerService, scopedLogger);
            Assert.IsType<LoggerService>(scopedLogger);
        }
        
        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LoggerService(null!, _mockConfiguration.Object));
        }
        
        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LoggerService(_mockLogger.Object, null!));
        }
    }
}
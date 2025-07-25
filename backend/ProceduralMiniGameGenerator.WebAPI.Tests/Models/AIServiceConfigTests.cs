using Xunit;

namespace ProceduralMiniGameGenerator.Models.Tests
{
    public class AIServiceConfigTests
    {
        [Fact]
        public void DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var config = new AIServiceConfig();

            // Assert
            Assert.False(config.IsEnabled);
            Assert.Equal(string.Empty, config.ApiEndpoint);
            Assert.Equal(string.Empty, config.ApiKey);
            Assert.Equal(150, config.MaxTokens);
            Assert.Equal(0.7, config.Temperature);
            Assert.Equal(30, config.TimeoutSeconds);
            Assert.Equal(2, config.RetryAttempts);
        }

        [Fact]
        public void IsValid_WithDisabledConfig_ReturnsTrue()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = false
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_WithValidEnabledConfig_ReturnsTrue()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 0.5,
                TimeoutSeconds = 30,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_WithEmptyApiEndpoint_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "",
                MaxTokens = 100,
                Temperature = 0.5,
                TimeoutSeconds = 30,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithNullApiEndpoint_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = null,
                MaxTokens = 100,
                Temperature = 0.5,
                TimeoutSeconds = 30,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithZeroMaxTokens_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 0,
                Temperature = 0.5,
                TimeoutSeconds = 30,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithNegativeMaxTokens_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = -1,
                Temperature = 0.5,
                TimeoutSeconds = 30,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithTemperatureBelowZero_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = -0.1,
                TimeoutSeconds = 30,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithTemperatureAboveOne_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 1.1,
                TimeoutSeconds = 30,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithZeroTimeoutSeconds_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 0.5,
                TimeoutSeconds = 0,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithNegativeTimeoutSeconds_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 0.5,
                TimeoutSeconds = -1,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithNegativeRetryAttempts_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 0.5,
                TimeoutSeconds = 30,
                RetryAttempts = -1
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_WithZeroRetryAttempts_ReturnsTrue()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 0.5,
                TimeoutSeconds = 30,
                RetryAttempts = 0
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_WithBoundaryTemperatureValues_ReturnsTrue()
        {
            // Arrange & Act & Assert
            var config1 = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 0.0,
                TimeoutSeconds = 30,
                RetryAttempts = 0
            };
            Assert.True(config1.IsValid());

            var config2 = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 1.0,
                TimeoutSeconds = 30,
                RetryAttempts = 0
            };
            Assert.True(config2.IsValid());
        }

        [Fact]
        public void IsValid_WithWhitespaceApiEndpoint_ReturnsFalse()
        {
            // Arrange
            var config = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "   ",
                MaxTokens = 100,
                Temperature = 0.5,
                TimeoutSeconds = 30,
                RetryAttempts = 3
            };

            // Act
            var result = config.IsValid();

            // Assert
            Assert.False(result);
        }
    }
}
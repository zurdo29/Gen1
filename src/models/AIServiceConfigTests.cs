using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ProceduralMiniGameGenerator.Models.Tests
{
    [TestClass]
    public class AIServiceConfigTests
    {
        [TestMethod]
        public void DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var config = new AIServiceConfig();

            // Assert
            Assert.IsFalse(config.IsEnabled);
            Assert.AreEqual(string.Empty, config.ApiEndpoint);
            Assert.AreEqual(string.Empty, config.ApiKey);
            Assert.AreEqual(150, config.MaxTokens);
            Assert.AreEqual(0.7, config.Temperature);
            Assert.AreEqual(30, config.TimeoutSeconds);
            Assert.AreEqual(2, config.RetryAttempts);
        }

        [TestMethod]
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
            Assert.IsTrue(result);
        }

        [TestMethod]
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
            Assert.IsTrue(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }

        [TestMethod]
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
            Assert.IsTrue(result);
        }

        [TestMethod]
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
            Assert.IsTrue(config1.IsValid());

            var config2 = new AIServiceConfig
            {
                IsEnabled = true,
                ApiEndpoint = "https://api.example.com",
                MaxTokens = 100,
                Temperature = 1.0,
                TimeoutSeconds = 30,
                RetryAttempts = 0
            };
            Assert.IsTrue(config2.IsValid());
        }

        [TestMethod]
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
            Assert.IsFalse(result);
        }
    }
}
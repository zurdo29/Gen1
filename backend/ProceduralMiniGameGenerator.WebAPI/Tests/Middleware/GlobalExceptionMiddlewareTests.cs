using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ProceduralMiniGameGenerator.WebAPI.Middleware;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.Text.Json;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly GlobalExceptionMiddleware _middleware;

        public GlobalExceptionMiddlewareTests()
        {
            _mockLoggerService = new Mock<ILoggerService>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            
            _middleware = new GlobalExceptionMiddleware(
                async (context) => { /* Next middleware */ },
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );
        }

        [Fact]
        public async Task InvokeAsync_WithValidationException_ReturnsValidationErrorResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var validationErrors = new List<ValidationError>
            {
                new ValidationError { Field = "Width", Message = "Width must be positive", Code = "POSITIVE_VALUE" },
                new ValidationError { Field = "Height", Message = "Height must be positive", Code = "POSITIVE_VALUE" }
            };
            var exception = new ValidationException("Validation failed", validationErrors);

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(400, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("VALIDATION_ERROR", errorResponse!.Code);
            Assert.Equal("Validation Error", errorResponse.Title);
            Assert.NotNull(errorResponse.ValidationErrors);
            Assert.Equal(2, errorResponse.ValidationErrors.Count);
        }

        [Fact]
        public async Task InvokeAsync_WithConfigurationException_ReturnsConfigurationErrorResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new ConfigurationException("Invalid configuration parameters");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(400, context.Response.StatusCode);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("INVALID_CONFIGURATION", errorResponse!.Code);
            Assert.Equal("Configuration Error", errorResponse.Title);
            Assert.Contains("troubleshooting#configuration-errors", errorResponse.TroubleshootingUrl);
        }

        [Fact]
        public async Task InvokeAsync_WithGenerationException_ReturnsGenerationErrorResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new GenerationException("Generation timeout occurred");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(422, context.Response.StatusCode);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("GENERATION_TIMEOUT", errorResponse!.Code);
            Assert.Equal("Generation Error", errorResponse.Title);
            Assert.Contains("troubleshooting#generation-errors", errorResponse.TroubleshootingUrl);
        }

        [Fact]
        public async Task InvokeAsync_WithExportException_ReturnsExportErrorResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new ExportException("Unsupported format specified");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(422, context.Response.StatusCode);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("UNSUPPORTED_FORMAT", errorResponse!.Code);
            Assert.Equal("Export Error", errorResponse.Title);
        }

        [Fact]
        public async Task InvokeAsync_WithTimeoutException_ReturnsTimeoutErrorResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new TimeoutException("Operation timed out");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(408, context.Response.StatusCode);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("OPERATION_TIMEOUT", errorResponse!.Code);
            Assert.Equal("Operation Timeout", errorResponse.Title);
        }

        [Fact]
        public async Task InvokeAsync_WithUnauthorizedAccessException_ReturnsAccessDeniedResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new UnauthorizedAccessException("Access denied");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(403, context.Response.StatusCode);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("ACCESS_DENIED", errorResponse!.Code);
            Assert.Equal("Access Denied", errorResponse.Title);
        }

        [Fact]
        public async Task InvokeAsync_WithOutOfMemoryException_ReturnsMemoryLimitResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new OutOfMemoryException("Insufficient memory");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(422, context.Response.StatusCode);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("MEMORY_LIMIT_EXCEEDED", errorResponse!.Code);
            Assert.Equal("Memory Limit Exceeded", errorResponse.Title);
            Assert.Contains("troubleshooting#memory-errors", errorResponse.TroubleshootingUrl);
        }

        [Fact]
        public async Task InvokeAsync_WithGenericException_ReturnsInternalServerErrorResponse()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("Something went wrong");
            _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("INTERNAL_SERVER_ERROR", errorResponse!.Code);
            Assert.Equal("Internal Server Error", errorResponse.Title);
            Assert.DoesNotContain("Something went wrong", errorResponse.Details); // Should be generic in production
            Assert.Null(errorResponse.StackTrace); // Should not include stack trace in production
        }

        [Fact]
        public async Task InvokeAsync_WithGenericExceptionInDevelopment_IncludesDetailedError()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new InvalidOperationException("Something went wrong");
            _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);

            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal("INTERNAL_SERVER_ERROR", errorResponse!.Code);
            Assert.Contains("Something went wrong", errorResponse.Details); // Should include details in development
            Assert.NotNull(errorResponse.StackTrace); // Should include stack trace in development
        }

        [Fact]
        public async Task InvokeAsync_LogsErrorWithContext()
        {
            // Arrange
            var context = CreateHttpContext();
            context.Request.Path = "/api/test";
            context.Request.Method = "POST";
            context.Items["RequestId"] = "test-request-id";
            
            var exception = new InvalidOperationException("Test error");

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLoggerService.Verify(
                x => x.LogErrorAsync(
                    exception,
                    "Unhandled exception in request pipeline",
                    It.Is<object>(o => o.ToString()!.Contains("test-request-id"))
                ),
                Times.Once
            );
        }

        [Theory]
        [InlineData("Generation timeout occurred", "GENERATION_TIMEOUT")]
        [InlineData("Out of memory during generation", "MEMORY_LIMIT_EXCEEDED")]
        [InlineData("Invalid configuration provided", "INVALID_CONFIGURATION")]
        [InlineData("Level size too large", "LEVEL_TOO_LARGE")]
        [InlineData("Unknown generation error", "GENERATION_FAILED")]
        public void DetermineGenerationErrorCode_ReturnsCorrectCode(string message, string expectedCode)
        {
            // This tests the private method indirectly through exception handling
            var context = CreateHttpContext();
            var exception = new GenerationException(message);

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            var task = middleware.InvokeAsync(context);
            task.Wait();

            // Assert
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal(expectedCode, errorResponse!.Code);
        }

        [Theory]
        [InlineData("Unsupported format specified", "UNSUPPORTED_FORMAT")]
        [InlineData("Export size too large", "EXPORT_SIZE_LIMIT")]
        [InlineData("Export timeout occurred", "EXPORT_TIMEOUT")]
        [InlineData("Unknown export error", "EXPORT_FAILED")]
        public void DetermineExportErrorCode_ReturnsCorrectCode(string message, string expectedCode)
        {
            // This tests the private method indirectly through exception handling
            var context = CreateHttpContext();
            var exception = new ExportException(message);

            var middleware = new GlobalExceptionMiddleware(
                async (context) => throw exception,
                _mockLoggerService.Object,
                _mockEnvironment.Object
            );

            // Act
            var task = middleware.InvokeAsync(context);
            task.Wait();

            // Assert
            var responseBody = GetResponseBody(context);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponseBody>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.Equal(expectedCode, errorResponse!.Code);
        }

        private static HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static string GetResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return reader.ReadToEnd();
        }
    }
}
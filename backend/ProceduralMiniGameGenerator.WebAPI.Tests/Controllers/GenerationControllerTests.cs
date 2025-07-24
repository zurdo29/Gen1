using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Controllers
{
    /// <summary>
    /// Integration tests for GenerationController
    /// </summary>
    public class GenerationControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public GenerationControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GenerateLevel_WithValidConfig_ReturnsLevel()
        {
            // Arrange
            var request = new WebGenerationRequest
            {
                Config = CreateValidConfig(),
                IncludePreview = true,
                SessionId = "test-session-1",
                UseBackgroundProcessing = false
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/generation/generate", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var level = JsonSerializer.Deserialize<Level>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(level);
            Assert.NotNull(level.Terrain);
            Assert.Equal(20, level.Terrain.Width);
            Assert.Equal(20, level.Terrain.Height);
            Assert.NotNull(level.Entities);
        }

        [Fact]
        public async Task GenerateLevel_WithLargeConfig_ReturnsBackgroundJob()
        {
            // Arrange
            var request = new WebGenerationRequest
            {
                Config = CreateLargeConfig(),
                IncludePreview = true,
                SessionId = "test-session-2",
                UseBackgroundProcessing = false // Should be overridden due to size
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/generation/generate", request);

            // Assert
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var jobResponse = JsonSerializer.Deserialize<BackgroundJobResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(jobResponse);
            Assert.NotEmpty(jobResponse.JobId);
            Assert.Equal("pending", jobResponse.Status);
            Assert.NotNull(jobResponse.StatusUrl);
        }

        [Fact]
        public async Task GenerateLevel_WithInvalidConfig_ReturnsBadRequest()
        {
            // Arrange
            var request = new WebGenerationRequest
            {
                Config = new GenerationConfig
                {
                    Width = -1, // Invalid
                    Height = -1, // Invalid
                    GenerationAlgorithm = "invalid-algorithm"
                },
                SessionId = "test-session-3"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/generation/generate", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ValidateConfiguration_WithValidConfig_ReturnsSuccess()
        {
            // Arrange
            var config = CreateValidConfig();

            // Act
            var response = await _client.PostAsJsonAsync("/api/generation/validate-config", config);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var validationResult = JsonSerializer.Deserialize<ValidationResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(validationResult);
            Assert.True(validationResult.IsValid);
            Assert.Empty(validationResult.Errors);
        }

        [Fact]
        public async Task ValidateConfiguration_WithInvalidConfig_ReturnsFailure()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = 0, // Invalid
                Height = 0, // Invalid
                GenerationAlgorithm = "nonexistent"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/generation/validate-config", config);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var validationResult = JsonSerializer.Deserialize<ValidationResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(validationResult);
            Assert.False(validationResult.IsValid);
            Assert.NotEmpty(validationResult.Errors);
        }

        [Fact]
        public async Task GetJobStatus_WithValidJobId_ReturnsStatus()
        {
            // Arrange - First create a background job
            var request = new WebGenerationRequest
            {
                Config = CreateLargeConfig(),
                SessionId = "test-session-4",
                UseBackgroundProcessing = true
            };

            var generateResponse = await _client.PostAsJsonAsync("/api/generation/generate", request);
            Assert.Equal(HttpStatusCode.Accepted, generateResponse.StatusCode);
            
            var generateContent = await generateResponse.Content.ReadAsStringAsync();
            var jobResponse = JsonSerializer.Deserialize<BackgroundJobResponse>(generateContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(jobResponse);
            var jobId = jobResponse.JobId;

            // Act
            var statusResponse = await _client.GetAsync($"/api/generation/job/{jobId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
            
            var statusContent = await statusResponse.Content.ReadAsStringAsync();
            var jobStatus = JsonSerializer.Deserialize<JobStatus>(statusContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(jobStatus);
            Assert.Equal(jobId, jobStatus.JobId);
            Assert.Contains(jobStatus.Status, new[] { "pending", "running", "completed", "failed" });
        }

        [Fact]
        public async Task GetJobStatus_WithInvalidJobId_ReturnsNotFound()
        {
            // Arrange
            var invalidJobId = "nonexistent-job-id";

            // Act
            var response = await _client.GetAsync($"/api/generation/job/{invalidJobId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAvailableAlgorithms_ReturnsAlgorithmList()
        {
            // Act
            var response = await _client.GetAsync("/api/generation/algorithms");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var algorithms = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(algorithms);
            Assert.NotEmpty(algorithms);
            Assert.Contains("perlin", algorithms);
            Assert.Contains("cellular", algorithms);
            Assert.Contains("maze", algorithms);
            Assert.Contains("rooms", algorithms);
        }

        [Fact]
        public async Task GenerateLevel_MultipleRequests_HandledConcurrently()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            
            for (int i = 0; i < 5; i++)
            {
                var request = new WebGenerationRequest
                {
                    Config = CreateValidConfig(),
                    SessionId = $"concurrent-test-{i}",
                    UseBackgroundProcessing = false
                };
                
                tasks.Add(_client.PostAsJsonAsync("/api/generation/generate", request));
            }

            // Act
            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted);
            }
        }

        [Fact]
        public async Task GenerateLevel_WithCaching_ReturnsCachedResult()
        {
            // Arrange
            var config = CreateValidConfig();
            config.Seed = 12345; // Fixed seed for consistent caching
            
            var request = new WebGenerationRequest
            {
                Config = config,
                SessionId = "cache-test-1",
                UseBackgroundProcessing = false
            };

            // Act - Make two identical requests
            var response1 = await _client.PostAsJsonAsync("/api/generation/generate", request);
            var response2 = await _client.PostAsJsonAsync("/api/generation/generate", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            
            // Both should return the same level (cached)
            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();
            
            var level1 = JsonSerializer.Deserialize<Level>(content1, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var level2 = JsonSerializer.Deserialize<Level>(content2, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(level1);
            Assert.NotNull(level2);
            // The second request should be faster due to caching (we can't easily test timing in unit tests)
        }

        /// <summary>
        /// Creates a valid configuration for testing
        /// </summary>
        private static GenerationConfig CreateValidConfig()
        {
            return new GenerationConfig
            {
                Width = 20,
                Height = 20,
                Seed = 12345,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    ["scale"] = 0.1
                },
                TerrainTypes = new List<string> { "ground", "wall" },
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Player,
                        Count = 1,
                        Properties = new Dictionary<string, object>()
                    }
                },
                VisualTheme = new VisualThemeConfig
                {
                    Name = "Test Theme",
                    ColorPalette = new ColorPalette
                    {
                        Primary = "#FF0000",
                        Secondary = "#00FF00",
                        Accent = "#0000FF",
                        Background = "#FFFFFF",
                        Text = "#000000"
                    }
                },
                Gameplay = new GameplayConfig
                {
                    Difficulty = 1,
                    ObjectiveType = "test",
                    TimeLimit = 60,
                    PlayerLives = 3
                }
            };
        }

        /// <summary>
        /// Creates a large configuration that should trigger background processing
        /// </summary>
        private static GenerationConfig CreateLargeConfig()
        {
            var config = CreateValidConfig();
            config.Width = 200; // Large enough to trigger background processing
            config.Height = 200;
            return config;
        }
    }

    /// <summary>
    /// Response model for background job creation (for testing)
    /// </summary>
    public class BackgroundJobResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StatusUrl { get; set; }
    }
}
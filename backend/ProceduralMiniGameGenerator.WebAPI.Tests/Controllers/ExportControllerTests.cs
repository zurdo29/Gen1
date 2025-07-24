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
    /// Integration tests for ExportController
    /// </summary>
    public class ExportControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ExportControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetAvailableFormats_ReturnsFormats()
        {
            // Act
            var response = await _client.GetAsync("/api/export/formats");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var formats = JsonSerializer.Deserialize<List<ExportFormat>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(formats);
            Assert.NotEmpty(formats);
            
            // Check that expected formats are present
            var formatIds = formats.Select(f => f.Id.ToLowerInvariant()).ToList();
            Assert.Contains("json", formatIds);
            Assert.Contains("xml", formatIds);
            Assert.Contains("csv", formatIds);
            Assert.Contains("unity", formatIds);
            
            // Verify format properties
            var jsonFormat = formats.First(f => f.Id.Equals("json", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("JSON", jsonFormat.Name);
            Assert.Equal(".json", jsonFormat.FileExtension);
            Assert.Equal("application/json", jsonFormat.MimeType);
            Assert.True(jsonFormat.SupportsCustomization);
        }

        [Fact]
        public async Task ExportLevel_WithValidJsonRequest_ReturnsFile()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "json",
                FileName = "test_level",
                IncludeGenerationConfig = true,
                IncludeStatistics = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/level", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verify it's valid JSON
            var exportData = JsonSerializer.Deserialize<LevelExportData>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(exportData);
            Assert.Equal("1.0", exportData.FormatVersion);
            Assert.NotNull(exportData.Level);
            Assert.Equal(level.Name, exportData.Level.Name);
        }

        [Fact]
        public async Task ExportLevel_WithValidXmlRequest_ReturnsFile()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "xml",
                FileName = "test_level",
                IncludeGenerationConfig = true,
                IncludeStatistics = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/level", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            Assert.Contains("<?xml", content);
            Assert.Contains("<LevelExportData", content);
        }

        [Fact]
        public async Task ExportLevel_WithValidCsvRequest_ReturnsFile()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "csv",
                FileName = "test_level",
                Options = new Dictionary<string, object>
                {
                    ["includeHeaders"] = "true",
                    ["separator"] = ","
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/level", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            Assert.Contains("X,Y,TileType,TileValue", content);
        }

        [Fact]
        public async Task ExportLevel_WithValidUnityRequest_ReturnsFile()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "unity",
                FileName = "test_level",
                Options = new Dictionary<string, object>
                {
                    ["coordinateSystem"] = "unity",
                    ["scaleFactor"] = "1.0",
                    ["includePrefabData"] = "true"
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/level", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Verify Unity-specific structure
            var unityData = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.True(unityData.TryGetProperty("formatVersion", out var formatVersion));
            Assert.Equal("Unity-1.0", formatVersion.GetString());
            
            Assert.True(unityData.TryGetProperty("level", out var levelData));
            Assert.True(levelData.TryGetProperty("terrain", out _));
            Assert.True(levelData.TryGetProperty("entities", out _));
        }

        [Fact]
        public async Task ExportLevel_WithInvalidFormat_ReturnsBadRequest()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "invalid-format",
                FileName = "test_level"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/level", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ExportLevel_WithNullLevel_ReturnsBadRequest()
        {
            // Arrange
            var request = new ExportRequest
            {
                Level = null!,
                Format = "json",
                FileName = "test_level"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/level", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ExportBatch_WithValidRequest_ReturnsJobId()
        {
            // Arrange
            var levels = new List<Level>
            {
                CreateTestLevel("Level 1"),
                CreateTestLevel("Level 2"),
                CreateTestLevel("Level 3")
            };

            var request = new BatchExportRequest
            {
                Levels = levels,
                Format = "json",
                BaseFileName = "batch_level",
                CreateZipPackage = true,
                IncludeGenerationConfig = true,
                IncludeStatistics = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/batch", request);

            // Assert
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var batchResponse = JsonSerializer.Deserialize<BatchExportResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(batchResponse);
            Assert.NotEmpty(batchResponse.JobId);
            Assert.NotNull(batchResponse.StatusUrl);
            Assert.NotNull(batchResponse.DownloadUrl);
        }

        [Fact]
        public async Task ExportBatch_WithTooManyLevels_ReturnsBadRequest()
        {
            // Arrange
            var levels = new List<Level>();
            for (int i = 0; i < 51; i++) // Exceed the limit of 50
            {
                levels.Add(CreateTestLevel($"Level {i + 1}"));
            }

            var request = new BatchExportRequest
            {
                Levels = levels,
                Format = "json"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/batch", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ExportBatch_WithEmptyLevels_ReturnsBadRequest()
        {
            // Arrange
            var request = new BatchExportRequest
            {
                Levels = new List<Level>(),
                Format = "json"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/batch", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetBatchExportStatus_WithValidJobId_ReturnsStatus()
        {
            // Arrange - First create a batch export job
            var levels = new List<Level> { CreateTestLevel() };
            var batchRequest = new BatchExportRequest
            {
                Levels = levels,
                Format = "json"
            };

            var batchResponse = await _client.PostAsJsonAsync("/api/export/batch", batchRequest);
            Assert.Equal(HttpStatusCode.Accepted, batchResponse.StatusCode);
            
            var batchContent = await batchResponse.Content.ReadAsStringAsync();
            var batchResult = JsonSerializer.Deserialize<BatchExportResponse>(batchContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(batchResult);
            var jobId = batchResult.JobId;

            // Act
            var statusResponse = await _client.GetAsync($"/api/export/batch/{jobId}/status");

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
        public async Task GetBatchExportStatus_WithInvalidJobId_ReturnsNotFound()
        {
            // Arrange
            var invalidJobId = "nonexistent-job-id";

            // Act
            var response = await _client.GetAsync($"/api/export/batch/{invalidJobId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DownloadBatchExport_WithInvalidJobId_ReturnsNotFound()
        {
            // Arrange
            var invalidJobId = "nonexistent-job-id";

            // Act
            var response = await _client.GetAsync($"/api/export/batch/{invalidJobId}/download");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ValidateExportRequest_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "json",
                FileName = "test_level"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/validate", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var validationResult = JsonSerializer.Deserialize<ValidationResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(validationResult);
            Assert.Empty(validationResult.Errors);
        }

        [Fact]
        public async Task ValidateExportRequest_WithInvalidRequest_ReturnsErrors()
        {
            // Arrange
            var request = new ExportRequest
            {
                Level = null!,
                Format = "invalid-format",
                FileName = "test_level"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/validate", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var validationResult = JsonSerializer.Deserialize<ValidationResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            Assert.NotNull(validationResult);
            Assert.NotEmpty(validationResult.Errors);
            Assert.Contains(validationResult.Errors, e => e.Contains("Level cannot be null"));
            Assert.Contains(validationResult.Errors, e => e.Contains("Unsupported export format"));
        }

        [Fact]
        public async Task ExportLevel_WithCustomOptions_AppliesOptions()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "json",
                FileName = "test_level",
                Options = new Dictionary<string, object>
                {
                    ["prettyPrint"] = "false"
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/export/level", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Minified JSON should not contain extra whitespace
            Assert.DoesNotContain("  ", content); // No double spaces (indentation)
        }

        [Fact]
        public async Task ExportLevel_MultipleFormats_AllSucceed()
        {
            // Arrange
            var level = CreateTestLevel();
            var formats = new[] { "json", "xml", "csv", "unity" };
            var tasks = new List<Task<HttpResponseMessage>>();

            foreach (var format in formats)
            {
                var request = new ExportRequest
                {
                    Level = level,
                    Format = format,
                    FileName = $"test_level_{format}"
                };
                
                tasks.Add(_client.PostAsJsonAsync("/api/export/level", request));
            }

            // Act
            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Creates a test level for export testing
        /// </summary>
        private static Level CreateTestLevel(string name = "Test Level")
        {
            var level = new Level
            {
                Name = name,
                Terrain = new TileMap(10, 10),
                Entities = new List<Entity>(),
                Metadata = new Dictionary<string, object>
                {
                    ["testProperty"] = "testValue",
                    ["createdAt"] = DateTime.UtcNow
                }
            };

            // Fill terrain with some test data
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    var tileType = (x + y) % 2 == 0 ? TileType.Ground : TileType.Wall;
                    level.Terrain.SetTile(x, y, tileType);
                }
            }

            // Add some test entities
            level.Entities.Add(new Entity
            {
                Type = EntityType.Player,
                Position = new System.Numerics.Vector2(1, 1),
                Properties = new Dictionary<string, object>
                {
                    ["health"] = 100,
                    ["name"] = "Test Player"
                }
            });

            level.Entities.Add(new Entity
            {
                Type = EntityType.Enemy,
                Position = new System.Numerics.Vector2(8, 8),
                Properties = new Dictionary<string, object>
                {
                    ["health"] = 50,
                    ["type"] = "goblin"
                }
            });

            return level;
        }
    }

    /// <summary>
    /// Response model for batch export creation (for testing)
    /// </summary>
    public class BatchExportResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StatusUrl { get; set; }
        public string? DownloadUrl { get; set; }
    }
}
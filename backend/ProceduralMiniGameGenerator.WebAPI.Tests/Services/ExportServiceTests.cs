using Microsoft.Extensions.Logging;
using Moq;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.Text.Json;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    /// <summary>
    /// Unit tests for ExportService
    /// </summary>
    public class ExportServiceTests
    {
        private readonly Mock<ILevelExportService> _mockLevelExportService;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly ExportService _exportService;

        public ExportServiceTests()
        {
            _mockLevelExportService = new Mock<ILevelExportService>();
            _mockLoggerService = new Mock<ILoggerService>();
            _exportService = new ExportService(_mockLevelExportService.Object, _mockLoggerService.Object);
        }

        [Fact]
        public async Task GetAvailableFormatsAsync_ReturnsExpectedFormats()
        {
            // Act
            var formats = await _exportService.GetAvailableFormatsAsync();

            // Assert
            Assert.NotNull(formats);
            Assert.Equal(4, formats.Count);
            
            var formatIds = formats.Select(f => f.Id.ToLowerInvariant()).ToList();
            Assert.Contains("json", formatIds);
            Assert.Contains("xml", formatIds);
            Assert.Contains("csv", formatIds);
            Assert.Contains("unity", formatIds);
            
            // Verify JSON format details
            var jsonFormat = formats.First(f => f.Id.Equals("json", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("JSON", jsonFormat.Name);
            Assert.Equal(".json", jsonFormat.FileExtension);
            Assert.Equal("application/json", jsonFormat.MimeType);
            Assert.True(jsonFormat.SupportsCustomization);
            Assert.Contains("includeConfig", jsonFormat.CustomizationOptions);
            
            // Verify logging was called
            _mockLoggerService.Verify(
                x => x.LogAsync(LogLevel.Information, "Getting available export formats", null),
                Times.Once);
        }

        [Fact]
        public async Task ExportLevelAsync_WithValidJsonRequest_ReturnsSuccess()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "json",
                FileName = "test_level"
            };

            var expectedJson = "{\"test\":\"data\"}";
            _mockLevelExportService
                .Setup(x => x.ExportLevelToJson(It.IsAny<Level>(), It.IsAny<GenerationConfig>()))
                .Returns(expectedJson);

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            Assert.Equal("test_level.json", result.FileName);
            Assert.Equal("application/json", result.MimeType);
            Assert.Equal(expectedJson.Length, result.FileSize);
            Assert.Empty(result.Errors);
            
            // Verify the exported JSON content
            var exportedJson = System.Text.Encoding.UTF8.GetString(result.FileData);
            Assert.Equal(expectedJson, exportedJson);
        }

        [Fact]
        public async Task ExportLevelAsync_WithValidXmlRequest_ReturnsSuccess()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "xml",
                FileName = "test_level"
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            Assert.Equal("test_level.xml", result.FileName);
            Assert.Equal("application/xml", result.MimeType);
            Assert.True(result.FileSize > 0);
            Assert.Empty(result.Errors);
            
            // Verify the exported XML content
            var exportedXml = System.Text.Encoding.UTF8.GetString(result.FileData);
            Assert.Contains("<?xml", exportedXml);
            Assert.Contains("<LevelExportData", exportedXml);
        }

        [Fact]
        public async Task ExportLevelAsync_WithValidCsvRequest_ReturnsSuccess()
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
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            Assert.Equal("test_level.csv", result.FileName);
            Assert.Equal("text/csv", result.MimeType);
            Assert.True(result.FileSize > 0);
            Assert.Empty(result.Errors);
            
            // Verify the exported CSV content
            var exportedCsv = System.Text.Encoding.UTF8.GetString(result.FileData);
            Assert.Contains("X,Y,TileType,TileValue", exportedCsv);
            Assert.Contains("0,0,", exportedCsv); // Should contain coordinate data
        }

        [Fact]
        public async Task ExportLevelAsync_WithValidUnityRequest_ReturnsSuccess()
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
                    ["scaleFactor"] = "2.0",
                    ["includePrefabData"] = "true"
                }
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            Assert.Equal("test_level_unity.json", result.FileName);
            Assert.Equal("application/json", result.MimeType);
            Assert.True(result.FileSize > 0);
            Assert.Empty(result.Errors);
            
            // Verify the exported Unity JSON content
            var exportedJson = System.Text.Encoding.UTF8.GetString(result.FileData);
            var unityData = JsonSerializer.Deserialize<JsonElement>(exportedJson);
            
            Assert.True(unityData.TryGetProperty("formatVersion", out var formatVersion));
            Assert.Equal("Unity-1.0", formatVersion.GetString());
            
            Assert.True(unityData.TryGetProperty("scaleFactor", out var scaleFactor));
            Assert.Equal(2.0, scaleFactor.GetDouble());
        }

        [Fact]
        public async Task ExportLevelAsync_WithInvalidFormat_ReturnsError()
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
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Unsupported format", result.Errors[0]);
        }

        [Fact]
        public async Task ExportLevelAsync_WithNullLevel_ReturnsError()
        {
            // Arrange
            var request = new ExportRequest
            {
                Level = null!,
                Format = "json",
                FileName = "test_level"
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Level cannot be null", result.Errors[0]);
        }

        [Fact]
        public async Task ExportLevelAsync_WithEmptyFormat_ReturnsError()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "",
                FileName = "test_level"
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Export format must be specified", result.Errors[0]);
        }

        [Fact]
        public async Task ExportBatchAsync_WithValidRequest_ReturnsJobId()
        {
            // Arrange
            var levels = new List<Level>
            {
                CreateTestLevel("Level 1"),
                CreateTestLevel("Level 2")
            };

            var request = new BatchExportRequest
            {
                Levels = levels,
                Format = "json",
                BaseFileName = "batch_level",
                CreateZipPackage = true
            };

            _mockLevelExportService
                .Setup(x => x.ExportLevelToJson(It.IsAny<Level>(), It.IsAny<GenerationConfig>()))
                .Returns("{\"test\":\"data\"}");

            // Act
            var jobId = await _exportService.ExportBatchAsync(request);

            // Assert
            Assert.NotNull(jobId);
            Assert.NotEmpty(jobId);
            
            // Verify job was created
            var status = await _exportService.GetBatchExportStatusAsync(jobId);
            Assert.NotNull(status);
            Assert.Equal(jobId, status.JobId);
            Assert.Equal("pending", status.Status);
        }

        [Fact]
        public async Task GetBatchExportStatusAsync_WithValidJobId_ReturnsStatus()
        {
            // Arrange
            var levels = new List<Level> { CreateTestLevel() };
            var request = new BatchExportRequest
            {
                Levels = levels,
                Format = "json"
            };

            _mockLevelExportService
                .Setup(x => x.ExportLevelToJson(It.IsAny<Level>(), It.IsAny<GenerationConfig>()))
                .Returns("{\"test\":\"data\"}");

            var jobId = await _exportService.ExportBatchAsync(request);

            // Act
            var status = await _exportService.GetBatchExportStatusAsync(jobId);

            // Assert
            Assert.NotNull(status);
            Assert.Equal(jobId, status.JobId);
            Assert.Contains(status.Status, new[] { "pending", "running", "completed", "failed" });
            Assert.True(status.Progress >= 0 && status.Progress <= 100);
        }

        [Fact]
        public async Task GetBatchExportStatusAsync_WithInvalidJobId_ReturnsNotFound()
        {
            // Arrange
            var invalidJobId = "nonexistent-job-id";

            // Act
            var status = await _exportService.GetBatchExportStatusAsync(invalidJobId);

            // Assert
            Assert.NotNull(status);
            Assert.Equal(invalidJobId, status.JobId);
            Assert.Equal("not_found", status.Status);
            Assert.Equal("Job not found", status.ErrorMessage);
        }

        [Fact]
        public async Task DownloadBatchExportAsync_WithInvalidJobId_ReturnsNull()
        {
            // Arrange
            var invalidJobId = "nonexistent-job-id";

            // Act
            var result = await _exportService.DownloadBatchExportAsync(invalidJobId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ExportLevelAsync_WithJsonMinificationOption_ReturnsMinifiedJson()
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

            var prettyJson = "{\n  \"test\": \"data\"\n}";
            _mockLevelExportService
                .Setup(x => x.ExportLevelToJson(It.IsAny<Level>(), It.IsAny<GenerationConfig>()))
                .Returns(prettyJson);

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            
            var exportedJson = System.Text.Encoding.UTF8.GetString(result.FileData);
            Assert.DoesNotContain("\n", exportedJson); // Should be minified
            Assert.DoesNotContain("  ", exportedJson); // Should not contain indentation
        }

        [Fact]
        public async Task ExportLevelAsync_WithCsvCustomSeparator_UsesCustomSeparator()
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
                    ["separator"] = ";",
                    ["includeHeaders"] = "true"
                }
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            
            var exportedCsv = System.Text.Encoding.UTF8.GetString(result.FileData);
            Assert.Contains("X;Y;TileType;TileValue", exportedCsv);
            Assert.Contains("0;0;", exportedCsv);
        }

        [Fact]
        public async Task ExportLevelAsync_WithUnityCoordinateSystem_ConvertsCoordinates()
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
                    ["coordinateSystem"] = "unity2d",
                    ["scaleFactor"] = "1.5"
                }
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            
            var exportedJson = System.Text.Encoding.UTF8.GetString(result.FileData);
            var unityData = JsonSerializer.Deserialize<JsonElement>(exportedJson);
            
            Assert.True(unityData.TryGetProperty("coordinateSystem", out var coordSystem));
            Assert.Equal("unity2d", coordSystem.GetString());
            
            Assert.True(unityData.TryGetProperty("scaleFactor", out var scaleFactor));
            Assert.Equal(1.5, scaleFactor.GetDouble());
        }

        [Fact]
        public async Task ExportLevelAsync_LogsOperations_VerifyLogging()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "json",
                FileName = "test_level"
            };

            _mockLevelExportService
                .Setup(x => x.ExportLevelToJson(It.IsAny<Level>(), It.IsAny<GenerationConfig>()))
                .Returns("{\"test\":\"data\"}");

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            
            // Verify logging calls
            _mockLoggerService.Verify(
                x => x.LogAsync(
                    LogLevel.Information,
                    It.Is<string>(s => s.Contains("Starting level export")),
                    It.IsAny<object>()),
                Times.Once);
                
            _mockLoggerService.Verify(
                x => x.LogAsync(
                    LogLevel.Information,
                    It.Is<string>(s => s.Contains("Level export completed successfully")),
                    It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ExportLevelAsync_WhenExceptionThrown_LogsError()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "json",
                FileName = "test_level"
            };

            _mockLevelExportService
                .Setup(x => x.ExportLevelToJson(It.IsAny<Level>(), It.IsAny<GenerationConfig>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Export failed: Test exception", result.Errors[0]);
            
            // Verify error logging
            _mockLoggerService.Verify(
                x => x.LogErrorAsync(
                    It.IsAny<Exception>(),
                    "Level export error",
                    It.IsAny<object>()),
                Times.Once);
        }

        /// <summary>
        /// Creates a test level for export testing
        /// </summary>
        private static Level CreateTestLevel(string name = "Test Level")
        {
            var level = new Level
            {
                Name = name,
                Terrain = new TileMap(5, 5),
                Entities = new List<Entity>(),
                Metadata = new Dictionary<string, object>
                {
                    ["testProperty"] = "testValue"
                }
            };

            // Fill terrain with test data
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    var tileType = (x + y) % 2 == 0 ? TileType.Ground : TileType.Wall;
                    level.Terrain.SetTile(x, y, tileType);
                }
            }

            // Add test entities
            level.Entities.Add(new Entity
            {
                Type = EntityType.Player,
                Position = new System.Numerics.Vector2(1, 1),
                Properties = new Dictionary<string, object> { ["health"] = 100 }
            });

            return level;
        }
    }
}
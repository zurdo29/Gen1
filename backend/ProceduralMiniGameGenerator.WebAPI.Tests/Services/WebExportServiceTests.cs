using Microsoft.Extensions.Logging;
using Moq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.Text.Json;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    /// <summary>
    /// Tests for web-specific export functionality
    /// </summary>
    public class WebExportServiceTests
    {
        private readonly Mock<ILevelExportService> _mockLevelExportService;
        private readonly Mock<ILoggerService> _mockLoggerService;
        private readonly ExportService _exportService;

        public WebExportServiceTests()
        {
            _mockLevelExportService = new Mock<ILevelExportService>();
            _mockLoggerService = new Mock<ILoggerService>();
            _exportService = new ExportService(_mockLevelExportService.Object, _mockLoggerService.Object);
        }

        [Fact]
        public async Task ExportToWebJson_ShouldReturnOptimizedJsonFormat()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "web-json",
                Options = new Dictionary<string, object>
                {
                    ["compactFormat"] = "false",
                    ["includeMetadata"] = "true",
                    ["includePreview"] = "true"
                }
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            Assert.Equal("application/json", result.MimeType);
            Assert.Contains("_web.json", result.FileName);

            // Verify JSON structure
            var jsonString = System.Text.Encoding.UTF8.GetString(result.FileData);
            var jsonDoc = JsonDocument.Parse(jsonString);
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("version", out var version));
            Assert.Equal("web-1.0", version.GetString());
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("level", out var levelElement));
            Assert.True(levelElement.TryGetProperty("terrain", out var terrainElement));
            Assert.True(levelElement.TryGetProperty("entities", out var entitiesElement));
        }

        [Fact]
        public async Task ExportToImage_ShouldReturnPngImageData()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "image",
                Options = new Dictionary<string, object>
                {
                    ["width"] = "800",
                    ["height"] = "600",
                    ["tileSize"] = "16",
                    ["showGrid"] = "true",
                    ["showEntities"] = "true"
                }
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            Assert.Equal("image/png", result.MimeType);
            Assert.Contains("_preview.png", result.FileName);
        }

        [Fact]
        public async Task ExportToCsvExtended_ShouldIncludeTerrainAndEntityData()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "csv-extended",
                Options = new Dictionary<string, object>
                {
                    ["includeEntities"] = "true",
                    ["includeHeaders"] = "true",
                    ["separator"] = ",",
                    ["includeStatistics"] = "true"
                }
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            Assert.Equal("text/csv", result.MimeType);
            Assert.Contains("_extended.csv", result.FileName);

            // Verify CSV content
            var csvContent = System.Text.Encoding.UTF8.GetString(result.FileData);
            Assert.Contains("# TERRAIN DATA", csvContent);
            Assert.Contains("# ENTITY DATA", csvContent);
            Assert.Contains("# STATISTICS", csvContent);
            Assert.Contains("X,Y,TileType,TileValue,Walkable", csvContent);
        }

        [Fact]
        public async Task ExportToShareUrl_ShouldGenerateShareableConfiguration()
        {
            // Arrange
            var level = CreateTestLevel();
            var request = new ExportRequest
            {
                Level = level,
                Format = "share-url",
                Options = new Dictionary<string, object>
                {
                    ["includePreview"] = "true",
                    ["expirationDays"] = "30",
                    ["baseUrl"] = "https://example.com"
                }
            };

            // Act
            var result = await _exportService.ExportLevelAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileData);
            Assert.Equal("text/plain", result.MimeType);
            Assert.Contains("_share.txt", result.FileName);

            // Verify share content
            var shareContent = System.Text.Encoding.UTF8.GetString(result.FileData);
            Assert.Contains("Shareable Level Configuration", shareContent);
            Assert.Contains("https://example.com/share/", shareContent);
            Assert.Contains("JSON Data:", shareContent);
        }

        [Fact]
        public async Task GetAvailableFormats_ShouldIncludeWebSpecificFormats()
        {
            // Act
            var formats = await _exportService.GetAvailableFormatsAsync();

            // Assert
            Assert.Contains(formats, f => f.Id == "web-json");
            Assert.Contains(formats, f => f.Id == "image");
            Assert.Contains(formats, f => f.Id == "csv-extended");
            Assert.Contains(formats, f => f.Id == "share-url");

            // Verify web-json format details
            var webJsonFormat = formats.First(f => f.Id == "web-json");
            Assert.Equal("Web JSON", webJsonFormat.Name);
            Assert.Equal(".json", webJsonFormat.FileExtension);
            Assert.True(webJsonFormat.SupportsCustomization);
            Assert.Contains("includeMetadata", webJsonFormat.CustomizationOptions);
            Assert.Contains("compactFormat", webJsonFormat.CustomizationOptions);
            Assert.Contains("includePreview", webJsonFormat.CustomizationOptions);
        }

        private Level CreateTestLevel()
        {
            var terrain = new TileMap(10, 10);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    terrain.SetTile(x, y, x == 0 || y == 0 || x == 9 || y == 9 ? TileType.Wall : TileType.Ground);
                }
            }

            var entities = new List<Entity>
            {
                new Entity { Type = EntityType.Player, Position = new Vector2(5, 5) },
                new Entity { Type = EntityType.Enemy, Position = new Vector2(3, 3) },
                new Entity { Type = EntityType.Item, Position = new Vector2(7, 7) }
            };

            return new Level
            {
                Name = "TestLevel",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>
                {
                    ["seed"] = 12345,
                    ["algorithm"] = "test",
                    ["parameters"] = new Dictionary<string, object> { ["difficulty"] = "easy" }
                }
            };
        }
    }
}
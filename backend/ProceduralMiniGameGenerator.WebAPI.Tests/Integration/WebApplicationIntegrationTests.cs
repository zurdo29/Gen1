using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Integration;

public class WebApplicationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WebApplicationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GenerateLevel_WithValidConfig_ReturnsLevel()
    {
        // Arrange
        var config = new GenerationConfig
        {
            Terrain = new TerrainConfig
            {
                Generator = "perlin-noise",
                Width = 20,
                Height = 20,
                Seed = 12345
            },
            Entities = new EntityConfig
            {
                Placer = "random",
                Density = 0.1f
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/generation/generate", config);

        // Assert
        response.EnsureSuccessStatusCode();
        var level = await response.Content.ReadFromJsonAsync<Level>();
        level.Should().NotBeNull();
        level!.Width.Should().Be(20);
        level.Height.Should().Be(20);
        level.Terrain.Should().NotBeNull();
        level.Entities.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateLevel_WithInvalidConfig_ReturnsBadRequest()
    {
        // Arrange
        var invalidConfig = new GenerationConfig
        {
            Terrain = new TerrainConfig
            {
                Width = -1, // Invalid width
                Height = 20
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/generation/generate", invalidConfig);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("width");
    }

    [Fact]
    public async Task ValidateConfig_WithValidConfig_ReturnsValid()
    {
        // Arrange
        var config = new GenerationConfig
        {
            Terrain = new TerrainConfig
            {
                Generator = "perlin-noise",
                Width = 20,
                Height = 20,
                Seed = 12345
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/generation/validate-config", config);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ValidationResult>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPresets_ReturnsPresetList()
    {
        // Act
        var response = await _client.GetAsync("/api/configuration/presets");

        // Assert
        response.EnsureSuccessStatusCode();
        var presets = await response.Content.ReadFromJsonAsync<List<ConfigPreset>>();
        presets.Should().NotBeNull();
        presets.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportLevel_WithValidLevel_ReturnsFile()
    {
        // Arrange
        var level = new Level
        {
            Id = "test-level",
            Width = 10,
            Height = 10,
            Terrain = new string[10, 10],
            Entities = new List<Entity>()
        };

        var exportRequest = new ExportRequest
        {
            Level = level,
            Format = "json",
            Options = new Dictionary<string, object>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/level", exportRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetExportFormats_ReturnsAvailableFormats()
    {
        // Act
        var response = await _client.GetAsync("/api/export/formats");

        // Assert
        response.EnsureSuccessStatusCode();
        var formats = await response.Content.ReadFromJsonAsync<List<ExportFormat>>();
        formats.Should().NotBeNull();
        formats.Should().NotBeEmpty();
        formats.Should().Contain(f => f.Id == "json");
        formats.Should().Contain(f => f.Id == "unity");
    }

    [Fact]
    public async Task ShareConfiguration_CreatesShareLink()
    {
        // Arrange
        var config = new GenerationConfig
        {
            Terrain = new TerrainConfig
            {
                Generator = "perlin-noise",
                Width = 20,
                Height = 20
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/configuration/share", config);

        // Assert
        response.EnsureSuccessStatusCode();
        var shareResult = await response.Content.ReadFromJsonAsync<ShareResult>();
        shareResult.Should().NotBeNull();
        shareResult!.ShareId.Should().NotBeEmpty();
        shareResult.ShareUrl.Should().NotBeEmpty();
        shareResult.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GetSharedConfiguration_WithValidShareId_ReturnsConfig()
    {
        // Arrange - First create a shared configuration
        var config = new GenerationConfig
        {
            Terrain = new TerrainConfig
            {
                Generator = "perlin-noise",
                Width = 20,
                Height = 20
            }
        };

        var shareResponse = await _client.PostAsJsonAsync("/api/configuration/share", config);
        var shareResult = await shareResponse.Content.ReadFromJsonAsync<ShareResult>();

        // Act
        var response = await _client.GetAsync($"/api/configuration/share/{shareResult!.ShareId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var retrievedConfig = await response.Content.ReadFromJsonAsync<GenerationConfig>();
        retrievedConfig.Should().NotBeNull();
        retrievedConfig!.Terrain.Width.Should().Be(20);
        retrievedConfig.Terrain.Height.Should().Be(20);
    }

    [Fact]
    public async Task ConcurrentGeneration_HandlesMultipleRequests()
    {
        // Arrange
        var config = new GenerationConfig
        {
            Terrain = new TerrainConfig
            {
                Generator = "perlin-noise",
                Width = 10,
                Height = 10,
                Seed = 12345
            }
        };

        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send 5 concurrent requests
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.PostAsJsonAsync("/api/generation/generate", config));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.EnsureSuccessStatusCode();
            var level = await response.Content.ReadFromJsonAsync<Level>();
            level.Should().NotBeNull();
        }
    }
}
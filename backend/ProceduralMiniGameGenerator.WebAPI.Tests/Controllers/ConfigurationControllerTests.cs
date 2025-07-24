using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Controllers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Controllers
{
    /// <summary>
    /// Integration tests for ConfigurationController
    /// </summary>
    public class ConfigurationControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigurationControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        #region Preset Management Tests

        [Fact]
        public async Task GetPresets_ReturnsDefaultPresets()
        {
            // Act
            var response = await _client.GetAsync("/api/configuration/presets");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var presets = JsonSerializer.Deserialize<List<ConfigPreset>>(content, _jsonOptions);
            
            Assert.NotNull(presets);
            Assert.NotEmpty(presets);
            Assert.Contains(presets, p => p.Name == "Small Level");
            Assert.Contains(presets, p => p.Name == "Medium Level");
            Assert.Contains(presets, p => p.Name == "Maze Level");
        }

        [Fact]
        public async Task GetPreset_WithValidId_ReturnsPreset()
        {
            // Arrange - First get all presets to find a valid ID
            var presetsResponse = await _client.GetAsync("/api/configuration/presets");
            var presetsContent = await presetsResponse.Content.ReadAsStringAsync();
            var presets = JsonSerializer.Deserialize<List<ConfigPreset>>(presetsContent, _jsonOptions);
            
            Assert.NotNull(presets);
            Assert.NotEmpty(presets);
            
            var firstPreset = presets.First();

            // Act
            var response = await _client.GetAsync($"/api/configuration/presets/{firstPreset.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var preset = JsonSerializer.Deserialize<ConfigPreset>(content, _jsonOptions);
            
            Assert.NotNull(preset);
            Assert.Equal(firstPreset.Id, preset.Id);
            Assert.Equal(firstPreset.Name, preset.Name);
            Assert.NotNull(preset.Config);
        }

        [Fact]
        public async Task GetPreset_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/configuration/presets/nonexistent-id");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SavePreset_WithValidPreset_ReturnsCreated()
        {
            // Arrange
            var preset = new ConfigPreset
            {
                Name = "Test Preset",
                Description = "A test preset for unit testing",
                Config = CreateValidConfig(),
                Tags = new List<string> { "test", "unit-test" },
                IsPublic = false
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/configuration/presets", preset);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var savedPreset = JsonSerializer.Deserialize<ConfigPreset>(content, _jsonOptions);
            
            Assert.NotNull(savedPreset);
            Assert.NotEmpty(savedPreset.Id);
            Assert.Equal(preset.Name, savedPreset.Name);
            Assert.Equal(preset.Description, savedPreset.Description);
            Assert.NotNull(savedPreset.Config);
            Assert.True(savedPreset.CreatedAt > DateTime.MinValue);
            Assert.True(savedPreset.LastModified > DateTime.MinValue);
        }

        [Fact]
        public async Task SavePreset_WithInvalidConfig_ReturnsBadRequest()
        {
            // Arrange
            var preset = new ConfigPreset
            {
                Name = "Invalid Preset",
                Config = new GenerationConfig
                {
                    Width = -1, // Invalid
                    Height = -1, // Invalid
                    GenerationAlgorithm = ""
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/configuration/presets", preset);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SavePreset_WithNullPreset_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/configuration/presets", (ConfigPreset?)null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePreset_WithValidData_ReturnsUpdated()
        {
            // Arrange - First create a preset
            var originalPreset = new ConfigPreset
            {
                Name = "Original Preset",
                Description = "Original description",
                Config = CreateValidConfig(),
                Tags = new List<string> { "original" }
            };

            var createResponse = await _client.PostAsJsonAsync("/api/configuration/presets", originalPreset);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createdPreset = JsonSerializer.Deserialize<ConfigPreset>(createContent, _jsonOptions);
            Assert.NotNull(createdPreset);

            // Modify the preset
            var updatedPreset = new ConfigPreset
            {
                Name = "Updated Preset",
                Description = "Updated description",
                Config = CreateValidConfig(),
                Tags = new List<string> { "updated", "modified" },
                IsPublic = true
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/configuration/presets/{createdPreset.Id}", updatedPreset);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ConfigPreset>(content, _jsonOptions);
            
            Assert.NotNull(result);
            Assert.Equal(createdPreset.Id, result.Id);
            Assert.Equal(updatedPreset.Name, result.Name);
            Assert.Equal(updatedPreset.Description, result.Description);
            Assert.Equal(updatedPreset.IsPublic, result.IsPublic);
            Assert.True(result.LastModified > result.CreatedAt);
        }

        [Fact]
        public async Task UpdatePreset_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var preset = new ConfigPreset
            {
                Name = "Test Preset",
                Config = CreateValidConfig()
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/configuration/presets/nonexistent-id", preset);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeletePreset_WithValidId_ReturnsNoContent()
        {
            // Arrange - First create a preset
            var preset = new ConfigPreset
            {
                Name = "Preset to Delete",
                Config = CreateValidConfig()
            };

            var createResponse = await _client.PostAsJsonAsync("/api/configuration/presets", preset);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createdPreset = JsonSerializer.Deserialize<ConfigPreset>(createContent, _jsonOptions);
            Assert.NotNull(createdPreset);

            // Act
            var response = await _client.DeleteAsync($"/api/configuration/presets/{createdPreset.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify it's actually deleted
            var getResponse = await _client.GetAsync($"/api/configuration/presets/{createdPreset.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task DeletePreset_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await _client.DeleteAsync("/api/configuration/presets/nonexistent-id");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Sharing Tests

        [Fact]
        public async Task CreateShareLink_WithValidConfig_ReturnsShareResult()
        {
            // Arrange
            var shareRequest = new ShareRequest
            {
                Config = CreateValidConfig(),
                ExpiryDays = 7,
                Description = "Test shared configuration"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/configuration/share", shareRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var shareResult = JsonSerializer.Deserialize<ShareResult>(content, _jsonOptions);
            
            Assert.NotNull(shareResult);
            Assert.NotEmpty(shareResult.ShareId);
            Assert.NotEmpty(shareResult.ShareUrl);
            Assert.True(shareResult.ExpiresAt > DateTime.UtcNow);
            Assert.True(shareResult.ExpiresAt <= DateTime.UtcNow.AddDays(7));
            Assert.NotNull(shareResult.Description);
        }

        [Fact]
        public async Task CreateShareLink_WithDefaultExpiry_Uses30Days()
        {
            // Arrange
            var shareRequest = new ShareRequest
            {
                Config = CreateValidConfig()
                // No ExpiryDays specified - should default to 30
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/configuration/share", shareRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var shareResult = JsonSerializer.Deserialize<ShareResult>(content, _jsonOptions);
            
            Assert.NotNull(shareResult);
            Assert.True(shareResult.ExpiresAt > DateTime.UtcNow.AddDays(29));
            Assert.True(shareResult.ExpiresAt <= DateTime.UtcNow.AddDays(31));
        }

        [Fact]
        public async Task CreateShareLink_WithInvalidExpiryDays_ReturnsBadRequest()
        {
            // Arrange
            var shareRequest = new ShareRequest
            {
                Config = CreateValidConfig(),
                ExpiryDays = 400 // Invalid - exceeds 365 day limit
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/configuration/share", shareRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateShareLink_WithInvalidConfig_ReturnsBadRequest()
        {
            // Arrange
            var shareRequest = new ShareRequest
            {
                Config = new GenerationConfig
                {
                    Width = -1, // Invalid
                    Height = -1, // Invalid
                    GenerationAlgorithm = ""
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/configuration/share", shareRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateShareLink_WithNullConfig_ReturnsBadRequest()
        {
            // Arrange
            var shareRequest = new ShareRequest
            {
                Config = null!
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/configuration/share", shareRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetSharedConfiguration_WithValidShareId_ReturnsConfig()
        {
            // Arrange - First create a share link
            var originalConfig = CreateValidConfig();
            var shareRequest = new ShareRequest
            {
                Config = originalConfig,
                ExpiryDays = 1
            };

            var createResponse = await _client.PostAsJsonAsync("/api/configuration/share", shareRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var shareResult = JsonSerializer.Deserialize<ShareResult>(createContent, _jsonOptions);
            Assert.NotNull(shareResult);

            // Act
            var response = await _client.GetAsync($"/api/configuration/share/{shareResult.ShareId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var retrievedConfig = JsonSerializer.Deserialize<GenerationConfig>(content, _jsonOptions);
            
            Assert.NotNull(retrievedConfig);
            Assert.Equal(originalConfig.Width, retrievedConfig.Width);
            Assert.Equal(originalConfig.Height, retrievedConfig.Height);
            Assert.Equal(originalConfig.GenerationAlgorithm, retrievedConfig.GenerationAlgorithm);
        }

        [Fact]
        public async Task GetSharedConfiguration_WithInvalidShareId_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/configuration/share/nonexistent-share-id");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task FullWorkflow_CreatePresetAndShare_WorksEndToEnd()
        {
            // Step 1: Create a preset
            var preset = new ConfigPreset
            {
                Name = "Integration Test Preset",
                Description = "Testing full workflow",
                Config = CreateValidConfig(),
                Tags = new List<string> { "integration", "test" }
            };

            var createPresetResponse = await _client.PostAsJsonAsync("/api/configuration/presets", preset);
            Assert.Equal(HttpStatusCode.Created, createPresetResponse.StatusCode);
            
            var createdPreset = JsonSerializer.Deserialize<ConfigPreset>(
                await createPresetResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(createdPreset);

            // Step 2: Share the configuration
            var shareRequest = new ShareRequest
            {
                Config = createdPreset.Config,
                ExpiryDays = 1
            };

            var shareResponse = await _client.PostAsJsonAsync("/api/configuration/share", shareRequest);
            Assert.Equal(HttpStatusCode.Created, shareResponse.StatusCode);
            
            var shareResult = JsonSerializer.Deserialize<ShareResult>(
                await shareResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(shareResult);

            // Step 3: Retrieve shared configuration
            var getSharedResponse = await _client.GetAsync($"/api/configuration/share/{shareResult.ShareId}");
            Assert.Equal(HttpStatusCode.OK, getSharedResponse.StatusCode);
            
            var sharedConfig = JsonSerializer.Deserialize<GenerationConfig>(
                await getSharedResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(sharedConfig);

            // Step 4: Verify configurations match
            Assert.Equal(createdPreset.Config.Width, sharedConfig.Width);
            Assert.Equal(createdPreset.Config.Height, sharedConfig.Height);
            Assert.Equal(createdPreset.Config.GenerationAlgorithm, sharedConfig.GenerationAlgorithm);

            // Step 5: Clean up - delete the preset
            var deleteResponse = await _client.DeleteAsync($"/api/configuration/presets/{createdPreset.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task ConcurrentPresetOperations_HandleCorrectly()
        {
            // Arrange
            var tasks = new List<Task<HttpResponseMessage>>();
            
            // Create multiple presets concurrently
            for (int i = 0; i < 5; i++)
            {
                var preset = new ConfigPreset
                {
                    Name = $"Concurrent Test Preset {i}",
                    Config = CreateValidConfig(),
                    Tags = new List<string> { "concurrent", $"test-{i}" }
                };
                
                tasks.Add(_client.PostAsJsonAsync("/api/configuration/presets", preset));
            }

            // Act
            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }

            // Verify all presets were created
            var getPresetsResponse = await _client.GetAsync("/api/configuration/presets");
            Assert.Equal(HttpStatusCode.OK, getPresetsResponse.StatusCode);
            
            var presets = JsonSerializer.Deserialize<List<ConfigPreset>>(
                await getPresetsResponse.Content.ReadAsStringAsync(), _jsonOptions);
            Assert.NotNull(presets);
            
            var concurrentPresets = presets.Where(p => p.Name.StartsWith("Concurrent Test Preset")).ToList();
            Assert.Equal(5, concurrentPresets.Count);
        }

        #endregion

        /// <summary>
        /// Creates a valid configuration for testing
        /// </summary>
        private static GenerationConfig CreateValidConfig()
        {
            return new GenerationConfig
            {
                Width = 25,
                Height = 25,
                Seed = 12345,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    ["scale"] = 0.1,
                    ["octaves"] = 3
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
    }
}
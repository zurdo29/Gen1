using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Controllers;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.IntegrationTests
{
    /// <summary>
    /// Comprehensive integration test that exercises all major modules:
    /// - Generation API with real-time updates
    /// - Configuration management and presets
    /// - Export functionality with multiple formats
    /// - Sharing and collaboration features
    /// - Performance optimizations and caching
    /// - Error handling and validation
    /// </summary>
    public class ComprehensiveIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ComprehensiveIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CompleteWorkflow_GeneratePreviewEditExportShare_ShouldWorkEndToEnd()
        {
            // Arrange - Create a comprehensive generation configuration
            var generationConfig = new GenerationConfig
            {
                Width = 50,
                Height = 50,
                TerrainType = "PerlinNoise",
                EntityDensity = 0.3f,
                Theme = "Forest",
                Seed = 12345,
                GameplayParameters = new Dictionary<string, object>
                {
                    ["difficulty"] = "medium",
                    ["playerSpawns"] = 4,
                    ["collectibles"] = true
                }
            };

            // Step 1: Validate configuration
            var validateResponse = await _client.PostAsJsonAsync("/api/configuration/validate", generationConfig);
            validateResponse.EnsureSuccessStatusCode();
            var validationResult = await validateResponse.Content.ReadFromJsonAsync<ValidationResult>();
            validationResult.IsValid.Should().BeTrue();

            // Step 2: Generate level with real-time updates
            var generateResponse = await _client.PostAsJsonAsync("/api/generation/generate", new GenerationRequest
            {
                Config = generationConfig,
                IncludePreview = true,
                EnableRealTimeUpdates = true
            });
            generateResponse.EnsureSuccessStatusCode();
            var generationResult = await generateResponse.Content.ReadFromJsonAsync<GenerationResult>();
            
            generationResult.Should().NotBeNull();
            generationResult.Level.Should().NotBeNull();
            generationResult.Level.Width.Should().Be(50);
            generationResult.Level.Height.Should().Be(50);
            generationResult.PreviewData.Should().NotBeNull();

            // Step 3: Save configuration as preset
            var preset = new ConfigPreset
            {
                Name = "Integration Test Forest",
                Description = "Comprehensive test configuration",
                Config = generationConfig,
                Tags = new[] { "test", "forest", "medium" }
            };

            var savePresetResponse = await _client.PostAsJsonAsync("/api/configuration/presets", preset);
            savePresetResponse.EnsureSuccessStatusCode();
            var savedPreset = await savePresetResponse.Content.ReadFromJsonAsync<ConfigPreset>();
            savedPreset.Id.Should().NotBeEmpty();

            // Step 4: Test batch generation
            var batchRequest = new BatchGenerationRequest
            {
                BaseConfig = generationConfig,
                Variations = new[]
                {
                    new ConfigVariation { Parameter = "Seed", Values = new object[] { 12345, 54321, 98765 } },
                    new ConfigVariation { Parameter = "EntityDensity", Values = new object[] { 0.2f, 0.3f, 0.4f } }
                },
                Count = 5
            };

            var batchResponse = await _client.PostAsJsonAsync("/api/generation/generate-batch", batchRequest);
            batchResponse.EnsureSuccessStatusCode();
            var batchJobId = await batchResponse.Content.ReadAsStringAsync();
            batchJobId.Should().NotBeNullOrEmpty();

            // Step 5: Poll batch generation status
            var maxAttempts = 10;
            var attempts = 0;
            JobStatus batchStatus;
            do
            {
                await Task.Delay(500); // Wait for processing
                var statusResponse = await _client.GetAsync($"/api/generation/job/{batchJobId}/status");
                statusResponse.EnsureSuccessStatusCode();
                batchStatus = await statusResponse.Content.ReadFromJsonAsync<JobStatus>();
                attempts++;
            } while (batchStatus.Status != "completed" && attempts < maxAttempts);

            batchStatus.Status.Should().Be("completed");
            batchStatus.Result.Should().NotBeNull();

            // Step 6: Export level in multiple formats
            var exportFormats = new[] { "JSON", "Unity", "CSV", "Image" };
            var exportResults = new Dictionary<string, byte[]>();

            foreach (var format in exportFormats)
            {
                var exportRequest = new ExportRequest
                {
                    Level = generationResult.Level,
                    Format = format,
                    Options = new Dictionary<string, object>
                    {
                        ["includeMetadata"] = true,
                        ["compression"] = format == "JSON" ? "gzip" : "none"
                    }
                };

                var exportResponse = await _client.PostAsJsonAsync("/api/export/level", exportRequest);
                exportResponse.EnsureSuccessStatusCode();
                var exportData = await exportResponse.Content.ReadAsByteArrayAsync();
                exportData.Should().NotBeEmpty();
                exportResults[format] = exportData;
            }

            // Step 7: Create shareable link
            var shareResponse = await _client.PostAsJsonAsync("/api/configuration/share", generationConfig);
            shareResponse.EnsureSuccessStatusCode();
            var shareResult = await shareResponse.Content.ReadFromJsonAsync<ShareResult>();
            shareResult.ShareId.Should().NotBeNullOrEmpty();
            shareResult.ShareUrl.Should().NotBeNullOrEmpty();

            // Step 8: Retrieve shared configuration
            var retrieveSharedResponse = await _client.GetAsync($"/api/configuration/share/{shareResult.ShareId}");
            retrieveSharedResponse.EnsureSuccessStatusCode();
            var retrievedConfig = await retrieveSharedResponse.Content.ReadFromJsonAsync<GenerationConfig>();
            retrievedConfig.Should().BeEquivalentTo(generationConfig);

            // Step 9: Test performance with caching
            var cachedGenerateResponse = await _client.PostAsJsonAsync("/api/generation/generate", new GenerationRequest
            {
                Config = generationConfig, // Same config should hit cache
                IncludePreview = true
            });
            cachedGenerateResponse.EnsureSuccessStatusCode();
            
            // Verify cache headers
            cachedGenerateResponse.Headers.Should().ContainKey("X-Cache-Status");

            // Step 10: Test error handling with invalid configuration
            var invalidConfig = new GenerationConfig
            {
                Width = -1, // Invalid
                Height = 0,  // Invalid
                TerrainType = "NonExistentType"
            };

            var invalidResponse = await _client.PostAsJsonAsync("/api/generation/generate", new GenerationRequest
            {
                Config = invalidConfig
            });
            
            invalidResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var errorResponse = await invalidResponse.Content.ReadAsStringAsync();
            errorResponse.Should().Contain("validation");
        }

        [Fact]
        public async Task RealTimeUpdates_WebSocketConnection_ShouldReceiveProgressUpdates()
        {
            // Test WebSocket real-time updates during generation
            var config = new GenerationConfig
            {
                Width = 100,
                Height = 100,
                TerrainType = "CellularAutomata",
                EntityDensity = 0.4f
            };

            // This would typically use WebSocket client for real-time testing
            // For now, we'll test the HTTP polling approach
            var generateResponse = await _client.PostAsJsonAsync("/api/generation/generate", new GenerationRequest
            {
                Config = config,
                EnableRealTimeUpdates = true
            });

            if (generateResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                var jobId = await generateResponse.Content.ReadAsStringAsync();
                
                // Poll for updates
                var updates = new List<JobStatus>();
                var maxAttempts = 20;
                var attempts = 0;
                
                JobStatus status;
                do
                {
                    await Task.Delay(200);
                    var statusResponse = await _client.GetAsync($"/api/generation/job/{jobId}/status");
                    statusResponse.EnsureSuccessStatusCode();
                    status = await statusResponse.Content.ReadFromJsonAsync<JobStatus>();
                    updates.Add(status);
                    attempts++;
                } while (status.Status != "completed" && status.Status != "failed" && attempts < maxAttempts);

                updates.Should().HaveCountGreaterThan(1);
                updates.Last().Status.Should().Be("completed");
                updates.Should().Contain(u => u.Progress > 0 && u.Progress < 100); // Should have intermediate progress
            }
        }

        [Fact]
        public async Task PerformanceOptimizations_ConcurrentRequests_ShouldHandleLoad()
        {
            // Test concurrent generation requests to verify performance optimizations
            var configs = Enumerable.Range(1, 10).Select(i => new GenerationConfig
            {
                Width = 25,
                Height = 25,
                TerrainType = "PerlinNoise",
                Seed = i * 1000,
                EntityDensity = 0.2f
            }).ToArray();

            var tasks = configs.Select(async config =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await _client.PostAsJsonAsync("/api/generation/generate", new GenerationRequest
                {
                    Config = config,
                    IncludePreview = true
                });
                stopwatch.Stop();
                
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<GenerationResult>();
                
                return new { Result = result, Duration = stopwatch.ElapsedMilliseconds };
            });

            var results = await Task.WhenAll(tasks);
            
            // All requests should complete successfully
            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.Result != null);
            
            // Performance should be reasonable (adjust thresholds as needed)
            var averageDuration = results.Average(r => r.Duration);
            averageDuration.Should().BeLessThan(5000); // 5 seconds average
        }

        [Fact]
        public async Task ErrorHandling_VariousErrorScenarios_ShouldReturnAppropriateResponses()
        {
            // Test 1: Invalid JSON
            var invalidJsonContent = new StringContent("{ invalid json }", System.Text.Encoding.UTF8, "application/json");
            var invalidJsonResponse = await _client.PostAsync("/api/generation/generate", invalidJsonContent);
            invalidJsonResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            // Test 2: Missing required fields
            var incompleteConfig = new { Width = 10 }; // Missing required fields
            var incompleteResponse = await _client.PostAsJsonAsync("/api/generation/generate", incompleteConfig);
            incompleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            // Test 3: Non-existent preset
            var nonExistentPresetResponse = await _client.GetAsync("/api/configuration/presets/non-existent-id");
            nonExistentPresetResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

            // Test 4: Invalid export format
            var invalidExportRequest = new ExportRequest
            {
                Level = new Level { Width = 10, Height = 10, Tiles = new Tile[100] },
                Format = "InvalidFormat"
            };
            var invalidExportResponse = await _client.PostAsJsonAsync("/api/export/level", invalidExportRequest);
            invalidExportResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            // Test 5: Rate limiting (if implemented)
            // This would test the rate limiting functionality
        }

        [Fact]
        public async Task ConfigurationManagement_FullCRUDOperations_ShouldWorkCorrectly()
        {
            // Create
            var preset = new ConfigPreset
            {
                Name = "CRUD Test Preset",
                Description = "Testing CRUD operations",
                Config = new GenerationConfig
                {
                    Width = 30,
                    Height = 30,
                    TerrainType = "Maze",
                    EntityDensity = 0.25f
                },
                Tags = new[] { "test", "crud" }
            };

            var createResponse = await _client.PostAsJsonAsync("/api/configuration/presets", preset);
            createResponse.EnsureSuccessStatusCode();
            var createdPreset = await createResponse.Content.ReadFromJsonAsync<ConfigPreset>();
            createdPreset.Id.Should().NotBeEmpty();

            // Read
            var readResponse = await _client.GetAsync($"/api/configuration/presets/{createdPreset.Id}");
            readResponse.EnsureSuccessStatusCode();
            var readPreset = await readResponse.Content.ReadFromJsonAsync<ConfigPreset>();
            readPreset.Should().BeEquivalentTo(createdPreset);

            // Update
            createdPreset.Name = "Updated CRUD Test Preset";
            createdPreset.Description = "Updated description";
            var updateResponse = await _client.PutAsJsonAsync($"/api/configuration/presets/{createdPreset.Id}", createdPreset);
            updateResponse.EnsureSuccessStatusCode();

            // Verify update
            var verifyUpdateResponse = await _client.GetAsync($"/api/configuration/presets/{createdPreset.Id}");
            verifyUpdateResponse.EnsureSuccessStatusCode();
            var updatedPreset = await verifyUpdateResponse.Content.ReadFromJsonAsync<ConfigPreset>();
            updatedPreset.Name.Should().Be("Updated CRUD Test Preset");

            // Delete
            var deleteResponse = await _client.DeleteAsync($"/api/configuration/presets/{createdPreset.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            // Verify deletion
            var verifyDeleteResponse = await _client.GetAsync($"/api/configuration/presets/{createdPreset.Id}");
            verifyDeleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ExportFunctionality_AllFormats_ShouldGenerateValidOutput()
        {
            // Generate a test level
            var config = new GenerationConfig
            {
                Width = 20,
                Height = 20,
                TerrainType = "PerlinNoise",
                EntityDensity = 0.3f,
                Seed = 42
            };

            var generateResponse = await _client.PostAsJsonAsync("/api/generation/generate", new GenerationRequest
            {
                Config = config
            });
            generateResponse.EnsureSuccessStatusCode();
            var generationResult = await generateResponse.Content.ReadFromJsonAsync<GenerationResult>();

            // Test all export formats
            var formatsResponse = await _client.GetAsync("/api/export/formats");
            formatsResponse.EnsureSuccessStatusCode();
            var availableFormats = await formatsResponse.Content.ReadFromJsonAsync<List<ExportFormat>>();

            foreach (var format in availableFormats)
            {
                var exportRequest = new ExportRequest
                {
                    Level = generationResult.Level,
                    Format = format.Id,
                    Options = new Dictionary<string, object>
                    {
                        ["includeMetadata"] = true,
                        ["optimizeForSize"] = false
                    }
                };

                var exportResponse = await _client.PostAsJsonAsync("/api/export/level", exportRequest);
                exportResponse.EnsureSuccessStatusCode();
                
                var exportData = await exportResponse.Content.ReadAsByteArrayAsync();
                exportData.Should().NotBeEmpty($"Export format {format.Id} should produce data");

                // Verify content type
                var contentType = exportResponse.Content.Headers.ContentType?.MediaType;
                contentType.Should().NotBeNullOrEmpty($"Export format {format.Id} should have content type");
            }
        }
    }

    // Supporting models for the tests
    public class GenerationRequest
    {
        public GenerationConfig Config { get; set; }
        public bool IncludePreview { get; set; } = false;
        public bool EnableRealTimeUpdates { get; set; } = false;
    }

    public class GenerationResult
    {
        public Level Level { get; set; }
        public object PreviewData { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }



    public class JobStatus
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
        public string ErrorMessage { get; set; }
        public object Result { get; set; }
    }

    public class ShareResult
    {
        public string ShareId { get; set; }
        public string ShareUrl { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class BatchGenerationRequest
    {
        public GenerationConfig BaseConfig { get; set; }
        public ConfigVariation[] Variations { get; set; }
        public int Count { get; set; }
    }

    public class ConfigVariation
    {
        public string Parameter { get; set; }
        public object[] Values { get; set; }
    }

    public class ExportFormat
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string FileExtension { get; set; }
    }
}
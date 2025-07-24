using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Performance;

public class PerformanceTests
{
    private const string BaseUrl = "http://localhost:5000";

    [Fact]
    public async Task GenerationEndpoint_PerformanceTest()
    {
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

        var httpClient = new HttpClient();
        var scenario = Scenario.Create("generation_load_test", async context =>
        {
            var json = JsonSerializer.Serialize(config);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{BaseUrl}/api/generation/generate", content);
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromMinutes(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert performance requirements
        var scnStats = stats.AllScenarioStats.First();
        scnStats.Ok.Request.Mean.Should().BeLessThan(TimeSpan.FromSeconds(2));
        scnStats.Ok.Request.Count.Should().BeGreaterThan(500);
        scnStats.Fail.Request.Count.Should().BeLessThan(10);
    }

    [Fact]
    public async Task ExportEndpoint_PerformanceTest()
    {
        var level = new Level
        {
            Id = "perf-test-level",
            Width = 50,
            Height = 50,
            Terrain = new string[50, 50],
            Entities = new List<Entity>()
        };

        var exportRequest = new ExportRequest
        {
            Level = level,
            Format = "json",
            Options = new Dictionary<string, object>()
        };

        var httpClient = new HttpClient();
        var scenario = Scenario.Create("export_load_test", async context =>
        {
            var json = JsonSerializer.Serialize(exportRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{BaseUrl}/api/export/level", content);
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromMinutes(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert performance requirements
        var scnStats = stats.AllScenarioStats.First();
        scnStats.Ok.Request.Mean.Should().BeLessThan(TimeSpan.FromSeconds(3));
        scnStats.Fail.Request.Count.Should().BeLessThan(5);
    }

    [Fact]
    public async Task ConcurrentUsers_StressTest()
    {
        var config = new GenerationConfig
        {
            Terrain = new TerrainConfig
            {
                Generator = "perlin-noise",
                Width = 30,
                Height = 30,
                Seed = 12345
            }
        };

        var httpClient = new HttpClient();
        var scenario = Scenario.Create("concurrent_users_test", async context =>
        {
            var json = JsonSerializer.Serialize(config);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{BaseUrl}/api/generation/generate", content);
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(2))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert system can handle concurrent load
        var scnStats = stats.AllScenarioStats.First();
        scnStats.Ok.Request.Count.Should().BeGreaterThan(1000);
        scnStats.Fail.Request.Count.Should().BeLessThan(50);
    }

    [Fact]
    public async Task MemoryUsage_LargeLevelGeneration()
    {
        var config = new GenerationConfig
        {
            Terrain = new TerrainConfig
            {
                Generator = "perlin-noise",
                Width = 100,
                Height = 100,
                Seed = 12345
            },
            Entities = new EntityConfig
            {
                Placer = "random",
                Density = 0.2f
            }
        };

        var httpClient = new HttpClient();
        var scenario = Scenario.Create("large_level_test", async context =>
        {
            var json = JsonSerializer.Serialize(config);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{BaseUrl}/api/generation/generate", content);
            
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 2, during: TimeSpan.FromMinutes(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert large levels can be generated without memory issues
        var scnStats = stats.AllScenarioStats.First();
        scnStats.Ok.Request.Mean.Should().BeLessThan(TimeSpan.FromSeconds(10));
        scnStats.Fail.Request.Count.Should().Be(0);
    }
}
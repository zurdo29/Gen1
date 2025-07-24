using System;
using System.Threading.Tasks;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Build;

// Check if this is a build command
if (args.Length > 0 && args[0].ToLower() == "build")
{
    var buildSystem = new BuildSystem();
    var buildTool = new CommandLineBuildTool(buildSystem);
    
    // Remove "build" from args and pass the rest
    var buildArgs = new string[args.Length - 1];
    Array.Copy(args, 1, buildArgs, 0, buildArgs.Length);
    
    return await buildTool.RunAsync(buildArgs);
}

Console.WriteLine("Procedural Mini-game Generator - Configuration Test");
Console.WriteLine("==================================================\n");

try
{
    // Run comprehensive configuration tests
    ConfigurationTest.RunAllTests();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing JSON Configuration Example");
    Console.WriteLine("==================================================\n");

    // Test with example configuration values
    TestExampleConfiguration();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Terrain Generator Interface");
    Console.WriteLine("==================================================\n");

    // Test terrain generator
    TerrainGeneratorTests.RunAllTests();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Perlin Noise Generator");
    Console.WriteLine("==================================================\n");

    // Test Perlin noise generator
    try
    {
        PerlinNoiseGeneratorTests.RunAllTests();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Perlin noise tests failed: {ex.Message}");
        Console.WriteLine("Continuing with other tests...");
    }

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Cellular Automata Generator");
    Console.WriteLine("==================================================\n");

    // Test cellular automata generator
    CellularAutomataGeneratorTests.RunAllTests();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Maze Generator");
    Console.WriteLine("==================================================\n");

    // Test maze generator
    MazeGeneratorTests.RunAllTests();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Running Comprehensive Terrain Generator Tests");
    Console.WriteLine("==================================================\n");

    // Run comprehensive terrain generator tests (Task 3.5)
    try
    {
        TerrainGeneratorTestRunner.RunComprehensiveTests();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Comprehensive terrain tests failed: {ex.Message}");
        Console.WriteLine("Continuing with other tests...");
    }

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Level Assembly and Validation");
    Console.WriteLine("==================================================\n");

    // Run level assembly and validation tests (Task 5.3)
    try
    {
        LevelAssemblerTestRunner.RunTests();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Level assembly tests failed: {ex.Message}");
        Console.WriteLine("Continuing with other tests...");
    }

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Editor Integration");
    Console.WriteLine("==================================================\n");

    // Run editor integration tests (Task 6.1)
    try
    {
        ProceduralMiniGameGenerator.Editor.EditorIntegrationTest.RunAllTests();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Editor integration tests failed: {ex.Message}");
        Console.WriteLine("Continuing with other tests...");
    }

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Level Export Functionality");
    Console.WriteLine("==================================================\n");

    // Run level export tests (Task 7.1)
    try
    {
        ProceduralMiniGameGenerator.Core.LevelExportTest.RunAllTests();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Level export tests failed: {ex.Message}");
        Console.WriteLine("Continuing with other tests...");
    }

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Level Import Functionality");
    Console.WriteLine("==================================================\n");

    // Run level import tests (Task 7.2)
    try
    {
        ProceduralMiniGameGenerator.Core.LevelExportTest.RunAllImportTests();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Level import tests failed: {ex.Message}");
        Console.WriteLine("Continuing with other tests...");
    }

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Running End-to-End Integration Tests");
    Console.WriteLine("==================================================\n");

    // Run comprehensive end-to-end tests (Task 12.1)
    ProceduralMiniGameGenerator.Tests.EndToEndTests.RunAllTests();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Running Performance Tests");
    Console.WriteLine("==================================================\n");

    // Run performance tests (Task 12.2)
    ProceduralMiniGameGenerator.Tests.PerformanceTests.RunAllTests();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Running Usability Tests");
    Console.WriteLine("==================================================\n");

    // Run usability tests (Task 12.3)
    ProceduralMiniGameGenerator.Tests.UsabilityTests.RunAllTests();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Testing Logging Integration");
    Console.WriteLine("==================================================\n");

    // Run logging integration tests (Task 2.3)
    try
    {
        ProceduralMiniGameGenerator.Tests.LoggingIntegrationTest.RunLoggingIntegrationTest();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Logging integration tests failed: {ex.Message}");
        Console.WriteLine("Continuing with other tests...");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error during testing: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
return 0;

static void TestExampleConfiguration()
{
    // Create a configuration similar to the example JSON
    var exampleConfig = new GenerationConfig
    {
        Width = 80,
        Height = 60,
        Seed = 12345,
        GenerationAlgorithm = "perlin",
        AlgorithmParameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "scale", 0.1 },
            { "octaves", 4 },
            { "persistence", 0.5 },
            { "lacunarity", 2.0 },
            { "waterLevel", 0.3 }
        },
        TerrainTypes = new System.Collections.Generic.List<string> { "ground", "wall", "water", "grass" },
        Entities = new System.Collections.Generic.List<EntityConfig>
        {
            new EntityConfig
            {
                Type = EntityType.Enemy,
                Count = 5,
                MinDistance = 3.0f,
                MaxDistanceFromPlayer = 50.0f,
                PlacementStrategy = "random",
                Properties = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "health", 50 },
                    { "damage", 10 },
                    { "speed", 2.0 }
                }
            },
            new EntityConfig
            {
                Type = EntityType.Item,
                Count = 8,
                MinDistance = 2.0f,
                MaxDistanceFromPlayer = 100.0f,
                PlacementStrategy = "spread",
                Properties = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "itemType", "health_potion" },
                    { "value", 25 }
                }
            },
            new EntityConfig
            {
                Type = EntityType.PowerUp,
                Count = 3,
                MinDistance = 5.0f,
                MaxDistanceFromPlayer = 80.0f,
                PlacementStrategy = "clustered",
                Properties = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "points", 100 },
                    { "rarity", "common" }
                }
            }
        },
        VisualTheme = new VisualThemeConfig
        {
            ThemeName = "forest",
            ColorPalette = new System.Collections.Generic.Dictionary<string, string>
            {
                { "ground", "#8B4513" },
                { "wall", "#654321" },
                { "water", "#4169E1" },
                { "grass", "#228B22" },
                { "player", "#FFD700" },
                { "enemy", "#DC143C" },
                { "item", "#32CD32" }
            },
            TileSprites = new System.Collections.Generic.Dictionary<string, string>
            {
                { "ground", "sprites/tiles/ground_forest.png" },
                { "wall", "sprites/tiles/wall_stone.png" },
                { "water", "sprites/tiles/water_blue.png" },
                { "grass", "sprites/tiles/grass_green.png" }
            },
            EntitySprites = new System.Collections.Generic.Dictionary<string, string>
            {
                { "player", "sprites/entities/player_knight.png" },
                { "enemy", "sprites/entities/goblin.png" },
                { "item", "sprites/entities/potion.png" },
                { "powerup", "sprites/entities/chest.png" }
            },
            EffectSettings = new System.Collections.Generic.Dictionary<string, object>
            {
                { "enableParticles", true },
                { "ambientLighting", 0.8 },
                { "shadowIntensity", 0.6 }
            }
        },
        Gameplay = new GameplayConfig
        {
            PlayerSpeed = 6.0f,
            PlayerHealth = 150,
            Difficulty = "normal",
            TimeLimit = 300.0f,
            VictoryConditions = new System.Collections.Generic.List<string> { "reach_exit", "collect_all_items" },
            Mechanics = new System.Collections.Generic.Dictionary<string, object>
            {
                { "enableJumping", false },
                { "enableShooting", true },
                { "respawnOnDeath", true },
                { "checkpointSystem", true }
            }
        }
    };

    Console.WriteLine("Validating example configuration...");
    var result = ConfigurationValidator.ValidateConfiguration(exampleConfig);
    
    Console.WriteLine($"Validation Result:");
    Console.WriteLine($"  Valid: {result.IsValid}");
    Console.WriteLine($"  Errors: {result.Errors.Count}");
    Console.WriteLine($"  Warnings: {result.Warnings.Count}");

    if (result.Errors.Count > 0 || result.Warnings.Count > 0)
    {
        Console.WriteLine("\nDetailed Results:");
        Console.WriteLine(result.GetSummary());
    }
    else
    {
        Console.WriteLine("✓ Example configuration is completely valid!");
    }
}
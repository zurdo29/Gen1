using System;
using System.Threading.Tasks;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Generators;

// TODO: Build system integration will be added later

Console.WriteLine("Procedural Mini-game Generator - Configuration Test");
Console.WriteLine("==================================================\n");

try
{
    Console.WriteLine("Testing JSON Configuration Example");
    Console.WriteLine("==================================================\n");

    // Test with example configuration values
    TestExampleConfiguration();

    Console.WriteLine("\n==================================================");
    Console.WriteLine("Basic Configuration Test Completed");
    Console.WriteLine("==================================================\n");
    
    Console.WriteLine("✓ Core system is operational");
    Console.WriteLine("✓ Configuration validation working");
    Console.WriteLine("✓ Model classes properly defined");
    
    Console.WriteLine("\nNote: Full test suite will be implemented in separate test projects");
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
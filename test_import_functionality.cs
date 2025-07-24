using System;
using System.IO;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

/// <summary>
/// Simple test runner to verify import functionality
/// </summary>
public class ImportFunctionalityTest
{
    public static void Main()
    {
        Console.WriteLine("=== Testing Level Import Functionality ===\n");
        
        try
        {
            TestBasicImportFunctionality();
            Console.WriteLine("\n✓ All import functionality tests passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Import functionality test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    private static void TestBasicImportFunctionality()
    {
        Console.WriteLine("1. Testing basic export/import roundtrip...");
        
        var exportService = new LevelExportService();
        
        // Create a test level
        var level = new Level
        {
            Name = "Test Import Level",
            Terrain = new TileMap(10, 8)
        };
        
        // Fill terrain with a simple pattern
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (x == 0 || y == 0 || x == 9 || y == 7)
                    level.Terrain.SetTile(x, y, TileType.Wall);
                else
                    level.Terrain.SetTile(x, y, TileType.Ground);
            }
        }
        
        // Add entities
        level.Entities.Add(new PlayerEntity { Position = new System.Numerics.Vector2(2, 2) });
        level.Entities.Add(new EnemyEntity { Position = new System.Numerics.Vector2(5, 4) });
        level.Entities.Add(new ItemEntity { Position = new System.Numerics.Vector2(7, 6) });
        
        // Add metadata
        level.Metadata["test_property"] = "test_value";
        level.Metadata["numeric_property"] = 123;
        
        // Create generation config
        var config = new GenerationConfig
        {
            Width = 10,
            Height = 8,
            Seed = 54321,
            GenerationAlgorithm = "test"
        };
        
        // Export to JSON
        Console.WriteLine("   Exporting level to JSON...");
        var json = exportService.ExportLevelToJson(level, config);
        Console.WriteLine($"   JSON length: {json.Length} characters");
        
        // Import from JSON
        Console.WriteLine("   Importing level from JSON...");
        var importResult = exportService.ImportLevelFromJson(json);
        
        if (!importResult.Success)
        {
            throw new Exception($"Import failed: {string.Join(", ", importResult.Errors)}");
        }
        
        var importedLevel = importResult.Level!;
        
        // Verify basic properties
        Console.WriteLine("   Verifying imported level properties...");
        
        if (importedLevel.Name != level.Name)
            throw new Exception($"Name mismatch: expected '{level.Name}', got '{importedLevel.Name}'");
        
        if (importedLevel.Terrain.Width != level.Terrain.Width)
            throw new Exception($"Width mismatch: expected {level.Terrain.Width}, got {importedLevel.Terrain.Width}");
        
        if (importedLevel.Terrain.Height != level.Terrain.Height)
            throw new Exception($"Height mismatch: expected {level.Terrain.Height}, got {importedLevel.Terrain.Height}");
        
        if (importedLevel.Entities.Count != level.Entities.Count)
            throw new Exception($"Entity count mismatch: expected {level.Entities.Count}, got {importedLevel.Entities.Count}");
        
        // Verify terrain data
        Console.WriteLine("   Verifying terrain data...");
        for (int x = 0; x < level.Terrain.Width; x++)
        {
            for (int y = 0; y < level.Terrain.Height; y++)
            {
                var originalTile = level.Terrain.GetTile(x, y);
                var importedTile = importedLevel.Terrain.GetTile(x, y);
                
                if (originalTile != importedTile)
                    throw new Exception($"Tile mismatch at ({x},{y}): expected {originalTile}, got {importedTile}");
            }
        }
        
        // Verify generation config
        Console.WriteLine("   Verifying generation config...");
        if (importResult.GenerationConfig == null)
            throw new Exception("Generation config was not imported");
        
        if (importResult.GenerationConfig.Width != config.Width)
            throw new Exception($"Config width mismatch: expected {config.Width}, got {importResult.GenerationConfig.Width}");
        
        if (importResult.GenerationConfig.Seed != config.Seed)
            throw new Exception($"Config seed mismatch: expected {config.Seed}, got {importResult.GenerationConfig.Seed}");
        
        Console.WriteLine("   ✓ Basic import functionality works correctly");
        
        // Test file-based import
        Console.WriteLine("\n2. Testing file-based import...");
        var filePath = "test_import_file.json";
        
        try
        {
            // Export to file
            Console.WriteLine("   Exporting to file...");
            var exportResult = exportService.ExportLevel(level, config, filePath);
            
            if (!exportResult.Success)
                throw new Exception($"Export to file failed: {string.Join(", ", exportResult.Errors)}");
            
            // Import from file
            Console.WriteLine("   Importing from file...");
            var fileImportResult = exportService.ImportLevel(filePath);
            
            if (!fileImportResult.Success)
                throw new Exception($"Import from file failed: {string.Join(", ", fileImportResult.Errors)}");
            
            if (fileImportResult.Level?.Name != level.Name)
                throw new Exception("File import level name mismatch");
            
            Console.WriteLine($"   ✓ File-based import works correctly (import time: {fileImportResult.ImportTime.TotalMilliseconds:F0}ms)");
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        
        // Test validation
        Console.WriteLine("\n3. Testing import validation...");
        
        // Test invalid JSON
        var invalidResult = exportService.ImportLevelFromJson("{ invalid json }");
        if (invalidResult.Success)
            throw new Exception("Should have failed with invalid JSON");
        
        Console.WriteLine("   ✓ Invalid JSON handling works correctly");
        
        // Test validation of exported file
        var validationPath = "test_validation_file.json";
        try
        {
            exportService.ExportLevel(level, config, validationPath);
            var validationResult = exportService.ValidateExportedLevel(validationPath);
            
            if (validationResult.Errors.Count > 0)
                throw new Exception($"Validation found unexpected errors: {string.Join(", ", validationResult.Errors)}");
            
            Console.WriteLine("   ✓ Export validation works correctly");
        }
        finally
        {
            if (File.Exists(validationPath))
                File.Delete(validationPath);
        }
    }
}
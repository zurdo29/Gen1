using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Test class for level export functionality
    /// </summary>
    public static class LevelExportTest
    {
        /// <summary>
        /// Runs all export functionality tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Level Export Functionality Tests ===");
            
            try
            {
                TestBasicExport();
                TestExportWithGenerationConfig();
                TestExportToFile();
                TestExportValidation();
                TestExportStatistics();
                TestExportMetadata();
                TestLargeLevel();
                
                Console.WriteLine("✓ All export tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Export tests failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Runs all import functionality tests
        /// </summary>
        public static void RunAllImportTests()
        {
            Console.WriteLine("=== Level Import Functionality Tests ===");
            
            try
            {
                TestBasicImport();
                TestRoundtripExportImport();
                TestImportFromFile();
                TestImportValidation();
                TestImportWithInvalidData();
                TestImportWithMissingData();
                TestImportValidationResult();
                
                Console.WriteLine("✓ All import tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Import tests failed: {ex.Message}");
                throw;
            }
        }
        
        private static void TestBasicExport()
        {
            Console.WriteLine("Testing basic level export...");
            
            var level = CreateTestLevel();
            var exportService = new LevelExportService();
            
            var json = exportService.ExportLevelToJson(level, new GenerationConfig());
            
            if (string.IsNullOrEmpty(json))
                throw new Exception("Export returned empty JSON");
            
            // Verify JSON structure
            var exportData = JsonSerializer.Deserialize<LevelExportData>(json);
            if (exportData == null)
                throw new Exception("Failed to deserialize exported JSON");
            
            if (exportData.Level.Name != level.Name)
                throw new Exception($"Level name mismatch: expected '{level.Name}', got '{exportData.Level.Name}'");
            
            if (exportData.Level.Width != level.Terrain.Width)
                throw new Exception($"Width mismatch: expected {level.Terrain.Width}, got {exportData.Level.Width}");
            
            if (exportData.Level.Height != level.Terrain.Height)
                throw new Exception($"Height mismatch: expected {level.Terrain.Height}, got {exportData.Level.Height}");
            
            Console.WriteLine("✓ Basic export works correctly");
        }
        
        private static void TestExportWithGenerationConfig()
        {
            Console.WriteLine("Testing export with generation configuration...");
            
            var level = CreateTestLevel();
            var config = CreateTestGenerationConfig();
            var exportService = new LevelExportService();
            
            var json = exportService.ExportLevelToJson(level, config);
            var exportData = JsonSerializer.Deserialize<LevelExportData>(json);
            
            if (exportData?.GenerationConfig == null)
                throw new Exception("Generation config was not included in export");
            
            if (exportData.GenerationConfig.Width != config.Width)
                throw new Exception("Generation config width mismatch");
            
            if (exportData.GenerationConfig.GenerationAlgorithm != config.GenerationAlgorithm)
                throw new Exception("Generation algorithm mismatch");
            
            Console.WriteLine("✓ Export with generation config works correctly");
        }
        
        private static void TestExportToFile()
        {
            Console.WriteLine("Testing export to file...");
            
            var level = CreateTestLevel();
            var config = CreateTestGenerationConfig();
            var exportService = new LevelExportService();
            var outputPath = "test_export.json";
            
            try
            {
                var result = exportService.ExportLevel(level, config, outputPath);
                
                if (!result.Success)
                    throw new Exception($"Export failed: {string.Join(", ", result.Errors)}");
                
                if (!File.Exists(outputPath))
                    throw new Exception("Export file was not created");
                
                if (result.FileSize <= 0)
                    throw new Exception("Export file size is invalid");
                
                // Verify file content
                var fileContent = File.ReadAllText(outputPath);
                var exportData = JsonSerializer.Deserialize<LevelExportData>(fileContent);
                
                if (exportData?.Level.Name != level.Name)
                    throw new Exception("Exported file content is invalid");
                
                Console.WriteLine($"✓ Export to file works correctly (size: {result.FileSize} bytes)");
            }
            finally
            {
                // Clean up
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }
        
        private static void TestExportValidation()
        {
            Console.WriteLine("Testing export validation...");
            
            var exportService = new LevelExportService();
            
            // Test null level
            try
            {
                exportService.ExportLevelToJson(null, null);
                throw new Exception("Should have thrown exception for null level");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
            
            // Test empty output path
            var level = CreateTestLevel();
            var result = exportService.ExportLevel(level, null, "");
            
            if (result.Success)
                throw new Exception("Should have failed with empty output path");
            
            if (result.Errors.Count == 0)
                throw new Exception("Should have reported errors for empty output path");
            
            Console.WriteLine("✓ Export validation works correctly");
        }
        
        private static void TestExportStatistics()
        {
            Console.WriteLine("Testing export statistics calculation...");
            
            var level = CreateTestLevel();
            var exportService = new LevelExportService();
            
            var json = exportService.ExportLevelToJson(level, new GenerationConfig());
            var exportData = JsonSerializer.Deserialize<LevelExportData>(json);
            
            if (exportData?.Statistics == null)
                throw new Exception("Statistics were not calculated");
            
            var stats = exportData.Statistics;
            
            if (stats.TotalTiles != level.Terrain.Width * level.Terrain.Height)
                throw new Exception($"Total tiles mismatch: expected {level.Terrain.Width * level.Terrain.Height}, got {stats.TotalTiles}");
            
            if (stats.TotalEntities != level.Entities.Count)
                throw new Exception($"Total entities mismatch: expected {level.Entities.Count}, got {stats.TotalEntities}");
            
            if (stats.NavigabilityRatio < 0 || stats.NavigabilityRatio > 1)
                throw new Exception($"Invalid navigability ratio: {stats.NavigabilityRatio}");
            
            Console.WriteLine($"✓ Statistics calculation works correctly (navigability: {stats.NavigabilityRatio:P1})");
        }
        
        private static void TestExportMetadata()
        {
            Console.WriteLine("Testing export metadata handling...");
            
            var level = CreateTestLevel();
            level.Metadata["custom_property"] = "test_value";
            level.Metadata["numeric_property"] = 42;
            
            var exportService = new LevelExportService();
            var json = exportService.ExportLevelToJson(level, new GenerationConfig());
            var exportData = JsonSerializer.Deserialize<LevelExportData>(json);
            
            if (exportData?.Level.Metadata == null)
                throw new Exception("Metadata was not exported");
            
            if (!exportData.Level.Metadata.ContainsKey("custom_property"))
                throw new Exception("Custom metadata property was not exported");
            
            Console.WriteLine("✓ Metadata handling works correctly");
        }
        
        private static void TestLargeLevel()
        {
            Console.WriteLine("Testing large level export performance...");
            
            var level = CreateLargeTestLevel();
            var exportService = new LevelExportService();
            
            var startTime = DateTime.Now;
            var json = exportService.ExportLevelToJson(level, new GenerationConfig());
            var endTime = DateTime.Now;
            
            var exportTime = endTime - startTime;
            
            if (string.IsNullOrEmpty(json))
                throw new Exception("Large level export failed");
            
            Console.WriteLine($"✓ Large level export completed in {exportTime.TotalMilliseconds:F0}ms");
        }
        
        private static Level CreateTestLevel()
        {
            var level = new Level
            {
                Name = "Test Level",
                Terrain = new TileMap(20, 15)
            };
            
            // Fill terrain with a simple pattern
            for (int x = 0; x < 20; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    if (x == 0 || y == 0 || x == 19 || y == 14)
                        level.Terrain.SetTile(x, y, TileType.Wall);
                    else if (x % 3 == 0 && y % 3 == 0)
                        level.Terrain.SetTile(x, y, TileType.Water);
                    else
                        level.Terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            // Add some entities
            level.Entities.Add(new PlayerEntity { Position = new System.Numerics.Vector2(5, 5) });
            level.Entities.Add(new EnemyEntity { Position = new System.Numerics.Vector2(10, 8) });
            level.Entities.Add(new ItemEntity { Position = new System.Numerics.Vector2(15, 10) });
            level.Entities.Add(new ExitEntity { Position = new System.Numerics.Vector2(18, 13) });
            
            // Add metadata
            level.Metadata["difficulty"] = "normal";
            level.Metadata["theme"] = "dungeon";
            
            return level;
        }
        
        private static Level CreateLargeTestLevel()
        {
            var level = new Level
            {
                Name = "Large Test Level",
                Terrain = new TileMap(200, 150)
            };
            
            // Fill terrain with a pattern
            var random = new Random(12345);
            for (int x = 0; x < 200; x++)
            {
                for (int y = 0; y < 150; y++)
                {
                    var tileType = random.NextDouble() switch
                    {
                        < 0.1 => TileType.Wall,
                        < 0.15 => TileType.Water,
                        < 0.2 => TileType.Grass,
                        _ => TileType.Ground
                    };
                    level.Terrain.SetTile(x, y, tileType);
                }
            }
            
            // Add many entities
            for (int i = 0; i < 100; i++)
            {
                var x = random.Next(1, 199);
                var y = random.Next(1, 149);
                
                if (level.Terrain.IsWalkable(x, y))
                {
                    var entityType = (EntityType)(i % 6);
                    var entity = CreateEntityFromType(entityType);
                    entity.Position = new System.Numerics.Vector2(x, y);
                    level.Entities.Add(entity);
                }
            }
            
            return level;
        }
        
        private static GenerationConfig CreateTestGenerationConfig()
        {
            return new GenerationConfig
            {
                Width = 20,
                Height = 15,
                Seed = 12345,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>
                {
                    { "scale", 0.1 },
                    { "octaves", 4 }
                },
                TerrainTypes = new List<string> { "ground", "wall", "water" },
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Player, Count = 1 },
                    new EntityConfig { Type = EntityType.Enemy, Count = 2 },
                    new EntityConfig { Type = EntityType.Item, Count = 3 }
                }
            };
        }
        
        private static Entity CreateEntityFromType(EntityType entityType)
        {
            return entityType switch
            {
                EntityType.Player => new PlayerEntity(),
                EntityType.Enemy => new EnemyEntity(),
                EntityType.Item => new ItemEntity(),
                EntityType.PowerUp => new PowerUpEntity(),
                EntityType.Checkpoint => new CheckpointEntity(),
                EntityType.Exit => new ExitEntity(),
                _ => new GenericEntity(entityType)
            };
        }
        
        private static void TestBasicImport()
        {
            Console.WriteLine("Testing basic level import...");
            
            var originalLevel = CreateTestLevel();
            var config = CreateTestGenerationConfig();
            var exportService = new LevelExportService();
            
            // Export level to JSON
            var json = exportService.ExportLevelToJson(originalLevel, config);
            
            // Import level from JSON
            var importResult = exportService.ImportLevelFromJson(json);
            
            if (!importResult.Success)
                throw new Exception($"Import failed: {string.Join(", ", importResult.Errors)}");
            
            if (importResult.Level == null)
                throw new Exception("Imported level is null");
            
            // Verify basic properties
            if (importResult.Level.Name != originalLevel.Name)
                throw new Exception($"Level name mismatch: expected '{originalLevel.Name}', got '{importResult.Level.Name}'");
            
            if (importResult.Level.Terrain.Width != originalLevel.Terrain.Width)
                throw new Exception($"Width mismatch: expected {originalLevel.Terrain.Width}, got {importResult.Level.Terrain.Width}");
            
            if (importResult.Level.Terrain.Height != originalLevel.Terrain.Height)
                throw new Exception($"Height mismatch: expected {originalLevel.Terrain.Height}, got {importResult.Level.Terrain.Height}");
            
            Console.WriteLine("✓ Basic import works correctly");
        }
        
        private static void TestRoundtripExportImport()
        {
            Console.WriteLine("Testing roundtrip export and import...");
            
            var originalLevel = CreateTestLevel();
            var config = CreateTestGenerationConfig();
            var exportService = new LevelExportService();
            
            // Export and import
            var json = exportService.ExportLevelToJson(originalLevel, config);
            var importResult = exportService.ImportLevelFromJson(json);
            
            if (!importResult.Success)
                throw new Exception($"Roundtrip failed: {string.Join(", ", importResult.Errors)}");
            
            var importedLevel = importResult.Level!;
            
            // Verify terrain data
            for (int x = 0; x < originalLevel.Terrain.Width; x++)
            {
                for (int y = 0; y < originalLevel.Terrain.Height; y++)
                {
                    var originalTile = originalLevel.Terrain.GetTile(x, y);
                    var importedTile = importedLevel.Terrain.GetTile(x, y);
                    
                    if (originalTile != importedTile)
                        throw new Exception($"Tile mismatch at ({x},{y}): expected {originalTile}, got {importedTile}");
                }
            }
            
            // Verify entity count
            if (importedLevel.Entities.Count != originalLevel.Entities.Count)
                throw new Exception($"Entity count mismatch: expected {originalLevel.Entities.Count}, got {importedLevel.Entities.Count}");
            
            // Verify generation config
            if (importResult.GenerationConfig == null)
                throw new Exception("Generation config was not imported");
            
            if (importResult.GenerationConfig.Width != config.Width)
                throw new Exception("Generation config width mismatch");
            
            Console.WriteLine("✓ Roundtrip export/import works correctly");
        }
        
        private static void TestImportFromFile()
        {
            Console.WriteLine("Testing import from file...");
            
            var level = CreateTestLevel();
            var config = CreateTestGenerationConfig();
            var exportService = new LevelExportService();
            var filePath = "test_import.json";
            
            try
            {
                // Export to file
                var exportResult = exportService.ExportLevel(level, config, filePath);
                if (!exportResult.Success)
                    throw new Exception($"Export failed: {string.Join(", ", exportResult.Errors)}");
                
                // Import from file
                var importResult = exportService.ImportLevel(filePath);
                
                if (!importResult.Success)
                    throw new Exception($"Import from file failed: {string.Join(", ", importResult.Errors)}");
                
                if (importResult.Level == null)
                    throw new Exception("Imported level is null");
                
                if (importResult.Level.Name != level.Name)
                    throw new Exception("Level name mismatch after file import");
                
                Console.WriteLine($"✓ Import from file works correctly (import time: {importResult.ImportTime.TotalMilliseconds:F0}ms)");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }
        
        private static void TestImportValidation()
        {
            Console.WriteLine("Testing import validation...");
            
            var exportService = new LevelExportService();
            
            // Test null/empty JSON
            var result1 = exportService.ImportLevelFromJson(null);
            if (result1.Success)
                throw new Exception("Should have failed with null JSON");
            
            var result2 = exportService.ImportLevelFromJson("");
            if (result2.Success)
                throw new Exception("Should have failed with empty JSON");
            
            // Test non-existent file
            var result3 = exportService.ImportLevel("non_existent_file.json");
            if (result3.Success)
                throw new Exception("Should have failed with non-existent file");
            
            Console.WriteLine("✓ Import validation works correctly");
        }
        
        private static void TestImportWithInvalidData()
        {
            Console.WriteLine("Testing import with invalid JSON data...");
            
            var exportService = new LevelExportService();
            
            // Test invalid JSON syntax
            var result1 = exportService.ImportLevelFromJson("{ invalid json }");
            if (result1.Success)
                throw new Exception("Should have failed with invalid JSON syntax");
            
            // Test valid JSON but wrong structure
            var result2 = exportService.ImportLevelFromJson("{\"someProperty\": \"value\"}");
            if (result2.Success)
                throw new Exception("Should have failed with wrong JSON structure");
            
            Console.WriteLine("✓ Invalid data handling works correctly");
        }
        
        private static void TestImportWithMissingData()
        {
            Console.WriteLine("Testing import with missing data fields...");
            
            var exportService = new LevelExportService();
            
            // Create minimal valid JSON with missing optional fields
            var minimalJson = @"{
                ""formatVersion"": ""1.0"",
                ""exportTimestamp"": ""2024-01-01T00:00:00Z"",
                ""level"": {
                    ""name"": ""Minimal Level"",
                    ""width"": 10,
                    ""height"": 10,
                    ""terrain"": [[0,0,0,0,0,0,0,0,0,0],[0,1,1,1,1,1,1,1,1,0],[0,1,0,0,0,0,0,0,1,0],[0,1,0,0,0,0,0,0,1,0],[0,1,0,0,0,0,0,0,1,0],[0,1,0,0,0,0,0,0,1,0],[0,1,0,0,0,0,0,0,1,0],[0,1,0,0,0,0,0,0,1,0],[0,1,1,1,1,1,1,1,1,0],[0,0,0,0,0,0,0,0,0,0]],
                    ""entities"": [],
                    ""metadata"": {}
                },
                ""statistics"": {
                    ""totalTiles"": 100,
                    ""walkableTiles"": 64,
                    ""wallTiles"": 36,
                    ""waterTiles"": 0,
                    ""totalEntities"": 0,
                    ""playerCount"": 0,
                    ""enemyCount"": 0,
                    ""itemCount"": 0
                }
            }";
            
            var result = exportService.ImportLevelFromJson(minimalJson);
            
            if (!result.Success)
                throw new Exception($"Import with minimal data failed: {string.Join(", ", result.Errors)}");
            
            if (result.Level == null)
                throw new Exception("Level was not imported");
            
            if (result.Level.Name != "Minimal Level")
                throw new Exception("Level name was not imported correctly");
            
            // Should have warnings about missing generation config
            if (result.Warnings.Count == 0)
                Console.WriteLine("  Note: No warnings generated for missing optional data");
            
            Console.WriteLine("✓ Missing data handling works correctly");
        }
        
        private static void TestImportValidationResult()
        {
            Console.WriteLine("Testing import validation result...");
            
            var level = CreateTestLevel();
            var config = CreateTestGenerationConfig();
            var exportService = new LevelExportService();
            var filePath = "test_validation.json";
            
            try
            {
                // Export to file
                var exportResult = exportService.ExportLevel(level, config, filePath);
                if (!exportResult.Success)
                    throw new Exception("Export failed");
                
                // Validate exported file
                var validationResult = exportService.ValidateExportedLevel(filePath);
                
                if (validationResult.Errors.Count > 0)
                    throw new Exception($"Validation found errors: {string.Join(", ", validationResult.Errors)}");
                
                Console.WriteLine("✓ Import validation result works correctly");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }
        
        /// <summary>
        /// Demonstrates the export functionality with various scenarios
        /// </summary>
        public static void DemonstrateExportFunctionality()
        {
            Console.WriteLine("\n=== Export Functionality Demonstration ===");
            
            var level = CreateTestLevel();
            var config = CreateTestGenerationConfig();
            var exportService = new LevelExportService();
            
            Console.WriteLine("1. Basic JSON export:");
            var json = exportService.ExportLevelToJson(level, config);
            Console.WriteLine($"   JSON length: {json.Length:N0} characters");
            
            Console.WriteLine("\n2. Export to file:");
            var result = exportService.ExportLevel(level, config, "demo_export.json");
            Console.WriteLine($"   Success: {result.Success}");
            Console.WriteLine($"   File size: {result.FileSize:N0} bytes");
            Console.WriteLine($"   Export time: {result.ExportTime.TotalMilliseconds:F0}ms");
            
            Console.WriteLine("\n3. Export statistics:");
            var exportData = JsonSerializer.Deserialize<LevelExportData>(json);
            var stats = exportData?.Statistics;
            if (stats != null)
            {
                Console.WriteLine($"   Total tiles: {stats.TotalTiles:N0}");
                Console.WriteLine($"   Walkable tiles: {stats.WalkableTiles:N0}");
                Console.WriteLine($"   Total entities: {stats.TotalEntities}");
                Console.WriteLine($"   Navigability: {stats.NavigabilityRatio:P1}");
                Console.WriteLine($"   Entity density: {stats.EntityDensity:F1} per 100 tiles");
            }
            
            Console.WriteLine("\n4. Supported formats:");
            var formats = exportService.GetSupportedFormats();
            Console.WriteLine($"   {string.Join(", ", formats)}");
            
            // Clean up
            if (File.Exists("demo_export.json"))
                File.Delete("demo_export.json");
            
            Console.WriteLine("\n=== End Demonstration ===\n");
        }
        
        /// <summary>
        /// Demonstrates the import functionality with various scenarios
        /// </summary>
        public static void DemonstrateImportFunctionality()
        {
            Console.WriteLine("\n=== Import Functionality Demonstration ===");
            
            var level = CreateTestLevel();
            var config = CreateTestGenerationConfig();
            var exportService = new LevelExportService();
            
            Console.WriteLine("1. Export and import roundtrip:");
            var json = exportService.ExportLevelToJson(level, config);
            var importResult = exportService.ImportLevelFromJson(json);
            Console.WriteLine($"   Import success: {importResult.Success}");
            Console.WriteLine($"   Import time: {importResult.ImportTime.TotalMilliseconds:F0}ms");
            Console.WriteLine($"   Warnings: {importResult.Warnings.Count}");
            
            Console.WriteLine("\n2. File-based import:");
            var filePath = "demo_import.json";
            try
            {
                var exportResult = exportService.ExportLevel(level, config, filePath);
                var fileImportResult = exportService.ImportLevel(filePath);
                Console.WriteLine($"   File import success: {fileImportResult.Success}");
                Console.WriteLine($"   Level name: {fileImportResult.Level?.Name}");
                Console.WriteLine($"   Terrain size: {fileImportResult.Level?.Terrain.Width}x{fileImportResult.Level?.Terrain.Height}");
                Console.WriteLine($"   Entity count: {fileImportResult.Level?.Entities.Count}");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            
            Console.WriteLine("\n3. Validation of exported file:");
            var validationPath = "demo_validation.json";
            try
            {
                exportService.ExportLevel(level, config, validationPath);
                var validationResult = exportService.ValidateExportedLevel(validationPath);
                Console.WriteLine($"   Validation errors: {validationResult.Errors.Count}");
                Console.WriteLine($"   Validation warnings: {validationResult.Warnings.Count}");
            }
            finally
            {
                if (File.Exists(validationPath))
                    File.Delete(validationPath);
            }
            
            Console.WriteLine("\n=== End Import Demonstration ===\n");
        }
    }
}
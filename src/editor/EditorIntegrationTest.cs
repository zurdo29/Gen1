using System;
using System.Collections.Generic;
using System.IO;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Editor
{
    /// <summary>
    /// Test class for editor integration functionality
    /// </summary>
    public static class EditorIntegrationTest
    {
        /// <summary>
        /// Runs all editor integration tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Editor Integration Tests ===");
            
            try
            {
                TestEditorIntegrationCreation();
                TestCommandRegistration();
                TestConfigurationFileSelection();
                TestErrorReporting();
                TestLevelDisplay();
                TestGenerationWindow();
                
                Console.WriteLine("✓ All editor integration tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Editor integration tests failed: {ex.Message}");
                throw;
            }
        }
        
        private static void TestEditorIntegrationCreation()
        {
            Console.WriteLine("Testing editor integration creation...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            if (editorIntegration == null)
                throw new Exception("Failed to create editor integration");
            
            Console.WriteLine("✓ Editor integration created successfully");
        }
        
        private static void TestCommandRegistration()
        {
            Console.WriteLine("Testing command registration...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            // This should not throw an exception
            editorIntegration.RegisterEditorCommands();
            
            Console.WriteLine("✓ Commands registered successfully");
        }
        
        private static void TestConfigurationFileSelection()
        {
            Console.WriteLine("Testing configuration file selection...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            // Create a test config file
            var testConfigPath = "test_config.json";
            var testConfig = new GenerationConfig
            {
                Width = 20,
                Height = 15,
                Seed = 123,
                GenerationAlgorithm = "test"
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(testConfig, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(testConfigPath, json);
            
            try
            {
                var selectedPath = editorIntegration.SelectConfigurationFile();
                
                if (string.IsNullOrEmpty(selectedPath))
                    throw new Exception("No configuration file was selected");
                
                Console.WriteLine($"✓ Configuration file selected: {Path.GetFileName(selectedPath)}");
            }
            finally
            {
                // Clean up test file
                if (File.Exists(testConfigPath))
                    File.Delete(testConfigPath);
            }
        }
        
        private static void TestErrorReporting()
        {
            Console.WriteLine("Testing error reporting...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            var testErrors = new List<string>
            {
                "Test error 1",
                "Test error 2",
                "Test error 3"
            };
            
            // This should not throw an exception
            editorIntegration.ReportErrors(testErrors);
            
            var recentErrors = editorIntegration.GetRecentErrors();
            if (recentErrors.Count != testErrors.Count)
                throw new Exception($"Expected {testErrors.Count} errors, got {recentErrors.Count}");
            
            Console.WriteLine("✓ Error reporting works correctly");
        }
        
        private static void TestLevelDisplay()
        {
            Console.WriteLine("Testing level display...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            // Create a test level
            var testLevel = new Level
            {
                Name = "Test Level",
                Terrain = new TileMap(10, 8),
                Entities = new List<Entity>
                {
                    new EnemyEntity
                    {
                        Position = new System.Numerics.Vector2(5, 4),
                        Properties = new Dictionary<string, object>()
                    }
                }
            };
            
            // Fill terrain with some basic tiles
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (x == 0 || y == 0 || x == 9 || y == 7)
                        testLevel.Terrain.SetTile(x, y, TileType.Wall);
                    else
                        testLevel.Terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            // This should not throw an exception
            editorIntegration.DisplayGeneratedLevel(testLevel);
            editorIntegration.ShowLevelPreview(testLevel);
            
            Console.WriteLine("✓ Level display works correctly");
        }
        
        private static void TestGenerationWindow()
        {
            Console.WriteLine("Testing generation window...");
            
            var mockGenerationManager = new MockGenerationManager();
            var editorIntegration = new EditorIntegration(mockGenerationManager);
            
            // This should not throw an exception
            editorIntegration.ShowGenerationWindow();
            
            Console.WriteLine("✓ Generation window created successfully");
        }
    }
    
    /// <summary>
    /// Mock implementation of IGenerationManager for testing
    /// </summary>
    public class MockGenerationManager : IGenerationManager
    {
        public Level GenerateLevel(GenerationConfig config)
        {
            var level = new Level
            {
                Name = "Mock Generated Level",
                Terrain = new TileMap(config.Width, config.Height),
                Entities = new List<Entity>()
            };
            
            // Fill with basic terrain
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    level.Terrain.SetTile(x, y, x == 0 || y == 0 || x == config.Width - 1 || y == config.Height - 1 
                        ? TileType.Wall : TileType.Ground);
                }
            }
            
            return level;
        }
        
        public void SetSeed(int seed)
        {
            // Mock implementation
        }
        
        public void RegisterGenerationAlgorithm(string name, Generators.ITerrainGenerator generator)
        {
            // Mock implementation
        }
        
        public void RegisterEntityPlacer(string name, IEntityPlacer placer)
        {
            // Mock implementation
        }
        
        public List<string> GetAvailableAlgorithms()
        {
            return new List<string> { "mock", "test" };
        }
        
        public List<string> GetAvailablePlacementStrategies()
        {
            return new List<string> { "random", "spread" };
        }
        
        public ValidationResult ValidateGenerationConfig(GenerationConfig config)
        {
            return new ValidationResult(); // Empty result means IsValid = true
        }
    }
}
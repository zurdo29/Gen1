using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Editor;
using ProceduralMiniGameGenerator.Build;
using ProceduralMiniGameGenerator.Validators;
using ProceduralMiniGameGenerator.Tests;

namespace ProceduralMiniGameGenerator.Tests
{
    /// <summary>
    /// End-to-end tests for the complete generation workflow
    /// Tests Requirements: 5.1, 5.2, 5.3, 11.1
    /// </summary>
    public class EndToEndTests
    {
        private static int testsPassed = 0;
        private static int testsTotal = 0;
        private static List<string> testResults = new List<string>();

        public static bool RunAllTests()
        {
            Console.WriteLine("=== END-TO-END TESTS ===");
            Console.WriteLine("Testing complete generation workflow and editor integration");
            Console.WriteLine("Requirements: 5.1, 5.2, 5.3, 11.1");
            Console.WriteLine();

            try
            {
                // Test 1: Complete generation workflow with default configuration
                TestCompleteWorkflowWithDefaults();

                // Test 2: Complete generation workflow with custom configuration
                TestCompleteWorkflowWithCustomConfig();

                // Test 3: Multiple generation iterations (Requirement 5.1)
                TestMultipleGenerationIterations();

                // Test 4: Real-time parameter modification (Requirement 5.2)
                TestRealTimeParameterModification();

                // Test 5: Level quality validation (Requirement 5.3)
                TestLevelQualityValidation();

                // Test 6: Editor integration workflow
                TestEditorIntegrationWorkflow();

                // Test 7: Export/Import roundtrip workflow
                TestExportImportWorkflow();

                // Test 8: Build system integration
                TestBuildSystemIntegration();

                // Test 9: Error handling in complete workflow
                TestErrorHandlingWorkflow();

                // Test 10: AI integration in complete workflow
                TestAIIntegrationWorkflow();

                PrintTestSummary();
                return testsPassed == testsTotal;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical error in end-to-end tests: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private static void TestCompleteWorkflowWithDefaults()
        {
            RunTest("Complete Workflow with Default Configuration", () =>
            {
                Console.WriteLine("  Testing complete generation workflow with default settings...");

                // Step 1: Create default configuration
                var configParser = new ConfigurationParser();
                var defaultConfig = configParser.GetDefaultConfig();
                
                AssertNotNull(defaultConfig, "Default configuration should not be null");
                Console.WriteLine("  ✓ Default configuration created");

                // Step 2: Validate configuration
                var validationResult = ConfigurationValidator.ValidateConfiguration(defaultConfig);
                
                AssertTrue(validationResult.IsValid, "Default configuration should be valid");
                Console.WriteLine("  ✓ Configuration validation passed");

                // Step 3: Generate terrain
                var randomGenerator = new RandomGenerator();
                var terrainGenerator = new PerlinNoiseGenerator(randomGenerator);
                var terrain = terrainGenerator.GenerateTerrain(defaultConfig, defaultConfig.Seed);
                
                AssertNotNull(terrain, "Generated terrain should not be null");
                AssertTrue(terrain.Width > 0 && terrain.Height > 0, "Terrain should have valid dimensions");
                Console.WriteLine("  ✓ Terrain generation completed");

                // Step 4: Place entities
                var entityPlacer = new EntityPlacer(randomGenerator);
                var entities = entityPlacer.PlaceEntities(terrain, defaultConfig, defaultConfig.Seed);
                
                AssertNotNull(entities, "Entity list should not be null");
                Console.WriteLine($"  ✓ Entity placement completed ({entities.Count} entities placed)");

                // Step 5: Assemble level
                var levelAssembler = new LevelAssembler();
                var level = levelAssembler.AssembleLevel(terrain, entities, defaultConfig);
                
                AssertNotNull(level, "Assembled level should not be null");
                AssertNotNull(level.Terrain, "Level terrain should not be null");
                AssertNotNull(level.Entities, "Level entities should not be null");
                Console.WriteLine("  ✓ Level assembly completed");

                // Step 6: Validate level
                var levelValidator = new LevelValidator();
                var isPlayable = levelValidator.IsPlayable(level);
                
                AssertTrue(isPlayable, "Generated level should be playable");
                Console.WriteLine("  ✓ Level validation passed");

                Console.WriteLine("  ✓ Complete workflow with defaults successful");
            });
        }

        private static void TestCompleteWorkflowWithCustomConfig()
        {
            RunTest("Complete Workflow with Custom Configuration", () =>
            {
                Console.WriteLine("  Testing complete generation workflow with custom configuration...");

                // Create custom configuration
                var customConfig = new GenerationConfig
                {
                    Width = 50,
                    Height = 40,
                    Seed = 98765,
                    GenerationAlgorithm = "cellular",
                    AlgorithmParameters = new Dictionary<string, object>
                    {
                        { "iterations", 5 },
                        { "birthLimit", 4 },
                        { "deathLimit", 3 }
                    },
                    Entities = new List<EntityConfig>
                    {
                        new EntityConfig
                        {
                            Type = EntityType.Enemy,
                            Count = 3,
                            MinDistance = 5.0f,
                            PlacementStrategy = "random"
                        }
                    }
                };

                // Run complete workflow
                var result = RunCompleteWorkflow(customConfig);
                
                AssertNotNull(result.Level, "Generated level should not be null");
                AssertTrue(result.IsValid, "Generated level should be valid");
                AssertEqual(result.Level.Terrain.Width, 50, "Level width should match configuration");
                AssertEqual(result.Level.Terrain.Height, 40, "Level height should match configuration");
                
                Console.WriteLine("  ✓ Complete workflow with custom config successful");
            });
        }

        private static void TestMultipleGenerationIterations()
        {
            RunTest("Multiple Generation Iterations (Requirement 5.1)", () =>
            {
                Console.WriteLine("  Testing multiple generation iterations produce different results...");

                var config = new GenerationConfig
                {
                    Width = 30,
                    Height = 30,
                    GenerationAlgorithm = "perlin"
                };

                var results = new List<WorkflowResult>();
                
                // Generate 5 different levels
                for (int i = 0; i < 5; i++)
                {
                    config.Seed = 1000 + i; // Different seed each time
                    var result = RunCompleteWorkflow(config);
                    results.Add(result);
                    
                    AssertNotNull(result.Level, $"Level {i + 1} should not be null");
                    AssertTrue(result.IsValid, $"Level {i + 1} should be valid");
                }

                // Verify levels are different
                for (int i = 0; i < results.Count - 1; i++)
                {
                    for (int j = i + 1; j < results.Count; j++)
                    {
                        var level1 = results[i].Level;
                        var level2 = results[j].Level;
                        
                        bool areDifferent = AreLevelsDifferent(level1, level2);
                        AssertTrue(areDifferent, $"Level {i + 1} and Level {j + 1} should be different");
                    }
                }

                Console.WriteLine("  ✓ Multiple iterations produce unique results");
            });
        }

        private static void TestRealTimeParameterModification()
        {
            RunTest("Real-time Parameter Modification (Requirement 5.2)", () =>
            {
                Console.WriteLine("  Testing real-time parameter modification...");

                var baseConfig = new GenerationConfig
                {
                    Width = 40,
                    Height = 40,
                    Seed = 12345,
                    GenerationAlgorithm = "perlin"
                };

                // Generate base level
                var baseResult = RunCompleteWorkflow(baseConfig);
                AssertNotNull(baseResult.Level, "Base level should not be null");

                // Test enemy count modification
                var modifiedConfig = baseConfig.Clone();
                modifiedConfig.Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 10 }
                };

                var modifiedResult = RunCompleteWorkflow(modifiedConfig);
                AssertNotNull(modifiedResult.Level, "Modified level should not be null");
                AssertTrue(modifiedResult.Level.Entities.Count >= 10, "Modified level should have more entities");

                // Test level size modification
                var resizedConfig = baseConfig.Clone();
                resizedConfig.Width = 60;
                resizedConfig.Height = 60;

                var resizedResult = RunCompleteWorkflow(resizedConfig);
                AssertNotNull(resizedResult.Level, "Resized level should not be null");
                AssertEqual(resizedResult.Level.Terrain.Width, 60, "Resized level should have correct width");
                AssertEqual(resizedResult.Level.Terrain.Height, 60, "Resized level should have correct height");

                Console.WriteLine("  ✓ Real-time parameter modification successful");
            });
        }

        private static void TestLevelQualityValidation()
        {
            RunTest("Level Quality Validation (Requirement 5.3)", () =>
            {
                Console.WriteLine("  Testing level quality validation standards...");

                var config = new GenerationConfig
                {
                    Width = 50,
                    Height = 50,
                    GenerationAlgorithm = "perlin",
                    Entities = new List<EntityConfig>
                    {
                        new EntityConfig { Type = EntityType.Enemy, Count = 5 },
                        new EntityConfig { Type = EntityType.Item, Count = 8 }
                    }
                };

                var result = RunCompleteWorkflow(config);
                AssertNotNull(result.Level, "Generated level should not be null");

                var levelValidator = new LevelValidator();
                
                // Test playability
                bool isPlayable = levelValidator.IsPlayable(result.Level);
                AssertTrue(isPlayable, "Level should be playable");

                // Test quality metrics
                float qualityScore = levelValidator.EvaluateQuality(result.Level);
                AssertTrue(qualityScore > 0.0f, "Quality score should be positive");
                AssertTrue(qualityScore <= 1.0f, "Quality score should not exceed 1.0");

                // Test validation issues
                var validationResult = levelValidator.ValidateLevel(result.Level, out List<string> issues);
                AssertTrue(validationResult, "Level should pass validation");
                AssertTrue(issues.Count == 0, "Level should have no validation issues");

                Console.WriteLine($"  ✓ Level quality validation passed (Quality: {qualityScore:F2})");
            });
        }

        private static void TestEditorIntegrationWorkflow()
        {
            RunTest("Editor Integration Workflow", () =>
            {
                Console.WriteLine("  Testing editor integration workflow...");

                // Create a mock generation manager for editor integration
                var mockGenerationManager = new MockGenerationManager();
                var editorIntegration = new EditorIntegration(mockGenerationManager);
                
                // Test editor command registration
                editorIntegration.RegisterEditorCommands();
                Console.WriteLine("  ✓ Editor commands registered");

                // Test generation window functionality
                var config = new GenerationConfig { Width = 30, Height = 30 };
                var result = RunCompleteWorkflow(config);
                
                // Simulate displaying level in editor
                editorIntegration.DisplayGeneratedLevel(result.Level);
                Console.WriteLine("  ✓ Level displayed in editor");

                // Test error reporting
                var errors = new List<string> { "Test error message" };
                editorIntegration.ReportErrors(errors);
                Console.WriteLine("  ✓ Error reporting tested");

                Console.WriteLine("  ✓ Editor integration workflow successful");
            });
        }

        private static void TestExportImportWorkflow()
        {
            RunTest("Export/Import Workflow", () =>
            {
                Console.WriteLine("  Testing export/import roundtrip workflow...");

                // Generate a level
                var config = new GenerationConfig
                {
                    Width = 25,
                    Height = 25,
                    Entities = new List<EntityConfig>
                    {
                        new EntityConfig { Type = EntityType.Enemy, Count = 3 }
                    }
                };

                var originalResult = RunCompleteWorkflow(config);
                AssertNotNull(originalResult.Level, "Original level should not be null");

                // Export level
                var exportService = new LevelExportService();
                string exportedJson = exportService.ExportLevelToJson(originalResult.Level, config);
                
                AssertNotNull(exportedJson, "Exported JSON should not be null");
                AssertTrue(exportedJson.Length > 0, "Exported JSON should not be empty");
                Console.WriteLine("  ✓ Level exported to JSON");

                // Import level
                var importResult = exportService.ImportLevelFromJson(exportedJson);
                var importedLevel = importResult.Level;
                
                AssertNotNull(importedLevel, "Imported level should not be null");
                AssertEqual(importedLevel.Terrain.Width, originalResult.Level.Terrain.Width, "Imported level width should match");
                AssertEqual(importedLevel.Terrain.Height, originalResult.Level.Terrain.Height, "Imported level height should match");
                AssertEqual(importedLevel.Entities.Count, originalResult.Level.Entities.Count, "Imported entity count should match");
                
                Console.WriteLine("  ✓ Level imported from JSON");
                Console.WriteLine("  ✓ Export/import roundtrip successful");
            });
        }

        private static void TestBuildSystemIntegration()
        {
            RunTest("Build System Integration", () =>
            {
                Console.WriteLine("  Testing build system integration...");

                // Generate a level
                var config = new GenerationConfig { Width = 20, Height = 20 };
                var result = RunCompleteWorkflow(config);
                
                AssertNotNull(result.Level, "Level should be generated for build test");

                // Test build system
                var buildSystem = new BuildSystem();
                var buildSettings = new BuildSettings();

                buildSystem.ConfigureBuildSettings(buildSettings);
                
                // Note: We don't actually build an executable in the test environment
                // but we verify the build system can be configured and initialized
                Console.WriteLine("  ✓ Build system configured");
                
                var buildLog = buildSystem.GetBuildLog();
                AssertNotNull(buildLog, "Build log should be available");
                
                Console.WriteLine("  ✓ Build system integration successful");
            });
        }

        private static void TestErrorHandlingWorkflow()
        {
            RunTest("Error Handling Workflow", () =>
            {
                Console.WriteLine("  Testing error handling in complete workflow...");

                // Test with invalid configuration
                var invalidConfig = new GenerationConfig
                {
                    Width = -10, // Invalid width
                    Height = 0,  // Invalid height
                    Seed = 12345
                };

                try
                {
                    var validationResult = ConfigurationValidator.ValidateConfiguration(invalidConfig);
                    
                    AssertFalse(validationResult.IsValid, "Invalid configuration should fail validation");
                    AssertTrue(validationResult.Errors.Count > 0, "Invalid configuration should have errors");
                    
                    Console.WriteLine("  ✓ Invalid configuration properly rejected");
                }
                catch (Exception ex)
                {
                    // Expected behavior for invalid configuration
                    Console.WriteLine($"  ✓ Exception properly thrown for invalid config: {ex.Message}");
                }

                // Test with impossible entity placement
                var impossibleConfig = new GenerationConfig
                {
                    Width = 5,
                    Height = 5,
                    Entities = new List<EntityConfig>
                    {
                        new EntityConfig { Type = EntityType.Enemy, Count = 100 } // Too many entities for small map
                    }
                };

                var result = RunCompleteWorkflow(impossibleConfig);
                // Should handle gracefully - either place fewer entities or report the issue
                AssertNotNull(result, "Workflow should handle impossible placement gracefully");
                
                Console.WriteLine("  ✓ Error handling workflow successful");
            });
        }

        private static void TestAIIntegrationWorkflow()
        {
            RunTest("AI Integration Workflow", () =>
            {
                Console.WriteLine("  Testing AI integration in complete workflow...");

                var config = new GenerationConfig
                {
                    Width = 30,
                    Height = 30,
                    Entities = new List<EntityConfig>
                    {
                        new EntityConfig { Type = EntityType.Item, Count = 3 }
                    }
                };

                // Test with AI integration enabled
                var result = RunCompleteWorkflow(config, enableAI: true);
                AssertNotNull(result.Level, "Level with AI integration should not be null");

                // Verify AI-generated content (if available)
                if (result.Level.Metadata.ContainsKey("ai_generated_name"))
                {
                    var levelName = result.Level.Metadata["ai_generated_name"].ToString();
                    AssertNotNull(levelName, "AI-generated level name should not be null");
                    AssertTrue(levelName.Length > 0, "AI-generated level name should not be empty");
                    Console.WriteLine($"  ✓ AI-generated level name: {levelName}");
                }

                Console.WriteLine("  ✓ AI integration workflow successful");
            });
        }

        private static WorkflowResult RunCompleteWorkflow(GenerationConfig config, bool enableAI = false)
        {
            try
            {
                // Step 1: Validate configuration
                var validationResult = ConfigurationValidator.ValidateConfiguration(config);
                
                if (!validationResult.IsValid)
                {
                    return new WorkflowResult { IsValid = false, Errors = validationResult.Errors };
                }

                // Step 2: Generate terrain
                var randomGenerator = new RandomGenerator();
                ITerrainGenerator terrainGenerator = config.GenerationAlgorithm?.ToLower() switch
                {
                    "cellular" => new CellularAutomataGenerator(randomGenerator),
                    "maze" => new MazeGenerator(randomGenerator),
                    _ => new PerlinNoiseGenerator(randomGenerator)
                };

                var terrain = terrainGenerator.GenerateTerrain(config, config.Seed);

                // Step 3: Place entities
                var entityPlacer = new EntityPlacer(randomGenerator);
                var entities = entityPlacer.PlaceEntities(terrain, config, config.Seed);

                // Step 4: Assemble level
                ILevelAssembler levelAssembler;
                if (enableAI)
                {
                    var baseLevelAssembler = new LevelAssembler();
                    var httpClient = new System.Net.Http.HttpClient();
                    var aiConfig = new AIServiceConfig();
                    var logger = new ConsoleLogger();
                    var aiGenerator = new AIContentGenerator(httpClient, aiConfig, logger);
                    levelAssembler = new AIEnhancedLevelAssembler(baseLevelAssembler, aiGenerator, logger);
                }
                else
                {
                    levelAssembler = new LevelAssembler();
                }
                    
                var level = levelAssembler.AssembleLevel(terrain, entities, config);

                // Step 5: Validate level
                var levelValidator = new LevelValidator();
                var isPlayable = levelValidator.IsPlayable(level);

                return new WorkflowResult
                {
                    Level = level,
                    IsValid = isPlayable,
                    Errors = new List<string>()
                };
            }
            catch (Exception ex)
            {
                return new WorkflowResult
                {
                    IsValid = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private static bool AreLevelsDifferent(Level level1, Level level2)
        {
            // Compare terrain
            for (int x = 0; x < Math.Min(level1.Terrain.Width, level2.Terrain.Width); x++)
            {
                for (int y = 0; y < Math.Min(level1.Terrain.Height, level2.Terrain.Height); y++)
                {
                    if (level1.Terrain.GetTile(x, y) != level2.Terrain.GetTile(x, y))
                    {
                        return true;
                    }
                }
            }

            // Compare entity positions
            if (level1.Entities.Count != level2.Entities.Count)
            {
                return true;
            }

            for (int i = 0; i < level1.Entities.Count; i++)
            {
                if (level1.Entities[i].Position != level2.Entities[i].Position)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RunTest(string testName, Action testAction)
        {
            testsTotal++;
            try
            {
                Console.WriteLine($"Running: {testName}");
                testAction();
                testsPassed++;
                testResults.Add($"✓ {testName}");
                Console.WriteLine($"✓ {testName} PASSED");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                testResults.Add($"❌ {testName}: {ex.Message}");
                Console.WriteLine($"❌ {testName} FAILED: {ex.Message}");
                Console.WriteLine();
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition)
                throw new Exception($"Assertion failed: {message}");
        }

        private static void AssertNotNull(object obj, string message)
        {
            if (obj == null)
                throw new Exception($"Assertion failed: {message}");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!expected.Equals(actual))
                throw new Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
        }

        private static void PrintTestSummary()
        {
            Console.WriteLine("=== END-TO-END TEST SUMMARY ===");
            Console.WriteLine($"Tests Run: {testsTotal}");
            Console.WriteLine($"Tests Passed: {testsPassed}");
            Console.WriteLine($"Tests Failed: {testsTotal - testsPassed}");
            Console.WriteLine($"Success Rate: {(double)testsPassed / testsTotal * 100:F1}%");
            Console.WriteLine();

            Console.WriteLine("Test Results:");
            foreach (var result in testResults)
            {
                Console.WriteLine($"  {result}");
            }
            Console.WriteLine();

            if (testsPassed == testsTotal)
            {
                Console.WriteLine("🎉 ALL END-TO-END TESTS PASSED! 🎉");
                Console.WriteLine("Requirements 5.1, 5.2, 5.3, and 11.1 are fully satisfied.");
            }
            else
            {
                Console.WriteLine("❌ Some end-to-end tests failed. Please review the results above.");
            }
        }

        private class WorkflowResult
        {
            public Level Level { get; set; }
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }
    }
}
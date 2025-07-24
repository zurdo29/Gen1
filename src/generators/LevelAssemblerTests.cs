using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;
using ProceduralMiniGameGenerator.Validators;

namespace ProceduralMiniGameGenerator.Tests
{
    /// <summary>
    /// Comprehensive unit tests for level assembly and validation
    /// </summary>
    public class LevelAssemblerTests
    {
        private readonly LevelAssembler _assembler;
        private readonly LevelValidator _validator;

        public LevelAssemblerTests()
        {
            _assembler = new LevelAssembler();
            _validator = new LevelValidator();
        }

        /// <summary>
        /// Tests basic level assembly with minimal configuration
        /// </summary>
        public void TestBasicLevelAssembly()
        {
            Console.WriteLine("Testing basic level assembly...");

            // Create simple terrain
            var terrain = CreateSimpleTerrain(20, 20);
            var entities = new List<Entity>();
            var config = CreateBasicConfig();

            // Assemble level
            var level = _assembler.AssembleLevel(terrain, entities, config);

            // Verify basic properties
            AssertNotNull(level, "Level should not be null");
            AssertNotNull(level.Terrain, "Level terrain should not be null");
            AssertNotNull(level.Entities, "Level entities should not be null");
            AssertNotNull(level.Name, "Level name should not be null");
            AssertNotNull(level.Metadata, "Level metadata should not be null");

            Console.WriteLine("✓ Basic level assembly test passed");
        }

        /// <summary>
        /// Tests level assembly with various entity configurations
        /// </summary>
        public void TestLevelAssemblyWithEntities()
        {
            Console.WriteLine("Testing level assembly with entities...");

            var terrain = CreateSimpleTerrain(30, 30);
            var entities = CreateTestEntities();
            var config = CreateBasicConfig();

            var level = _assembler.AssembleLevel(terrain, entities, config);

            // Verify entities are preserved
            AssertEqual(entities.Count, level.Entities.Count, "Entity count should be preserved");
            
            // Verify entity types are preserved
            var originalTypes = entities.Select(e => e.Type).OrderBy(t => t).ToList();
            var levelTypes = level.Entities.Select(e => e.Type).OrderBy(t => t).ToList();
            AssertEqual(originalTypes.Count, levelTypes.Count, "Entity type count should match");

            Console.WriteLine("✓ Level assembly with entities test passed");
        }

        /// <summary>
        /// Tests level assembly with different terrain configurations
        /// </summary>
        public void TestLevelAssemblyWithDifferentTerrains()
        {
            Console.WriteLine("Testing level assembly with different terrains...");

            var testCases = new[]
            {
                (10, 10, "Small terrain"),
                (50, 50, "Medium terrain"),
                (100, 100, "Large terrain"),
                (20, 40, "Rectangular terrain"),
                (40, 20, "Wide terrain")
            };

            foreach (var (width, height, description) in testCases)
            {
                var terrain = CreateVariedTerrain(width, height);
                var entities = new List<Entity>();
                var config = CreateBasicConfig();
                config.Width = width;
                config.Height = height;

                var level = _assembler.AssembleLevel(terrain, entities, config);

                AssertEqual(width, level.Terrain.Width, $"{description}: Width should match");
                AssertEqual(height, level.Terrain.Height, $"{description}: Height should match");
                AssertTrue(level.Metadata.ContainsKey("Dimensions"), $"{description}: Should contain dimensions metadata");
            }

            Console.WriteLine("✓ Level assembly with different terrains test passed");
        }

        /// <summary>
        /// Tests visual theme application
        /// </summary>
        public void TestVisualThemeApplication()
        {
            Console.WriteLine("Testing visual theme application...");

            var terrain = CreateSimpleTerrain(20, 20);
            var entities = CreateTestEntities();
            var config = CreateBasicConfig();
            var level = _assembler.AssembleLevel(terrain, entities, config);

            var theme = CreateTestVisualTheme();
            _assembler.ApplyVisualTheme(level, theme);

            // Verify theme is applied to metadata
            AssertTrue(level.Metadata.ContainsKey("VisualTheme"), "Should contain visual theme metadata");
            AssertEqual(theme.Name, level.Metadata["VisualTheme"], "Theme name should match");

            // Verify entities have sprite properties
            foreach (var entity in level.Entities)
            {
                if (theme.EntitySprites.ContainsKey(entity.Type))
                {
                    AssertTrue(entity.Properties.ContainsKey("Sprite"), $"Entity {entity.Type} should have sprite property");
                }
            }

            Console.WriteLine("✓ Visual theme application test passed");
        }

        /// <summary>
        /// Tests level validation with valid levels
        /// </summary>
        public void TestLevelValidationValid()
        {
            Console.WriteLine("Testing level validation with valid levels...");

            var terrain = CreateNavigableTerrain(30, 30);
            var entities = CreateValidEntitySet();
            var config = CreateBasicConfig();
            var level = _assembler.AssembleLevel(terrain, entities, config);

            var isValid = _validator.ValidateLevel(level, out var issues);

            AssertTrue(isValid, "Valid level should pass validation");
            AssertEqual(0, issues.Count, "Valid level should have no issues");
            AssertTrue(_validator.IsPlayable(level), "Valid level should be playable");

            var quality = _validator.EvaluateQuality(level);
            AssertTrue(quality > 0.5f, $"Valid level should have decent quality (got {quality})");

            Console.WriteLine("✓ Level validation with valid levels test passed");
        }

        /// <summary>
        /// Tests level validation correctly identifies various issues
        /// </summary>
        public void TestLevelValidationIdentifiesIssues()
        {
            Console.WriteLine("Testing level validation identifies issues...");

            // Test null level
            var isValid = _validator.ValidateLevel(null, out var issues);
            AssertFalse(isValid, "Null level should fail validation");
            AssertTrue(issues.Count > 0, "Null level should have issues");

            // Test level with no navigable terrain
            var blockedTerrain = CreateBlockedTerrain(20, 20);
            var blockedLevel = new Level { Terrain = blockedTerrain, Entities = new List<Entity>() };
            isValid = _validator.ValidateLevel(blockedLevel, out issues);
            AssertFalse(isValid, "Blocked terrain should fail validation");
            AssertTrue(issues.Any(i => i.Contains("navigable")), "Should identify navigability issues");

            // Test level with entities in invalid positions
            var terrain = CreateSimpleTerrain(20, 20);
            var invalidEntities = CreateInvalidEntitySet(terrain);
            var invalidLevel = new Level { Terrain = terrain, Entities = invalidEntities };
            isValid = _validator.ValidateLevel(invalidLevel, out issues);
            AssertFalse(isValid, "Invalid entity placement should fail validation");
            AssertTrue(issues.Any(i => i.Contains("non-walkable")), "Should identify invalid entity placement");

            // Test level without essential entities
            var incompleteLevel = new Level 
            { 
                Terrain = CreateNavigableTerrain(20, 20), 
                Entities = new List<Entity> { EntityFactory.CreateEntity(EntityType.Enemy) }
            };
            isValid = _validator.ValidateLevel(incompleteLevel, out issues);
            AssertFalse(isValid, "Level without player/exit should fail validation");
            AssertTrue(issues.Any(i => i.Contains("player") || i.Contains("exit")), "Should identify missing essential entities");

            Console.WriteLine("✓ Level validation identifies issues test passed");
        }

        /// <summary>
        /// Tests level quality evaluation with different scenarios
        /// </summary>
        public void TestLevelQualityEvaluation()
        {
            Console.WriteLine("Testing level quality evaluation...");

            // High quality level
            var highQualityLevel = CreateHighQualityLevel();
            var highQuality = _validator.EvaluateQuality(highQualityLevel);
            AssertTrue(highQuality > 0.7f, $"High quality level should score > 0.7 (got {highQuality})");

            // Low quality level
            var lowQualityLevel = CreateLowQualityLevel();
            var lowQuality = _validator.EvaluateQuality(lowQualityLevel);
            AssertTrue(lowQuality < 0.5f, $"Low quality level should score < 0.5 (got {lowQuality})");

            // Medium quality level
            var mediumQualityLevel = CreateMediumQualityLevel();
            var mediumQuality = _validator.EvaluateQuality(mediumQualityLevel);
            AssertTrue(mediumQuality >= 0.4f && mediumQuality <= 0.8f, 
                $"Medium quality level should score between 0.4-0.8 (got {mediumQuality})");

            Console.WriteLine("✓ Level quality evaluation test passed");
        }

        /// <summary>
        /// Tests level assembly error handling
        /// </summary>
        public void TestLevelAssemblyErrorHandling()
        {
            Console.WriteLine("Testing level assembly error handling...");

            var terrain = CreateSimpleTerrain(20, 20);
            var entities = new List<Entity>();
            var config = CreateBasicConfig();

            // Test null terrain
            try
            {
                _assembler.AssembleLevel(null, entities, config);
                AssertFail("Should throw exception for null terrain");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            // Test null entities
            try
            {
                _assembler.AssembleLevel(terrain, null, config);
                AssertFail("Should throw exception for null entities");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            // Test null config
            try
            {
                _assembler.AssembleLevel(terrain, entities, null);
                AssertFail("Should throw exception for null config");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }

            Console.WriteLine("✓ Level assembly error handling test passed");
        }

        /// <summary>
        /// Tests level metadata generation
        /// </summary>
        public void TestLevelMetadataGeneration()
        {
            Console.WriteLine("Testing level metadata generation...");

            var terrain = CreateVariedTerrain(25, 25);
            var entities = CreateTestEntities();
            var config = CreateBasicConfig();
            var level = _assembler.AssembleLevel(terrain, entities, config);

            // Verify essential metadata
            var requiredKeys = new[] 
            { 
                "GeneratedAt", "GenerationSeed", "GenerationAlgorithm", 
                "Dimensions", "TerrainStatistics", "EntityStatistics",
                "NavigableArea", "NavigabilityRatio"
            };

            foreach (var key in requiredKeys)
            {
                AssertTrue(level.Metadata.ContainsKey(key), $"Metadata should contain {key}");
            }

            // Verify terrain statistics
            var terrainStats = level.Metadata["TerrainStatistics"] as Dictionary<string, object>;
            AssertNotNull(terrainStats, "Terrain statistics should not be null");
            AssertTrue(terrainStats.ContainsKey("TotalTiles"), "Should contain total tiles count");

            // Verify entity statistics
            var entityStats = level.Metadata["EntityStatistics"] as Dictionary<string, object>;
            AssertNotNull(entityStats, "Entity statistics should not be null");
            AssertEqual(entities.Count, entityStats["TotalEntities"], "Should match entity count");

            Console.WriteLine("✓ Level metadata generation test passed");
        }

        /// <summary>
        /// Tests playability validation with edge cases
        /// </summary>
        public void TestPlayabilityValidation()
        {
            Console.WriteLine("Testing playability validation...");

            // Test minimum navigable area
            var smallNavigableLevel = CreateLevelWithNavigableArea(50); // Below minimum
            AssertFalse(_validator.IsPlayable(smallNavigableLevel), "Level with insufficient navigable area should not be playable");

            var largeNavigableLevel = CreateLevelWithNavigableArea(200); // Above minimum
            AssertTrue(_validator.IsPlayable(largeNavigableLevel), "Level with sufficient navigable area should be playable");

            // Test entity placement validation
            var validPlacementLevel = CreateLevelWithValidEntityPlacement();
            AssertTrue(_validator.IsPlayable(validPlacementLevel), "Level with valid entity placement should be playable");

            var invalidPlacementLevel = CreateLevelWithInvalidEntityPlacement();
            AssertFalse(_validator.IsPlayable(invalidPlacementLevel), "Level with invalid entity placement should not be playable");

            Console.WriteLine("✓ Playability validation test passed");
        }

        /// <summary>
        /// Runs all tests
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("=== Running Level Assembly and Validation Tests ===");
            
            try
            {
                TestBasicLevelAssembly();
                TestLevelAssemblyWithEntities();
                TestLevelAssemblyWithDifferentTerrains();
                TestVisualThemeApplication();
                TestLevelValidationValid();
                TestLevelValidationIdentifiesIssues();
                TestLevelQualityEvaluation();
                TestLevelAssemblyErrorHandling();
                TestLevelMetadataGeneration();
                TestPlayabilityValidation();
                
                Console.WriteLine("\n✅ All level assembly and validation tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Test failed: {ex.Message}");
                throw;
            }
        }

        // Helper methods for creating test data

        private TileMap CreateSimpleTerrain(int width, int height)
        {
            var terrain = new TileMap(width, height);
            
            // Create borders and some internal structure
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        terrain.SetTile(x, y, TileType.Wall);
                    }
                    else
                    {
                        terrain.SetTile(x, y, TileType.Ground);
                    }
                }
            }
            
            return terrain;
        }

        private TileMap CreateVariedTerrain(int width, int height)
        {
            var terrain = new TileMap(width, height);
            var random = new Random(42); // Fixed seed for reproducible tests
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tileTypes = new[] { TileType.Ground, TileType.Wall, TileType.Water, TileType.Grass };
                    terrain.SetTile(x, y, tileTypes[random.Next(tileTypes.Length)]);
                }
            }
            
            return terrain;
        }

        private TileMap CreateNavigableTerrain(int width, int height)
        {
            var terrain = new TileMap(width, height);
            
            // Create mostly navigable terrain with some walls
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 70% ground, 30% walls
                    terrain.SetTile(x, y, (x + y) % 10 < 7 ? TileType.Ground : TileType.Wall);
                }
            }
            
            return terrain;
        }

        private TileMap CreateBlockedTerrain(int width, int height)
        {
            var terrain = new TileMap(width, height);
            
            // All walls - no navigable area
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    terrain.SetTile(x, y, TileType.Wall);
                }
            }
            
            return terrain;
        }

        private List<Entity> CreateTestEntities()
        {
            return new List<Entity>
            {
                CreateEntityAt(EntityType.Player, 5, 5),
                CreateEntityAt(EntityType.Enemy, 10, 10),
                CreateEntityAt(EntityType.Item, 15, 15),
                CreateEntityAt(EntityType.Exit, 20, 20)
            };
        }

        private List<Entity> CreateValidEntitySet()
        {
            return new List<Entity>
            {
                CreateEntityAt(EntityType.Player, 5, 5),
                CreateEntityAt(EntityType.Exit, 20, 20),
                CreateEntityAt(EntityType.Enemy, 10, 10),
                CreateEntityAt(EntityType.Item, 15, 15)
            };
        }

        private List<Entity> CreateInvalidEntitySet(TileMap terrain)
        {
            var entities = new List<Entity>();
            
            // Place entity on wall (invalid)
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    if (!terrain.IsWalkable(x, y))
                    {
                        entities.Add(CreateEntityAt(EntityType.Enemy, x, y));
                        break;
                    }
                }
                if (entities.Count > 0) break;
            }
            
            return entities;
        }

        private Entity CreateEntityAt(EntityType type, int x, int y)
        {
            var entity = EntityFactory.CreateEntity(type);
            entity.Position = new Vector2(x, y);
            return entity;
        }

        private GenerationConfig CreateBasicConfig()
        {
            return new GenerationConfig
            {
                Width = 30,
                Height = 30,
                Seed = 12345,
                GenerationAlgorithm = "perlin",
                AlgorithmParameters = new Dictionary<string, object>(),
                Entities = new List<EntityConfig>()
            };
        }

        private VisualTheme CreateTestVisualTheme()
        {
            return new VisualTheme
            {
                Name = "Test Theme",
                TileSprites = new Dictionary<TileType, string>
                {
                    { TileType.Ground, "ground.png" },
                    { TileType.Wall, "wall.png" }
                },
                EntitySprites = new Dictionary<EntityType, string>
                {
                    { EntityType.Player, "player.png" },
                    { EntityType.Enemy, "enemy.png" }
                }
            };
        }

        private Level CreateHighQualityLevel()
        {
            var terrain = CreateNavigableTerrain(40, 40);
            var entities = new List<Entity>
            {
                CreateEntityAt(EntityType.Player, 5, 5),
                CreateEntityAt(EntityType.Exit, 35, 35),
                CreateEntityAt(EntityType.Enemy, 20, 20),
                CreateEntityAt(EntityType.Item, 15, 25),
                CreateEntityAt(EntityType.PowerUp, 25, 15)
            };
            
            return new Level
            {
                Name = "High Quality Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>()
            };
        }

        private Level CreateLowQualityLevel()
        {
            var terrain = CreateBlockedTerrain(20, 20);
            // Add minimal navigable area
            terrain.SetTile(10, 10, TileType.Ground);
            terrain.SetTile(11, 10, TileType.Ground);
            
            return new Level
            {
                Name = "",
                Terrain = terrain,
                Entities = new List<Entity>(),
                Metadata = new Dictionary<string, object>()
            };
        }

        private Level CreateMediumQualityLevel()
        {
            var terrain = CreateSimpleTerrain(30, 30);
            var entities = new List<Entity>
            {
                CreateEntityAt(EntityType.Player, 5, 5),
                CreateEntityAt(EntityType.Enemy, 20, 20)
            };
            
            return new Level
            {
                Name = "Medium Quality Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>()
            };
        }

        private Level CreateLevelWithNavigableArea(int navigableCount)
        {
            var terrain = new TileMap(20, 20);
            int placed = 0;
            
            // Fill with walls first
            for (int x = 0; x < 20; x++)
            {
                for (int y = 0; y < 20; y++)
                {
                    terrain.SetTile(x, y, TileType.Wall);
                }
            }
            
            // Place specific number of navigable tiles
            for (int x = 0; x < 20 && placed < navigableCount; x++)
            {
                for (int y = 0; y < 20 && placed < navigableCount; y++)
                {
                    terrain.SetTile(x, y, TileType.Ground);
                    placed++;
                }
            }
            
            return new Level
            {
                Terrain = terrain,
                Entities = new List<Entity>()
            };
        }

        private Level CreateLevelWithValidEntityPlacement()
        {
            var terrain = CreateNavigableTerrain(20, 20);
            var entities = new List<Entity>
            {
                CreateEntityAt(EntityType.Player, 5, 5), // On navigable terrain
                CreateEntityAt(EntityType.Enemy, 10, 10) // On navigable terrain
            };
            
            return new Level
            {
                Terrain = terrain,
                Entities = entities
            };
        }

        private Level CreateLevelWithInvalidEntityPlacement()
        {
            var terrain = CreateNavigableTerrain(20, 20);
            var entities = new List<Entity>
            {
                CreateEntityAt(EntityType.Player, 0, 0) // On wall (invalid)
            };
            
            return new Level
            {
                Terrain = terrain,
                Entities = entities
            };
        }

        // Assertion helper methods

        private void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        private void AssertFalse(bool condition, string message)
        {
            if (condition)
                throw new Exception($"Assertion failed: {message}");
        }

        private void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
        }

        private void AssertNotNull(object obj, string message)
        {
            if (obj == null)
                throw new Exception($"Assertion failed: {message}");
        }

        private void AssertFail(string message)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
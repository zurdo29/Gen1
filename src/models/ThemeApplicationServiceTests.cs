using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Unit tests for ThemeApplicationService
    /// </summary>
    [TestClass]
    public class ThemeApplicationServiceTests
    {
        private ThemeApplicationService _service = null!;
        private IVisualThemeService _themeService = null!;
        
        [TestInitialize]
        public void Setup()
        {
            _themeService = new VisualThemeService();
            _service = new ThemeApplicationService(_themeService);
        }
        
        [TestMethod]
        public void Constructor_WithNullThemeService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new ThemeApplicationService(null!));
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithValidInputs_AppliesThemeSuccessfully()
        {
            // Arrange
            var level = CreateTestLevel();
            var theme = CreateTestTheme();
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.IsNotNull(warnings);
            Assert.AreEqual("test-theme", level.Metadata["VisualTheme"]);
            Assert.IsTrue(level.Metadata.ContainsKey("AppliedTileSprites"));
            Assert.IsTrue(level.Metadata.ContainsKey("ColorPalette"));
            Assert.IsTrue(level.Metadata.ContainsKey("ThemeAppliedAt"));
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithNullLevel_ThrowsArgumentNullException()
        {
            // Arrange
            var theme = CreateTestTheme();
            
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _service.ApplyThemeToLevel(null!, theme));
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithNullTheme_ThrowsArgumentNullException()
        {
            // Arrange
            var level = CreateTestLevel();
            
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _service.ApplyThemeToLevel(level, null!));
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithMissingTileSprites_GeneratesWarnings()
        {
            // Arrange
            var level = CreateTestLevel();
            var theme = new VisualTheme
            {
                Name = "incomplete-theme",
                TileSprites = new Dictionary<TileType, string>(), // Empty - missing sprites
                EntitySprites = new Dictionary<EntityType, string>
                {
                    [EntityType.Player] = "assets/player.png"
                },
                Colors = new ColorPalette()
            };
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.IsTrue(warnings.Any());
            Assert.IsTrue(warnings.Any(w => w.Contains("No sprite defined for tile type")));
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithMissingEntitySprites_GeneratesWarnings()
        {
            // Arrange
            var level = CreateTestLevel();
            var theme = new VisualTheme
            {
                Name = "incomplete-theme",
                TileSprites = new Dictionary<TileType, string>
                {
                    [TileType.Ground] = "assets/ground.png",
                    [TileType.Wall] = "assets/wall.png"
                },
                EntitySprites = new Dictionary<EntityType, string>(), // Empty - missing sprites
                Colors = new ColorPalette()
            };
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.IsTrue(warnings.Any());
            Assert.IsTrue(warnings.Any(w => w.Contains("No sprite defined for entity type")));
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithInvalidAssetPaths_UsesFallbacks()
        {
            // Arrange
            var level = CreateTestLevel();
            var theme = new VisualTheme
            {
                Name = "invalid-assets-theme",
                TileSprites = new Dictionary<TileType, string>
                {
                    [TileType.Ground] = "missing", // Invalid asset
                    [TileType.Wall] = "assets/wall.png"
                },
                EntitySprites = new Dictionary<EntityType, string>
                {
                    [EntityType.Player] = "missing" // Invalid asset
                },
                Colors = new ColorPalette()
            };
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.IsTrue(warnings.Any());
            Assert.IsTrue(warnings.Any(w => w.Contains("not found, using fallback")));
            
            // Check that fallback sprites were applied
            var player = level.Entities.First(e => e.Type == EntityType.Player);
            Assert.IsTrue(player.Properties["Sprite"].ToString()!.Contains("fallback"));
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_AppliesColorPalette()
        {
            // Arrange
            var level = CreateTestLevel();
            var theme = CreateTestTheme();
            theme.Colors = new ColorPalette
            {
                Primary = "#FF0000",
                Secondary = "#00FF00",
                CustomColors = new Dictionary<string, string>
                {
                    ["Player_Color"] = "#0000FF"
                }
            };
            
            // Add a player entity with color property
            var player = level.Entities.First(e => e.Type == EntityType.Player);
            player.Properties["Color"] = "#FFFFFF"; // Will be overridden by theme
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.AreEqual(theme.Colors, level.Metadata["ColorPalette"]);
            Assert.AreEqual("#0000FF", player.Properties["Color"]);
        }
        
        [TestMethod]
        public void ValidateThemeApplication_WithValidInputs_ReturnsSuccess()
        {
            // Arrange
            var level = CreateTestLevel();
            var theme = CreateTestTheme();
            
            // Act
            var result = _service.ValidateThemeApplication(level, theme);
            
            // Assert
            Assert.IsTrue(result.IsValid);
        }
        
        [TestMethod]
        public void ValidateThemeApplication_WithNullLevel_ReturnsError()
        {
            // Arrange
            var theme = CreateTestTheme();
            
            // Act
            var result = _service.ValidateThemeApplication(null!, theme);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Level cannot be null")));
        }
        
        [TestMethod]
        public void ValidateThemeApplication_WithNullTheme_ReturnsError()
        {
            // Arrange
            var level = CreateTestLevel();
            
            // Act
            var result = _service.ValidateThemeApplication(level, null!);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Theme cannot be null")));
        }
        
        [TestMethod]
        public void ValidateThemeApplication_WithMissingAssets_ReturnsWarnings()
        {
            // Arrange
            var level = CreateTestLevel();
            var theme = new VisualTheme
            {
                Name = "incomplete-theme",
                TileSprites = new Dictionary<TileType, string>(), // Missing tile sprites
                EntitySprites = new Dictionary<EntityType, string>(), // Missing entity sprites
                Colors = new ColorPalette()
            };
            
            // Act
            var result = _service.ValidateThemeApplication(level, theme);
            
            // Assert
            Assert.IsTrue(result.IsValid); // Still valid, but has warnings
            Assert.IsTrue(result.Warnings.Any());
        }
        
        [TestMethod]
        public void CreateApplicationReport_GeneratesCompleteReport()
        {
            // Arrange
            var level = CreateTestLevel();
            var theme = CreateTestTheme();
            var warnings = new List<string> { "Test warning" };
            
            // Act
            var report = _service.CreateApplicationReport(level, theme, warnings);
            
            // Assert
            Assert.AreEqual(theme.Name, report.ThemeName);
            Assert.AreEqual(level.Name, report.LevelName);
            Assert.IsTrue(report.Success);
            Assert.AreEqual(1, report.FallbacksUsed);
            Assert.AreEqual(warnings.Count, report.Warnings.Count);
            Assert.IsTrue(report.TileTypesProcessed > 0);
            Assert.IsTrue(report.EntitiesProcessed > 0);
        }
        
        /// <summary>
        /// Creates a test level for testing
        /// </summary>
        private Level CreateTestLevel()
        {
            var terrain = new TileMap(10, 10);
            
            // Set some tiles
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (x == 0 || x == 9 || y == 0 || y == 9)
                        terrain.SetTile(x, y, TileType.Wall);
                    else
                        terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var entities = new List<Entity>
            {
                new PlayerEntity { Position = new Vector2(5, 5) },
                new EnemyEntity { Position = new Vector2(3, 3) },
                new ItemEntity { Position = new Vector2(7, 7) }
            };
            
            return new Level
            {
                Name = "Test Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>()
            };
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithPerlinNoiseLevel_AppliesThemeCorrectly()
        {
            // Arrange
            var level = CreatePerlinNoiseLevelForTesting();
            var theme = CreateComprehensiveTheme();
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.IsNotNull(warnings);
            Assert.AreEqual("comprehensive-theme", level.Metadata["VisualTheme"]);
            
            // Verify terrain-specific tiles are handled
            var appliedTileSprites = (Dictionary<TileType, string>)level.Metadata["AppliedTileSprites"];
            Assert.IsTrue(appliedTileSprites.ContainsKey(TileType.Ground));
            Assert.IsTrue(appliedTileSprites.ContainsKey(TileType.Water));
            Assert.IsTrue(appliedTileSprites.ContainsKey(TileType.Grass));
            
            // Verify entities have sprites applied
            foreach (var entity in level.Entities)
            {
                Assert.IsTrue(entity.Properties.ContainsKey("Sprite"));
                Assert.AreEqual("comprehensive-theme", entity.Properties["ThemeApplied"]);
            }
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithCellularAutomataLevel_AppliesThemeCorrectly()
        {
            // Arrange
            var level = CreateCellularAutomataLevelForTesting();
            var theme = CreateComprehensiveTheme();
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.IsNotNull(warnings);
            Assert.AreEqual("comprehensive-theme", level.Metadata["VisualTheme"]);
            
            // Verify cave-specific tiles are handled
            var appliedTileSprites = (Dictionary<TileType, string>)level.Metadata["AppliedTileSprites"];
            Assert.IsTrue(appliedTileSprites.ContainsKey(TileType.Empty));
            Assert.IsTrue(appliedTileSprites.ContainsKey(TileType.Stone));
            Assert.IsTrue(appliedTileSprites.ContainsKey(TileType.Wall));
            
            // Verify entities appropriate for cave levels
            var hasCheckpoint = level.Entities.Any(e => e.Type == EntityType.Checkpoint);
            var hasExit = level.Entities.Any(e => e.Type == EntityType.Exit);
            Assert.IsTrue(hasCheckpoint || hasExit, "Cave level should have checkpoint or exit");
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithMazeLevel_AppliesThemeCorrectly()
        {
            // Arrange
            var level = CreateMazeLevelForTesting();
            var theme = CreateComprehensiveTheme();
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.IsNotNull(warnings);
            Assert.AreEqual("comprehensive-theme", level.Metadata["VisualTheme"]);
            
            // Verify maze-specific tiles are handled
            var appliedTileSprites = (Dictionary<TileType, string>)level.Metadata["AppliedTileSprites"];
            Assert.IsTrue(appliedTileSprites.ContainsKey(TileType.Ground));
            Assert.IsTrue(appliedTileSprites.ContainsKey(TileType.Wall));
            
            // Verify maze has proper structure (walls and paths)
            var terrain = level.Terrain;
            bool hasWalls = false, hasPaths = false;
            for (int x = 0; x < terrain.Width && (!hasWalls || !hasPaths); x++)
            {
                for (int y = 0; y < terrain.Height && (!hasWalls || !hasPaths); y++)
                {
                    if (terrain.GetTile(x, y) == TileType.Wall) hasWalls = true;
                    if (terrain.GetTile(x, y) == TileType.Ground) hasPaths = true;
                }
            }
            Assert.IsTrue(hasWalls && hasPaths, "Maze should have both walls and paths");
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithComplexLevel_HandlesAllEntityTypes()
        {
            // Arrange
            var level = CreateComplexLevelWithAllEntityTypes();
            var theme = CreateComprehensiveTheme();
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, theme);
            
            // Assert
            Assert.IsNotNull(warnings);
            
            // Verify all entity types have sprites applied
            var entityTypes = level.Entities.Select(e => e.Type).Distinct().ToList();
            Assert.IsTrue(entityTypes.Count >= 5, "Complex level should have multiple entity types");
            
            foreach (var entity in level.Entities)
            {
                Assert.IsTrue(entity.Properties.ContainsKey("Sprite"));
                Assert.IsFalse(string.IsNullOrEmpty(entity.Properties["Sprite"].ToString()));
            }
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithIncompleteTheme_UsesFallbacksForMissingAssets()
        {
            // Arrange
            var level = CreateComplexLevelWithAllEntityTypes();
            var incompleteTheme = new VisualTheme
            {
                Name = "incomplete-theme",
                TileSprites = new Dictionary<TileType, string>
                {
                    [TileType.Ground] = "assets/ground.png"
                    // Missing other tile types
                },
                EntitySprites = new Dictionary<EntityType, string>
                {
                    [EntityType.Player] = "assets/player.png"
                    // Missing other entity types
                },
                Colors = new ColorPalette()
            };
            
            // Act
            var warnings = _service.ApplyThemeToLevel(level, incompleteTheme);
            
            // Assert
            Assert.IsTrue(warnings.Count > 0, "Should generate warnings for missing assets");
            
            // Verify fallbacks were used
            var fallbackWarnings = warnings.Where(w => w.Contains("fallback")).ToList();
            Assert.IsTrue(fallbackWarnings.Count > 0, "Should have fallback warnings");
            
            // Verify all entities still have sprites (fallbacks)
            foreach (var entity in level.Entities)
            {
                Assert.IsTrue(entity.Properties.ContainsKey("Sprite"));
                var spritePath = entity.Properties["Sprite"].ToString();
                Assert.IsFalse(string.IsNullOrEmpty(spritePath));
            }
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithDifferentLevelSizes_HandlesCorrectly()
        {
            // Arrange
            var smallLevel = CreateSmallLevel();
            var largeLevel = CreateLargeLevel();
            var theme = CreateComprehensiveTheme();
            
            // Act
            var smallWarnings = _service.ApplyThemeToLevel(smallLevel, theme);
            var largeWarnings = _service.ApplyThemeToLevel(largeLevel, theme);
            
            // Assert
            Assert.IsNotNull(smallWarnings);
            Assert.IsNotNull(largeWarnings);
            
            // Both should have theme applied successfully
            Assert.AreEqual("comprehensive-theme", smallLevel.Metadata["VisualTheme"]);
            Assert.AreEqual("comprehensive-theme", largeLevel.Metadata["VisualTheme"]);
            
            // Large level might have more tile types, but both should work
            var smallTileSprites = (Dictionary<TileType, string>)smallLevel.Metadata["AppliedTileSprites"];
            var largeTileSprites = (Dictionary<TileType, string>)largeLevel.Metadata["AppliedTileSprites"];
            
            Assert.IsTrue(smallTileSprites.Count > 0);
            Assert.IsTrue(largeTileSprites.Count >= smallTileSprites.Count);
        }
        
        [TestMethod]
        public void ApplyThemeToLevel_WithSpecializedThemes_AppliesCorrectly()
        {
            // Arrange
            var level = CreateTestLevel();
            var dungeonTheme = CreateDungeonTheme();
            var forestTheme = CreateForestTheme();
            
            // Act
            var dungeonWarnings = _service.ApplyThemeToLevel(level, dungeonTheme);
            
            // Reset level metadata for second test
            level.Metadata.Clear();
            foreach (var entity in level.Entities)
            {
                entity.Properties.Remove("Sprite");
                entity.Properties.Remove("ThemeApplied");
            }
            
            var forestWarnings = _service.ApplyThemeToLevel(level, forestTheme);
            
            // Assert
            Assert.IsNotNull(dungeonWarnings);
            Assert.IsNotNull(forestWarnings);
            
            // Verify forest theme was applied (last one)
            Assert.AreEqual("forest-theme", level.Metadata["VisualTheme"]);
            
            // Verify theme-specific properties
            var colorPalette = (ColorPalette)level.Metadata["ColorPalette"];
            Assert.AreEqual("#228B22", colorPalette.Primary); // Forest green
        }
        
        [TestMethod]
        public void ValidateThemeApplication_WithDifferentLevelTypes_ValidatesCorrectly()
        {
            // Arrange
            var perlinLevel = CreatePerlinNoiseLevelForTesting();
            var caveLevel = CreateCellularAutomataLevelForTesting();
            var mazeLevel = CreateMazeLevelForTesting();
            var theme = CreateComprehensiveTheme();
            
            // Act
            var perlinResult = _service.ValidateThemeApplication(perlinLevel, theme);
            var caveResult = _service.ValidateThemeApplication(caveLevel, theme);
            var mazeResult = _service.ValidateThemeApplication(mazeLevel, theme);
            
            // Assert
            Assert.IsTrue(perlinResult.IsValid, "Perlin noise level should be valid for theming");
            Assert.IsTrue(caveResult.IsValid, "Cave level should be valid for theming");
            Assert.IsTrue(mazeResult.IsValid, "Maze level should be valid for theming");
            
            // All should have minimal warnings since we're using comprehensive theme
            Assert.IsTrue(perlinResult.Warnings.Count <= 1);
            Assert.IsTrue(caveResult.Warnings.Count <= 1);
            Assert.IsTrue(mazeResult.Warnings.Count <= 1);
        }
        
        /// <summary>
        /// Creates a level that simulates Perlin noise generation with varied terrain
        /// </summary>
        private Level CreatePerlinNoiseLevelForTesting()
        {
            var terrain = new TileMap(12, 12);
            
            // Simulate Perlin noise pattern with varied terrain types
            for (int x = 0; x < 12; x++)
            {
                for (int y = 0; y < 12; y++)
                {
                    // Create a pattern that simulates natural terrain
                    if (x == 0 || x == 11 || y == 0 || y == 11)
                        terrain.SetTile(x, y, TileType.Wall);
                    else if ((x + y) % 4 == 0)
                        terrain.SetTile(x, y, TileType.Water);
                    else if ((x * y) % 3 == 0)
                        terrain.SetTile(x, y, TileType.Grass);
                    else
                        terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var entities = new List<Entity>
            {
                new PlayerEntity { Position = new Vector2(6, 6) },
                new EnemyEntity { Position = new Vector2(3, 3) },
                new ItemEntity { Position = new Vector2(9, 9) },
                new PowerUpEntity { Position = new Vector2(2, 8) }
            };
            
            return new Level
            {
                Name = "Perlin Noise Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>
                {
                    ["GenerationAlgorithm"] = "PerlinNoise",
                    ["LevelType"] = "Natural"
                }
            };
        }
        
        /// <summary>
        /// Creates a level that simulates cellular automata generation (cave-like)
        /// </summary>
        private Level CreateCellularAutomataLevelForTesting()
        {
            var terrain = new TileMap(10, 10);
            
            // Simulate cellular automata pattern with caves
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    // Create cave-like pattern
                    if (x == 0 || x == 9 || y == 0 || y == 9)
                        terrain.SetTile(x, y, TileType.Stone);
                    else if ((x + y) % 3 == 0 && x % 2 == 1)
                        terrain.SetTile(x, y, TileType.Empty); // Cave spaces
                    else if ((x * y) % 5 == 0)
                        terrain.SetTile(x, y, TileType.Wall);
                    else
                        terrain.SetTile(x, y, TileType.Stone);
                }
            }
            
            var entities = new List<Entity>
            {
                new PlayerEntity { Position = new Vector2(5, 5) },
                new EnemyEntity { Position = new Vector2(2, 2) },
                new CheckpointEntity { Position = new Vector2(7, 3) },
                new ExitEntity { Position = new Vector2(8, 8) }
            };
            
            return new Level
            {
                Name = "Cave Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>
                {
                    ["GenerationAlgorithm"] = "CellularAutomata",
                    ["LevelType"] = "Cave"
                }
            };
        }
        
        /// <summary>
        /// Creates a level that simulates maze generation
        /// </summary>
        private Level CreateMazeLevelForTesting()
        {
            var terrain = new TileMap(11, 11);
            
            // Create maze-like pattern
            for (int x = 0; x < 11; x++)
            {
                for (int y = 0; y < 11; y++)
                {
                    // Maze pattern: walls on odd coordinates, paths on even
                    if (x % 2 == 1 && y % 2 == 1)
                        terrain.SetTile(x, y, TileType.Wall);
                    else if (x % 2 == 0 && y % 2 == 0)
                        terrain.SetTile(x, y, TileType.Ground);
                    else
                        // Random walls to create maze complexity
                        terrain.SetTile(x, y, (x + y) % 3 == 0 ? TileType.Wall : TileType.Ground);
                }
            }
            
            var entities = new List<Entity>
            {
                new PlayerEntity { Position = new Vector2(0, 0) },
                new ExitEntity { Position = new Vector2(10, 10) },
                new ItemEntity { Position = new Vector2(4, 6) },
                new EnemyEntity { Position = new Vector2(6, 4) }
            };
            
            return new Level
            {
                Name = "Maze Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>
                {
                    ["GenerationAlgorithm"] = "Maze",
                    ["LevelType"] = "Maze"
                }
            };
        }
        
        /// <summary>
        /// Creates a complex level with all entity types for comprehensive testing
        /// </summary>
        private Level CreateComplexLevelWithAllEntityTypes()
        {
            var terrain = new TileMap(15, 15);
            
            // Create varied terrain
            for (int x = 0; x < 15; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    if (x == 0 || x == 14 || y == 0 || y == 14)
                        terrain.SetTile(x, y, TileType.Wall);
                    else if (x < 5 && y < 5)
                        terrain.SetTile(x, y, TileType.Grass);
                    else if (x > 10 && y > 10)
                        terrain.SetTile(x, y, TileType.Water);
                    else if (x > 10 && y < 5)
                        terrain.SetTile(x, y, TileType.Sand);
                    else
                        terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var entities = new List<Entity>
            {
                new PlayerEntity { Position = new Vector2(7, 7) },
                new EnemyEntity { Position = new Vector2(3, 3) },
                new ItemEntity { Position = new Vector2(11, 11) },
                new PowerUpEntity { Position = new Vector2(2, 12) },
                new NPCEntity { Position = new Vector2(12, 2) },
                new ExitEntity { Position = new Vector2(13, 13) },
                new CheckpointEntity { Position = new Vector2(7, 2) },
                new ObstacleEntity { Position = new Vector2(5, 10) },
                new TriggerEntity { Position = new Vector2(10, 7) }
            };
            
            return new Level
            {
                Name = "Complex Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>
                {
                    ["GenerationAlgorithm"] = "Hybrid",
                    ["LevelType"] = "Complex"
                }
            };
        }
        
        /// <summary>
        /// Creates a small level for size testing
        /// </summary>
        private Level CreateSmallLevel()
        {
            var terrain = new TileMap(5, 5);
            
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    if (x == 0 || x == 4 || y == 0 || y == 4)
                        terrain.SetTile(x, y, TileType.Wall);
                    else
                        terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var entities = new List<Entity>
            {
                new PlayerEntity { Position = new Vector2(2, 2) },
                new ItemEntity { Position = new Vector2(1, 3) }
            };
            
            return new Level
            {
                Name = "Small Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>()
            };
        }
        
        /// <summary>
        /// Creates a large level for size testing
        /// </summary>
        private Level CreateLargeLevel()
        {
            var terrain = new TileMap(20, 20);
            
            for (int x = 0; x < 20; x++)
            {
                for (int y = 0; y < 20; y++)
                {
                    if (x == 0 || x == 19 || y == 0 || y == 19)
                        terrain.SetTile(x, y, TileType.Wall);
                    else if (x < 7 && y < 7)
                        terrain.SetTile(x, y, TileType.Grass);
                    else if (x > 13 && y > 13)
                        terrain.SetTile(x, y, TileType.Water);
                    else if ((x + y) % 4 == 0)
                        terrain.SetTile(x, y, TileType.Sand);
                    else if ((x * y) % 7 == 0)
                        terrain.SetTile(x, y, TileType.Stone);
                    else
                        terrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var entities = new List<Entity>();
            for (int i = 1; i < 19; i += 3)
            {
                for (int j = 1; j < 19; j += 4)
                {
                    if (entities.Count < 15) // Limit entities
                    {
                        var entityType = (EntityType)(entities.Count % 9);
                        entities.Add(CreateEntityByType(entityType, new Vector2(i, j)));
                    }
                }
            }
            
            return new Level
            {
                Name = "Large Level",
                Terrain = terrain,
                Entities = entities,
                Metadata = new Dictionary<string, object>()
            };
        }
        
        /// <summary>
        /// Creates a comprehensive theme with all tile and entity types
        /// </summary>
        private VisualTheme CreateComprehensiveTheme()
        {
            return new VisualTheme
            {
                Name = "comprehensive-theme",
                TileSprites = new Dictionary<TileType, string>
                {
                    [TileType.Empty] = "assets/empty.png",
                    [TileType.Ground] = "assets/ground.png",
                    [TileType.Wall] = "assets/wall.png",
                    [TileType.Water] = "assets/water.png",
                    [TileType.Grass] = "assets/grass.png",
                    [TileType.Stone] = "assets/stone.png",
                    [TileType.Sand] = "assets/sand.png",
                    [TileType.Lava] = "assets/lava.png",
                    [TileType.Ice] = "assets/ice.png"
                },
                EntitySprites = new Dictionary<EntityType, string>
                {
                    [EntityType.Player] = "assets/player.png",
                    [EntityType.Enemy] = "assets/enemy.png",
                    [EntityType.Item] = "assets/item.png",
                    [EntityType.PowerUp] = "assets/powerup.png",
                    [EntityType.NPC] = "assets/npc.png",
                    [EntityType.Exit] = "assets/exit.png",
                    [EntityType.Checkpoint] = "assets/checkpoint.png",
                    [EntityType.Obstacle] = "assets/obstacle.png",
                    [EntityType.Trigger] = "assets/trigger.png"
                },
                Colors = new ColorPalette
                {
                    Primary = "#4A90E2",
                    Secondary = "#F5A623",
                    CustomColors = new Dictionary<string, string>
                    {
                        ["Player_Color"] = "#00FF00",
                        ["Enemy_Color"] = "#FF0000",
                        ["Item_Color"] = "#FFD700"
                    }
                }
            };
        }
        
        /// <summary>
        /// Creates a dungeon-themed visual theme
        /// </summary>
        private VisualTheme CreateDungeonTheme()
        {
            return new VisualTheme
            {
                Name = "dungeon-theme",
                TileSprites = new Dictionary<TileType, string>
                {
                    [TileType.Ground] = "assets/dungeon/floor.png",
                    [TileType.Wall] = "assets/dungeon/wall.png",
                    [TileType.Stone] = "assets/dungeon/stone.png"
                },
                EntitySprites = new Dictionary<EntityType, string>
                {
                    [EntityType.Player] = "assets/dungeon/hero.png",
                    [EntityType.Enemy] = "assets/dungeon/monster.png",
                    [EntityType.Item] = "assets/dungeon/treasure.png"
                },
                Colors = new ColorPalette
                {
                    Primary = "#8B4513", // Brown
                    Secondary = "#2F4F4F" // Dark slate gray
                }
            };
        }
        
        /// <summary>
        /// Creates a forest-themed visual theme
        /// </summary>
        private VisualTheme CreateForestTheme()
        {
            return new VisualTheme
            {
                Name = "forest-theme",
                TileSprites = new Dictionary<TileType, string>
                {
                    [TileType.Ground] = "assets/forest/dirt.png",
                    [TileType.Grass] = "assets/forest/grass.png",
                    [TileType.Water] = "assets/forest/stream.png"
                },
                EntitySprites = new Dictionary<EntityType, string>
                {
                    [EntityType.Player] = "assets/forest/ranger.png",
                    [EntityType.Enemy] = "assets/forest/wolf.png",
                    [EntityType.Item] = "assets/forest/berry.png"
                },
                Colors = new ColorPalette
                {
                    Primary = "#228B22", // Forest green
                    Secondary = "#8FBC8F" // Dark sea green
                }
            };
        }
        
        /// <summary>
        /// Helper method to create entities by type
        /// </summary>
        private Entity CreateEntityByType(EntityType type, Vector2 position)
        {
            return type switch
            {
                EntityType.Player => new PlayerEntity { Position = position },
                EntityType.Enemy => new EnemyEntity { Position = position },
                EntityType.Item => new ItemEntity { Position = position },
                EntityType.PowerUp => new PowerUpEntity { Position = position },
                EntityType.NPC => new NPCEntity { Position = position },
                EntityType.Exit => new ExitEntity { Position = position },
                EntityType.Checkpoint => new CheckpointEntity { Position = position },
                EntityType.Obstacle => new ObstacleEntity { Position = position },
                EntityType.Trigger => new TriggerEntity { Position = position },
                _ => new PlayerEntity { Position = position }
            };
        }
        
        /// <summary>
        /// Creates a test theme for testing
        /// </summary>
        private VisualTheme CreateTestTheme()
        {
            return new VisualTheme
            {
                Name = "test-theme",
                TileSprites = new Dictionary<TileType, string>
                {
                    [TileType.Ground] = "assets/ground.png",
                    [TileType.Wall] = "assets/wall.png"
                },
                EntitySprites = new Dictionary<EntityType, string>
                {
                    [EntityType.Player] = "assets/player.png",
                    [EntityType.Enemy] = "assets/enemy.png",
                    [EntityType.Item] = "assets/item.png"
                },
                Colors = new ColorPalette
                {
                    Primary = "#FF0000",
                    Secondary = "#00FF00"
                }
            };
        }
    }
}
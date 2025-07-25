using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ProceduralMiniGameGenerator.Models.Tests
{
    [TestClass]
    public class VisualThemeServiceTests
    {
        private VisualThemeService _themeService = null!;
        
        [TestInitialize]
        public void Setup()
        {
            _themeService = new VisualThemeService();
        }
        
        [TestMethod]
        public void GetAvailableThemes_ShouldReturnDefaultThemes()
        {
            // Act
            var themes = _themeService.GetAvailableThemes();
            
            // Assert
            Assert.IsNotNull(themes);
            Assert.IsTrue(themes.Count >= 4); // default, fantasy, scifi, retro
            Assert.IsTrue(themes.Any(t => t.Name == "default"));
            Assert.IsTrue(themes.Any(t => t.Name == "fantasy"));
            Assert.IsTrue(themes.Any(t => t.Name == "scifi"));
            Assert.IsTrue(themes.Any(t => t.Name == "retro"));
        }
        
        [TestMethod]
        public void GetTheme_WithValidName_ShouldReturnCorrectTheme()
        {
            // Act
            var theme = _themeService.GetTheme("fantasy");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("fantasy", theme.Name);
            Assert.IsNotNull(theme.Colors);
            Assert.IsTrue(theme.TileSprites.Count > 0);
            Assert.IsTrue(theme.EntitySprites.Count > 0);
        }
        
        [TestMethod]
        public void GetTheme_WithInvalidName_ShouldReturnDefaultTheme()
        {
            // Act
            var theme = _themeService.GetTheme("nonexistent");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("default", theme.Name);
        }
        
        [TestMethod]
        public void GetTheme_WithNullName_ShouldReturnDefaultTheme()
        {
            // Act
            var theme = _themeService.GetTheme(null);
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("default", theme.Name);
        }
        
        [TestMethod]
        public void GetDefaultTheme_ShouldReturnValidTheme()
        {
            // Act
            var theme = _themeService.GetDefaultTheme();
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("default", theme.Name);
            Assert.IsNotNull(theme.Colors);
            Assert.IsTrue(theme.TileSprites.Count > 0);
            Assert.IsTrue(theme.EntitySprites.Count > 0);
            
            // Verify all tile types have sprites
            foreach (TileType tileType in Enum.GetValues<TileType>())
            {
                Assert.IsTrue(theme.TileSprites.ContainsKey(tileType), $"Missing sprite for tile type: {tileType}");
                Assert.IsFalse(string.IsNullOrEmpty(theme.TileSprites[tileType]), $"Empty sprite path for tile type: {tileType}");
            }
            
            // Verify all entity types have sprites
            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                Assert.IsTrue(theme.EntitySprites.ContainsKey(entityType), $"Missing sprite for entity type: {entityType}");
                Assert.IsFalse(string.IsNullOrEmpty(theme.EntitySprites[entityType]), $"Empty sprite path for entity type: {entityType}");
            }
        }
        
        [TestMethod]
        public void CreateTheme_WithValidConfig_ShouldReturnTheme()
        {
            // Arrange
            var config = new VisualThemeConfig
            {
                ThemeName = "test-theme",
                ColorPalette = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["primary"] = "#FF0000",
                    ["secondary"] = "#00FF00",
                    ["background"] = "#0000FF"
                },
                TileSprites = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Ground"] = "test/ground.png",
                    ["Wall"] = "test/wall.png"
                }
            };
            
            // Act
            var theme = _themeService.CreateTheme(config);
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("test-theme", theme.Name);
            Assert.AreEqual("#FF0000", theme.Colors.Primary);
            Assert.AreEqual("#00FF00", theme.Colors.Secondary);
            Assert.AreEqual("#0000FF", theme.Colors.Background);
            Assert.IsTrue(theme.TileSprites.ContainsKey(TileType.Ground));
            Assert.AreEqual("test/ground.png", theme.TileSprites[TileType.Ground]);
        }
        
        [TestMethod]
        public void CreateTheme_WithNullConfig_ShouldThrowException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _themeService.CreateTheme(null));
        }
        
        [TestMethod]
        public void ValidateTheme_WithValidConfig_ShouldReturnValid()
        {
            // Arrange
            var config = new VisualThemeConfig
            {
                ThemeName = "valid-theme",
                ColorPalette = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["primary"] = "#FF0000"
                }
            };
            
            // Act
            var result = _themeService.ValidateTheme(config);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }
        
        [TestMethod]
        public void ValidateTheme_WithNullConfig_ShouldReturnInvalid()
        {
            // Act
            var result = _themeService.ValidateTheme(null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("null")));
        }
        
        [TestMethod]
        public void RegisterTheme_WithValidTheme_ShouldAddToAvailableThemes()
        {
            // Arrange
            var customTheme = new VisualTheme
            {
                Name = "custom-test-theme",
                Colors = new ColorPalette()
            };
            
            // Act
            _themeService.RegisterTheme(customTheme);
            var themes = _themeService.GetAvailableThemes();
            
            // Assert
            Assert.IsTrue(themes.Any(t => t.Name == "custom-test-theme"));
            
            var retrievedTheme = _themeService.GetTheme("custom-test-theme");
            Assert.IsNotNull(retrievedTheme);
            Assert.AreEqual("custom-test-theme", retrievedTheme.Name);
        }
        
        [TestMethod]
        public void RegisterTheme_WithNullTheme_ShouldThrowException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _themeService.RegisterTheme(null));
        }
        
        [TestMethod]
        public void RegisterTheme_WithEmptyName_ShouldThrowException()
        {
            // Arrange
            var theme = new VisualTheme { Name = "" };
            
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _themeService.RegisterTheme(theme));
        }
        
        [TestMethod]
        public void GetAvailableColorPalettes_ShouldReturnPalettes()
        {
            // Act
            var palettes = _themeService.GetAvailableColorPalettes();
            
            // Assert
            Assert.IsNotNull(palettes);
            Assert.IsTrue(palettes.Count >= 4); // classic, warm, cool, nature
            
            // Verify each palette has required colors
            foreach (var palette in palettes)
            {
                Assert.IsFalse(string.IsNullOrEmpty(palette.Primary));
                Assert.IsFalse(string.IsNullOrEmpty(palette.Secondary));
                Assert.IsFalse(string.IsNullOrEmpty(palette.Background));
            }
        }
        
        [TestMethod]
        public void GetAvailableTileSets_ShouldReturnTileSets()
        {
            // Act
            var tileSets = _themeService.GetAvailableTileSets();
            
            // Assert
            Assert.IsNotNull(tileSets);
            Assert.IsTrue(tileSets.Count >= 3); // basic, detailed, minimalist
            Assert.IsTrue(tileSets.ContainsKey("basic"));
            Assert.IsTrue(tileSets.ContainsKey("detailed"));
            Assert.IsTrue(tileSets.ContainsKey("minimalist"));
            
            // Verify each tile set has sprites for all tile types
            foreach (var tileSet in tileSets.Values)
            {
                foreach (TileType tileType in Enum.GetValues<TileType>())
                {
                    Assert.IsTrue(tileSet.ContainsKey(tileType), $"Tile set missing sprite for {tileType}");
                    Assert.IsFalse(string.IsNullOrEmpty(tileSet[tileType]), $"Empty sprite path for {tileType}");
                }
            }
        }
        
        [TestMethod]
        public void CreateTheme_WithMissingSprites_ShouldApplyFallbacks()
        {
            // Arrange
            var config = new VisualThemeConfig
            {
                ThemeName = "incomplete-theme",
                TileSprites = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["Ground"] = "custom/ground.png"
                    // Missing other tile types
                }
            };
            
            // Act
            var theme = _themeService.CreateTheme(config);
            
            // Assert
            Assert.IsNotNull(theme);
            
            // Should have custom sprite for Ground
            Assert.AreEqual("custom/ground.png", theme.TileSprites[TileType.Ground]);
            
            // Should have fallback sprites for other types
            Assert.IsTrue(theme.TileSprites.ContainsKey(TileType.Wall));
            Assert.IsFalse(string.IsNullOrEmpty(theme.TileSprites[TileType.Wall]));
            Assert.IsTrue(theme.TileSprites.ContainsKey(TileType.Water));
            Assert.IsFalse(string.IsNullOrEmpty(theme.TileSprites[TileType.Water]));
        }
    }
}
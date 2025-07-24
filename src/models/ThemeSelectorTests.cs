using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ProceduralMiniGameGenerator.Models.Tests
{
    [TestClass]
    public class ThemeSelectorTests
    {
        private VisualThemeService _themeService = null!;
        private ThemeSelector _themeSelector = null!;
        
        [TestInitialize]
        public void Setup()
        {
            _themeService = new VisualThemeService();
            _themeSelector = new ThemeSelector(_themeService);
        }
        
        [TestMethod]
        public void SelectTheme_WithStylePreference_ShouldReturnPreferredTheme()
        {
            // Act
            var theme = _themeSelector.SelectTheme(stylePreference: "fantasy");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("fantasy", theme.Name);
        }
        
        [TestMethod]
        public void SelectTheme_WithGameTypeRpg_ShouldReturnFantasyTheme()
        {
            // Act
            var theme = _themeSelector.SelectTheme(gameType: "rpg");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("fantasy", theme.Name);
        }
        
        [TestMethod]
        public void SelectTheme_WithGameTypeSpace_ShouldReturnSciFiTheme()
        {
            // Act
            var theme = _themeSelector.SelectTheme(gameType: "space");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("scifi", theme.Name);
        }
        
        [TestMethod]
        public void SelectTheme_WithGameTypeArcade_ShouldReturnRetroTheme()
        {
            // Act
            var theme = _themeSelector.SelectTheme(gameType: "arcade");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("retro", theme.Name);
        }
        
        [TestMethod]
        public void SelectTheme_WithUnknownGameType_ShouldReturnDefaultTheme()
        {
            // Act
            var theme = _themeSelector.SelectTheme(gameType: "unknown");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("default", theme.Name);
        }
        
        [TestMethod]
        public void SelectTheme_WithNoParameters_ShouldReturnDefaultTheme()
        {
            // Act
            var theme = _themeSelector.SelectTheme();
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("default", theme.Name);
        }
        
        [TestMethod]
        public void CreateCustomTheme_WithBasicParameters_ShouldReturnCustomTheme()
        {
            // Act
            var theme = _themeSelector.CreateCustomTheme("my-custom-theme");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("my-custom-theme", theme.Name);
            Assert.IsNotNull(theme.Colors);
            Assert.IsTrue(theme.TileSprites.Count > 0);
            Assert.IsTrue(theme.EntitySprites.Count > 0);
        }
        
        [TestMethod]
        public void CreateCustomTheme_WithColorPalette_ShouldApplyColorPalette()
        {
            // Act
            var theme = _themeSelector.CreateCustomTheme("custom-warm", colorPaletteName: "warm");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("custom-warm", theme.Name);
            Assert.IsNotNull(theme.Colors);
            // Should have warm colors (orange-ish primary)
            Assert.AreEqual("#FF6B35", theme.Colors.Primary);
        }
        
        [TestMethod]
        public void CreateCustomTheme_WithTileSet_ShouldApplyTileSet()
        {
            // Act
            var theme = _themeSelector.CreateCustomTheme("custom-minimal", tileSetName: "minimalist");
            
            // Assert
            Assert.IsNotNull(theme);
            Assert.AreEqual("custom-minimal", theme.Name);
            Assert.IsTrue(theme.TileSprites.ContainsKey(TileType.Ground));
            Assert.AreEqual("sprites/minimal/white.png", theme.TileSprites[TileType.Ground]);
        }
        
        [TestMethod]
        public void GetColorPaletteByName_WithValidName_ShouldReturnPalette()
        {
            // Act
            var palette = _themeSelector.GetColorPaletteByName("warm");
            
            // Assert
            Assert.IsNotNull(palette);
            Assert.AreEqual("#FF6B35", palette.Primary);
        }
        
        [TestMethod]
        public void GetColorPaletteByName_WithInvalidName_ShouldReturnFirstPalette()
        {
            // Act
            var palette = _themeSelector.GetColorPaletteByName("nonexistent");
            
            // Assert
            Assert.IsNotNull(palette);
        }
        
        [TestMethod]
        public void ValidateThemeCompleteness_WithCompleteTheme_ShouldReturnValid()
        {
            // Arrange
            var theme = _themeService.GetDefaultTheme();
            
            // Act
            var result = _themeSelector.ValidateThemeCompleteness(theme);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }
        
        [TestMethod]
        public void ValidateThemeCompleteness_WithIncompleteTheme_ShouldReturnInvalid()
        {
            // Arrange
            var theme = new VisualTheme
            {
                Name = "incomplete",
                Colors = new ColorPalette()
                // Missing tile and entity sprites
            };
            
            // Act
            var result = _themeSelector.ValidateThemeCompleteness(theme);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Missing sprite")));
        }
        
        [TestMethod]
        public void ValidateThemeCompleteness_WithNullTheme_ShouldReturnInvalid()
        {
            // Act
            var result = _themeSelector.ValidateThemeCompleteness(null);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("null")));
        }
        
        [TestMethod]
        public void GetThemeRecommendations_WithGrassTile_ShouldRecommendFantasy()
        {
            // Act
            var recommendations = _themeSelector.GetThemeRecommendations(TileType.Grass);
            
            // Assert
            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Contains("fantasy"));
            Assert.IsTrue(recommendations.Contains("default"));
        }
        
        [TestMethod]
        public void GetThemeRecommendations_WithIceTile_ShouldRecommendSciFi()
        {
            // Act
            var recommendations = _themeSelector.GetThemeRecommendations(TileType.Ice);
            
            // Assert
            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Contains("scifi"));
        }
        
        [TestMethod]
        public void GetThemeRecommendations_WithLava_ShouldIncludeFantasy()
        {
            // Act
            var recommendations = _themeSelector.GetThemeRecommendations(TileType.Ground, hasLava: true);
            
            // Assert
            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Contains("fantasy"));
        }
        
        [TestMethod]
        public void GetThemeRecommendations_WithWaterNoLava_ShouldIncludeSciFi()
        {
            // Act
            var recommendations = _themeSelector.GetThemeRecommendations(TileType.Ground, hasWater: true, hasLava: false);
            
            // Assert
            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Contains("scifi"));
        }
        
        [TestMethod]
        public void GetThemeRecommendations_ShouldAlwaysIncludeRetro()
        {
            // Act
            var recommendations = _themeSelector.GetThemeRecommendations(TileType.Empty);
            
            // Assert
            Assert.IsNotNull(recommendations);
            Assert.IsTrue(recommendations.Contains("retro"));
        }
    }
}
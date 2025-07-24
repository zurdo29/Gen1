using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Utility class for selecting and customizing visual themes
    /// </summary>
    public class ThemeSelector
    {
        private readonly IVisualThemeService _themeService;
        
        public ThemeSelector(IVisualThemeService themeService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        }
        
        /// <summary>
        /// Selects a theme based on game type or style preferences
        /// </summary>
        /// <param name="gameType">Type of game (e.g., "platformer", "rpg", "puzzle")</param>
        /// <param name="stylePreference">Style preference (e.g., "fantasy", "scifi", "retro")</param>
        /// <returns>Selected visual theme</returns>
        public VisualTheme SelectTheme(string? gameType = null, string? stylePreference = null)
        {
            var availableThemes = _themeService.GetAvailableThemes();
            
            // If specific style preference is given, try to find it
            if (!string.IsNullOrEmpty(stylePreference))
            {
                var preferredTheme = availableThemes.FirstOrDefault(t => 
                    t.Name.Equals(stylePreference, StringComparison.OrdinalIgnoreCase));
                if (preferredTheme != null)
                    return preferredTheme;
            }
            
            // Select based on game type
            if (!string.IsNullOrEmpty(gameType))
            {
                return gameType.ToLower() switch
                {
                    "rpg" or "adventure" => _themeService.GetTheme("fantasy"),
                    "space" or "shooter" => _themeService.GetTheme("scifi"),
                    "arcade" or "platformer" => _themeService.GetTheme("retro"),
                    _ => _themeService.GetDefaultTheme()
                };
            }
            
            return _themeService.GetDefaultTheme();
        }
        
        /// <summary>
        /// Creates a custom theme by combining elements from existing themes
        /// </summary>
        /// <param name="baseName">Name for the custom theme</param>
        /// <param name="colorPaletteName">Name of color palette to use</param>
        /// <param name="tileSetName">Name of tile set to use</param>
        /// <param name="baseThemeName">Base theme to inherit from</param>
        /// <returns>Custom visual theme</returns>
        public VisualTheme CreateCustomTheme(string baseName, string? colorPaletteName = null, 
            string? tileSetName = null, string baseThemeName = "default")
        {
            var baseTheme = _themeService.GetTheme(baseThemeName);
            var customTheme = new VisualTheme
            {
                Name = baseName,
                Colors = baseTheme.Colors,
                TileSprites = new Dictionary<TileType, string>(baseTheme.TileSprites),
                EntitySprites = new Dictionary<EntityType, string>(baseTheme.EntitySprites),
                Properties = new Dictionary<string, object>(baseTheme.Properties)
            };
            
            // Apply custom color palette if specified
            if (!string.IsNullOrEmpty(colorPaletteName))
            {
                var colorPalette = GetColorPaletteByName(colorPaletteName);
                if (colorPalette != null)
                {
                    customTheme.Colors = colorPalette;
                }
            }
            
            // Apply custom tile set if specified
            if (!string.IsNullOrEmpty(tileSetName))
            {
                var tileSets = _themeService.GetAvailableTileSets();
                if (tileSets.ContainsKey(tileSetName))
                {
                    customTheme.TileSprites = new Dictionary<TileType, string>(tileSets[tileSetName]);
                }
            }
            
            return customTheme;
        }
        
        /// <summary>
        /// Gets a color palette by name
        /// </summary>
        /// <param name="paletteName">Name of the color palette</param>
        /// <returns>Color palette or null if not found</returns>
        public ColorPalette? GetColorPaletteByName(string? paletteName)
        {
            var palettes = _themeService.GetAvailableColorPalettes();
            
            // For now, we'll match by primary color or create a named palette
            return paletteName?.ToLower() switch
            {
                "warm" => palettes.FirstOrDefault(p => p.Primary == "#FF6B35"),
                "cool" => palettes.FirstOrDefault(p => p.Primary == "#00BFFF"),
                "nature" => palettes.FirstOrDefault(p => p.Primary == "#228B22"),
                "classic" or "default" => palettes.FirstOrDefault(p => p.Primary == "#FFFFFF"),
                _ => palettes.FirstOrDefault()
            };
        }
        
        /// <summary>
        /// Validates that a theme has all required sprites and colors
        /// </summary>
        /// <param name="theme">Theme to validate</param>
        /// <returns>Validation result with any missing elements</returns>
        public ValidationResult ValidateThemeCompleteness(VisualTheme theme)
        {
            var result = new ValidationResult();
            
            if (theme == null)
            {
                result.Errors.Add("Theme cannot be null");
                return result;
            }
            
            // Check for required tile sprites
            var requiredTileTypes = Enum.GetValues<TileType>();
            foreach (var tileType in requiredTileTypes)
            {
                if (!theme.TileSprites.ContainsKey(tileType) || string.IsNullOrEmpty(theme.TileSprites[tileType]))
                {
                    result.Errors.Add($"Missing sprite for tile type: {tileType}");
                }
            }
            
            // Check for required entity sprites
            var requiredEntityTypes = Enum.GetValues<EntityType>();
            foreach (var entityType in requiredEntityTypes)
            {
                if (!theme.EntitySprites.ContainsKey(entityType) || string.IsNullOrEmpty(theme.EntitySprites[entityType]))
                {
                    result.Errors.Add($"Missing sprite for entity type: {entityType}");
                }
            }
            
            // Check color palette
            if (theme.Colors == null)
            {
                result.Errors.Add("Theme must have a color palette");
            }
            else
            {
                if (string.IsNullOrEmpty(theme.Colors.Primary))
                    result.Errors.Add("Primary color is required");
                if (string.IsNullOrEmpty(theme.Colors.Secondary))
                    result.Errors.Add("Secondary color is required");
                if (string.IsNullOrEmpty(theme.Colors.Background))
                    result.Errors.Add("Background color is required");
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets theme recommendations based on level characteristics
        /// </summary>
        /// <param name="dominantTileType">Most common tile type in the level</param>
        /// <param name="hasWater">Whether the level contains water</param>
        /// <param name="hasLava">Whether the level contains lava</param>
        /// <returns>List of recommended theme names</returns>
        public List<string> GetThemeRecommendations(TileType dominantTileType, bool hasWater = false, bool hasLava = false)
        {
            var recommendations = new List<string>();
            
            // Base recommendations on dominant tile type
            switch (dominantTileType)
            {
                case TileType.Grass:
                case TileType.Stone:
                    recommendations.Add("fantasy");
                    recommendations.Add("default");
                    break;
                    
                case TileType.Sand:
                    recommendations.Add("fantasy");
                    recommendations.Add("retro");
                    break;
                    
                case TileType.Ice:
                    recommendations.Add("scifi");
                    recommendations.Add("default");
                    break;
                    
                case TileType.Lava:
                    recommendations.Add("fantasy");
                    recommendations.Add("scifi");
                    break;
                    
                default:
                    recommendations.Add("default");
                    break;
            }
            
            // Add specific recommendations based on special features
            if (hasLava)
            {
                if (!recommendations.Contains("fantasy"))
                    recommendations.Add("fantasy");
            }
            
            if (hasWater && !hasLava)
            {
                if (!recommendations.Contains("scifi"))
                    recommendations.Add("scifi");
            }
            
            // Always include retro as a fallback option
            if (!recommendations.Contains("retro"))
                recommendations.Add("retro");
                
            return recommendations;
        }
    }
}
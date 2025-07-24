using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Interface for visual theme management services
    /// </summary>
    public interface IVisualThemeService
    {
        /// <summary>
        /// Gets all available visual themes
        /// </summary>
        /// <returns>List of available themes</returns>
        List<VisualTheme> GetAvailableThemes();
        
        /// <summary>
        /// Gets a theme by name
        /// </summary>
        /// <param name="themeName">Name of the theme</param>
        /// <returns>Visual theme or null if not found</returns>
        VisualTheme GetTheme(string themeName);
        
        /// <summary>
        /// Creates a theme from configuration
        /// </summary>
        /// <param name="config">Theme configuration</param>
        /// <returns>Created visual theme</returns>
        VisualTheme CreateTheme(VisualThemeConfig config);
        
        /// <summary>
        /// Validates a theme configuration
        /// </summary>
        /// <param name="config">Theme configuration to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateTheme(VisualThemeConfig config);
        
        /// <summary>
        /// Gets the default theme
        /// </summary>
        /// <returns>Default visual theme</returns>
        VisualTheme GetDefaultTheme();
        
        /// <summary>
        /// Registers a new theme
        /// </summary>
        /// <param name="theme">Theme to register</param>
        void RegisterTheme(VisualTheme theme);
        
        /// <summary>
        /// Gets available color palettes
        /// </summary>
        /// <returns>List of predefined color palettes</returns>
        List<ColorPalette> GetAvailableColorPalettes();
        
        /// <summary>
        /// Gets available tile sets
        /// </summary>
        /// <returns>Dictionary of tile set names to tile sprite mappings</returns>
        Dictionary<string, Dictionary<TileType, string>> GetAvailableTileSets();
    }
}
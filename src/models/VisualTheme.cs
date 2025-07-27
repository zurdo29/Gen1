using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Represents a complete visual theme with all assets and styling information
    /// </summary>
    public class VisualTheme
    {
        /// <summary>
        /// Name of the visual theme
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Mapping of tile types to sprite paths
        /// </summary>
        public Dictionary<TileType, string> TileSprites { get; set; } = new Dictionary<TileType, string>();
        
        /// <summary>
        /// Mapping of entity types to sprite paths
        /// </summary>
        public Dictionary<EntityType, string> EntitySprites { get; set; } = new Dictionary<EntityType, string>();
        
        /// <summary>
        /// Color palette for the theme
        /// </summary>
        public ColorPalette Colors { get; set; } = new ColorPalette();
        
        /// <summary>
        /// Additional theme properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a visual theme from configuration
        /// </summary>
        /// <param name="config">Theme configuration</param>
        /// <returns>Visual theme instance</returns>
        public static VisualTheme FromConfig(VisualThemeConfig config)
        {
            var theme = new VisualTheme
            {
                Name = config.ThemeName
            };
            
            // Convert string mappings to enum mappings
            foreach (var kvp in config.TileSprites)
            {
                if (System.Enum.TryParse<TileType>(kvp.Key, out var tileType))
                {
                    theme.TileSprites[tileType] = kvp.Value;
                }
            }
            
            foreach (var kvp in config.EntitySprites)
            {
                if (System.Enum.TryParse<EntityType>(kvp.Key, out var entityType))
                {
                    theme.EntitySprites[entityType] = kvp.Value;
                }
            }
            
            // Convert color palette
            theme.Colors = config.ColorPalette;
            
            return theme;
        }
    }
}
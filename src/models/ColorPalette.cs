using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Represents a color palette for visual theming
    /// </summary>
    public class ColorPalette
    {
        /// <summary>
        /// Primary color
        /// </summary>
        public string Primary { get; set; } = "#FFFFFF";
        
        /// <summary>
        /// Secondary color
        /// </summary>
        public string Secondary { get; set; } = "#000000";
        
        /// <summary>
        /// Accent color
        /// </summary>
        public string Accent { get; set; } = "#FF0000";
        
        /// <summary>
        /// Background color
        /// </summary>
        public string Background { get; set; } = "#808080";
        
        /// <summary>
        /// Additional named colors
        /// </summary>
        public Dictionary<string, string> CustomColors { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Creates a color palette from a dictionary of color values
        /// </summary>
        /// <param name="colors">Dictionary of color name to hex value mappings</param>
        /// <returns>Color palette instance</returns>
        public static ColorPalette FromDictionary(Dictionary<string, string> colors)
        {
            var palette = new ColorPalette();
            
            if (colors.ContainsKey("primary"))
                palette.Primary = colors["primary"];
            if (colors.ContainsKey("secondary"))
                palette.Secondary = colors["secondary"];
            if (colors.ContainsKey("accent"))
                palette.Accent = colors["accent"];
            if (colors.ContainsKey("background"))
                palette.Background = colors["background"];
            
            // Add any additional colors
            foreach (var kvp in colors)
            {
                if (kvp.Key != "primary" && kvp.Key != "secondary" && kvp.Key != "accent" && kvp.Key != "background")
                {
                    palette.CustomColors[kvp.Key] = kvp.Value;
                }
            }
            
            return palette;
        }
        
        /// <summary>
        /// Gets a color by name
        /// </summary>
        /// <param name="colorName">Name of the color</param>
        /// <returns>Hex color value</returns>
        public string GetColor(string colorName)
        {
            return colorName.ToLower() switch
            {
                "primary" => Primary,
                "secondary" => Secondary,
                "accent" => Accent,
                "background" => Background,
                _ => CustomColors.ContainsKey(colorName) ? CustomColors[colorName] : "#FFFFFF"
            };
        }
    }
}
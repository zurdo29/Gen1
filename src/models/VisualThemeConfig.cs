using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration for visual theming
    /// </summary>
    public class VisualThemeConfig
    {
        /// <summary>
        /// Name of the visual theme
        /// </summary>
        [Required(ErrorMessage = "Theme name is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Theme name must be between 1 and 50 characters")]
        public string ThemeName { get; set; } = "default";
        
        /// <summary>
        /// Alias for ThemeName for backward compatibility
        /// </summary>
        public string Name 
        { 
            get => ThemeName; 
            set => ThemeName = value; 
        }
        
        /// <summary>
        /// Color palette for the theme
        /// </summary>
        public Dictionary<string, string> ColorPalette { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Tile sprite mappings
        /// </summary>
        public Dictionary<string, string> TileSprites { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Entity sprite mappings
        /// </summary>
        public Dictionary<string, string> EntitySprites { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Additional visual effects settings
        /// </summary>
        public Dictionary<string, object> EffectSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Validates the visual theme configuration and returns validation errors
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var context = new ValidationContext(this);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            if (!Validator.TryValidateObject(this, context, results, true))
            {
                errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            }

            // Validate color palette
            if (ColorPalette != null)
            {
                foreach (var colorEntry in ColorPalette)
                {
                    if (string.IsNullOrEmpty(colorEntry.Key))
                    {
                        errors.Add("Color palette contains empty color name");
                        continue;
                    }

                    if (!IsValidColorValue(colorEntry.Value))
                    {
                        errors.Add($"Invalid color value '{colorEntry.Value}' for color '{colorEntry.Key}'. Use hex format (#RRGGBB or #RRGGBBAA) or named colors");
                    }
                }
            }

            // Validate sprite paths
            if (TileSprites != null)
            {
                foreach (var spriteEntry in TileSprites)
                {
                    if (string.IsNullOrEmpty(spriteEntry.Key))
                    {
                        errors.Add("Tile sprites contain empty tile type");
                    }
                    if (string.IsNullOrEmpty(spriteEntry.Value))
                    {
                        errors.Add($"Empty sprite path for tile type '{spriteEntry.Key}'");
                    }
                }
            }

            if (EntitySprites != null)
            {
                foreach (var spriteEntry in EntitySprites)
                {
                    if (string.IsNullOrEmpty(spriteEntry.Key))
                    {
                        errors.Add("Entity sprites contain empty entity type");
                    }
                    if (string.IsNullOrEmpty(spriteEntry.Value))
                    {
                        errors.Add($"Empty sprite path for entity type '{spriteEntry.Key}'");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Creates a deep copy of this visual theme configuration
        /// </summary>
        public VisualThemeConfig Clone()
        {
            return new VisualThemeConfig
            {
                ThemeName = this.ThemeName,
                ColorPalette = new Dictionary<string, string>(this.ColorPalette),
                TileSprites = new Dictionary<string, string>(this.TileSprites),
                EntitySprites = new Dictionary<string, string>(this.EntitySprites),
                EffectSettings = new Dictionary<string, object>(this.EffectSettings)
            };
        }

        /// <summary>
        /// Checks if a color value is valid (hex format or named color)
        /// </summary>
        private static bool IsValidColorValue(string colorValue)
        {
            if (string.IsNullOrEmpty(colorValue))
                return false;

            // Check hex format (#RRGGBB or #RRGGBBAA)
            var hexPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$";
            if (Regex.IsMatch(colorValue, hexPattern))
                return true;

            // Check common named colors
            var namedColors = new[] { "red", "green", "blue", "yellow", "orange", "purple", "pink", "brown", 
                                    "black", "white", "gray", "grey", "cyan", "magenta", "lime", "navy", 
                                    "maroon", "olive", "teal", "silver", "aqua", "fuchsia" };
            return namedColors.Contains(colorValue.ToLower());
        }
    }
}
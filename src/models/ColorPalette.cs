using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Color palette configuration for visual themes
    /// </summary>
    public class ColorPalette
    {
        /// <summary>
        /// Primary color (hex format)
        /// </summary>
        [Required(ErrorMessage = "Primary color is required")]
        public string Primary { get; set; } = "#4CAF50";
        
        /// <summary>
        /// Secondary color (hex format)
        /// </summary>
        [Required(ErrorMessage = "Secondary color is required")]
        public string Secondary { get; set; } = "#2196F3";
        
        /// <summary>
        /// Accent color (hex format)
        /// </summary>
        [Required(ErrorMessage = "Accent color is required")]
        public string Accent { get; set; } = "#FF9800";
        
        /// <summary>
        /// Background color (hex format)
        /// </summary>
        [Required(ErrorMessage = "Background color is required")]
        public string Background { get; set; } = "#FFFFFF";
        
        /// <summary>
        /// Text color (hex format)
        /// </summary>
        [Required(ErrorMessage = "Text color is required")]
        public string Text { get; set; } = "#000000";

        /// <summary>
        /// Custom colors dictionary for additional colors
        /// </summary>
        public Dictionary<string, string> CustomColors { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a ColorPalette from a dictionary
        /// </summary>
        private static readonly HashSet<string> StandardColorKeys = new HashSet<string>
        {
            "primary", "secondary", "accent", "background", "text"
        };

        public static ColorPalette FromDictionary(Dictionary<string, string> colors)
        {
            if (colors == null)
                throw new ArgumentNullException(nameof(colors));
                
            var palette = new ColorPalette();
            
            if (colors.ContainsKey("primary")) palette.Primary = colors["primary"];
            if (colors.ContainsKey("secondary")) palette.Secondary = colors["secondary"];
            if (colors.ContainsKey("accent")) palette.Accent = colors["accent"];
            if (colors.ContainsKey("background")) palette.Background = colors["background"];
            if (colors.ContainsKey("text")) palette.Text = colors["text"];
            
            // Add any additional colors to CustomColors
            foreach (var kvp in colors)
            {
                if (!StandardColorKeys.Contains(kvp.Key))
                {
                    palette.CustomColors[kvp.Key] = kvp.Value;
                }
            }
            
            return palette;
        }

        /// <summary>
        /// Converts the color palette to a dictionary
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>
            {
                ["primary"] = Primary,
                ["secondary"] = Secondary,
                ["accent"] = Accent,
                ["background"] = Background,
                ["text"] = Text
            };
            
            // Add custom colors
            foreach (var kvp in CustomColors)
            {
                result[kvp.Key] = kvp.Value;
            }
            
            return result;
        }

        /// <summary>
        /// Validates the color palette
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var context = new ValidationContext(this);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            if (!Validator.TryValidateObject(this, context, results, true))
            {
                errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
            }

            // Validate hex color format
            var hexColorRegex = new Regex(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
            
            if (!string.IsNullOrEmpty(Primary) && !hexColorRegex.IsMatch(Primary))
                errors.Add($"Primary color '{Primary}' is not a valid hex color");
                
            if (!string.IsNullOrEmpty(Secondary) && !hexColorRegex.IsMatch(Secondary))
                errors.Add($"Secondary color '{Secondary}' is not a valid hex color");
                
            if (!string.IsNullOrEmpty(Accent) && !hexColorRegex.IsMatch(Accent))
                errors.Add($"Accent color '{Accent}' is not a valid hex color");
                
            if (!string.IsNullOrEmpty(Background) && !hexColorRegex.IsMatch(Background))
                errors.Add($"Background color '{Background}' is not a valid hex color");
                
            if (!string.IsNullOrEmpty(Text) && !hexColorRegex.IsMatch(Text))
                errors.Add($"Text color '{Text}' is not a valid hex color");

            return errors;
        }

        /// <summary>
        /// Creates a deep copy of this color palette
        /// </summary>
        public ColorPalette Clone()
        {
            return new ColorPalette
            {
                Primary = this.Primary,
                Secondary = this.Secondary,
                Accent = this.Accent,
                Background = this.Background,
                Text = this.Text,
                CustomColors = new Dictionary<string, string>(this.CustomColors)
            };
        }
    }
}
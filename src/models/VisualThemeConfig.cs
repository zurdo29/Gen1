using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration for visual theme settings
    /// </summary>
    public class VisualThemeConfig
    {
        /// <summary>
        /// Name of the visual theme
        /// </summary>
        [Required(ErrorMessage = "Theme name is required")]
        public string Name { get; set; } = "Default";
        
        /// <summary>
        /// Theme name for validation (alias for Name)
        /// </summary>
        public string ThemeName 
        { 
            get => Name; 
            set => Name = value; 
        }
        
        /// <summary>
        /// Color palette for the theme
        /// </summary>
        public ColorPalette ColorPalette { get; set; } = new ColorPalette();

        /// <summary>
        /// Tile sprite configurations
        /// </summary>
        public Dictionary<string, string> TileSprites { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Entity sprite configurations
        /// </summary>
        public Dictionary<string, string> EntitySprites { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Effect settings for visual effects
        /// </summary>
        public Dictionary<string, object> EffectSettings { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Validates the visual theme configuration
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

            // Validate color palette
            if (ColorPalette != null)
            {
                var paletteErrors = ColorPalette.Validate();
                errors.AddRange(paletteErrors.Select(e => $"Color Palette: {e}"));
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
                Name = this.Name,
                ColorPalette = this.ColorPalette?.Clone(),
                TileSprites = new Dictionary<string, string>(this.TileSprites),
                EntitySprites = new Dictionary<string, string>(this.EntitySprites),
                EffectSettings = new Dictionary<string, object>(this.EffectSettings)
            };
        }
    }
}
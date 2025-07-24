using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration for level generation
    /// </summary>
    public class GenerationConfig
    {
        /// <summary>
        /// Width of the generated level in tiles
        /// </summary>
        [Range(10, 1000, ErrorMessage = "Width must be between 10 and 1000 tiles")]
        public int Width { get; set; } = 50;
        
        /// <summary>
        /// Height of the generated level in tiles
        /// </summary>
        [Range(10, 1000, ErrorMessage = "Height must be between 10 and 1000 tiles")]
        public int Height { get; set; } = 50;
        
        /// <summary>
        /// Random seed for reproducible generation
        /// </summary>
        public int Seed { get; set; } = 0;
        
        /// <summary>
        /// Name of the generation algorithm to use
        /// </summary>
        [Required(ErrorMessage = "Generation algorithm is required")]
        public string GenerationAlgorithm { get; set; } = "perlin";
        
        /// <summary>
        /// Algorithm-specific parameters
        /// </summary>
        public Dictionary<string, object> AlgorithmParameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Terrain types to include in generation
        /// </summary>
        public List<string> TerrainTypes { get; set; } = new List<string> { "ground", "wall", "water" };
        
        /// <summary>
        /// Configuration for entities to place
        /// </summary>
        public List<EntityConfig> Entities { get; set; } = new List<EntityConfig>();
        
        /// <summary>
        /// Visual theme configuration
        /// </summary>
        public VisualThemeConfig VisualTheme { get; set; } = new VisualThemeConfig();
        
        /// <summary>
        /// Gameplay-specific configuration
        /// </summary>
        public GameplayConfig Gameplay { get; set; } = new GameplayConfig();

        /// <summary>
        /// Validates the configuration and returns validation errors
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

            // Additional custom validation
            if (!IsValidAlgorithm(GenerationAlgorithm))
            {
                errors.Add($"Invalid generation algorithm '{GenerationAlgorithm}'. Valid algorithms are: perlin, cellular, maze, rooms");
            }

            // Validate terrain types
            if (TerrainTypes != null && TerrainTypes.Count > 0)
            {
                var validTerrainTypes = new[] { "ground", "wall", "water", "grass", "stone", "sand", "lava", "ice" };
                foreach (var terrainType in TerrainTypes)
                {
                    if (string.IsNullOrEmpty(terrainType))
                    {
                        errors.Add("Terrain types list contains empty or null terrain type");
                    }
                    else if (!validTerrainTypes.Contains(terrainType.ToLower()))
                    {
                        errors.Add($"Invalid terrain type '{terrainType}'. Valid terrain types are: {string.Join(", ", validTerrainTypes)}");
                    }
                }
            }
            else if (TerrainTypes == null || TerrainTypes.Count == 0)
            {
                errors.Add("At least one terrain type must be specified");
            }

            // Validate entities
            if (Entities != null)
            {
                for (int i = 0; i < Entities.Count; i++)
                {
                    var entityErrors = Entities[i].Validate();
                    errors.AddRange(entityErrors.Select(e => $"Entity configuration {i + 1}: {e}"));
                }
            }

            // Validate visual theme
            if (VisualTheme != null)
            {
                var themeErrors = VisualTheme.Validate();
                errors.AddRange(themeErrors.Select(e => $"Visual Theme: {e}"));
            }

            // Validate gameplay config
            if (Gameplay != null)
            {
                var gameplayErrors = Gameplay.Validate();
                errors.AddRange(gameplayErrors.Select(e => $"Gameplay: {e}"));
            }

            return errors;
        }

        /// <summary>
        /// Applies default values for missing or invalid properties
        /// </summary>
        /// <returns>List of warnings for applied defaults</returns>
        public List<string> ApplyDefaults()
        {
            var warnings = new List<string>();

            if (Width < 10 || Width > 1000)
            {
                warnings.Add($"Width {Width} is invalid, using default value 50");
                Width = 50;
            }

            if (Height < 10 || Height > 1000)
            {
                warnings.Add($"Height {Height} is invalid, using default value 50");
                Height = 50;
            }

            if (string.IsNullOrEmpty(GenerationAlgorithm) || !IsValidAlgorithm(GenerationAlgorithm))
            {
                warnings.Add($"Generation algorithm '{GenerationAlgorithm}' is invalid, using default 'perlin'");
                GenerationAlgorithm = "perlin";
            }

            if (AlgorithmParameters == null)
            {
                warnings.Add("Algorithm parameters were null, using empty dictionary");
                AlgorithmParameters = new Dictionary<string, object>();
            }

            if (Entities == null)
            {
                warnings.Add("Entities list was null, using empty list");
                Entities = new List<EntityConfig>();
            }

            if (VisualTheme == null)
            {
                warnings.Add("Visual theme was null, using default theme");
                VisualTheme = new VisualThemeConfig();
            }

            if (Gameplay == null)
            {
                warnings.Add("Gameplay config was null, using default config");
                Gameplay = new GameplayConfig();
            }

            return warnings;
        }

        /// <summary>
        /// Creates a deep copy of this configuration
        /// </summary>
        public GenerationConfig Clone()
        {
            return new GenerationConfig
            {
                Width = this.Width,
                Height = this.Height,
                Seed = this.Seed,
                GenerationAlgorithm = this.GenerationAlgorithm,
                AlgorithmParameters = new Dictionary<string, object>(this.AlgorithmParameters),
                TerrainTypes = new List<string>(this.TerrainTypes),
                Entities = this.Entities.Select(e => e.Clone()).ToList(),
                VisualTheme = this.VisualTheme?.Clone(),
                Gameplay = this.Gameplay?.Clone()
            };
        }

        /// <summary>
        /// Checks if the algorithm name is valid
        /// </summary>
        private static bool IsValidAlgorithm(string algorithm)
        {
            var validAlgorithms = new[] { "perlin", "cellular", "maze", "rooms" };
            return !string.IsNullOrEmpty(algorithm) && validAlgorithms.Contains(algorithm.ToLower());
        }
    }
}
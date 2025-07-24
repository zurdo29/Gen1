using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration for entity placement
    /// </summary>
    public class EntityConfig
    {
        /// <summary>
        /// Type of entity to place
        /// </summary>
        [Required(ErrorMessage = "Entity type is required")]
        public EntityType Type { get; set; }
        
        /// <summary>
        /// Number of entities of this type to place
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Entity count must be between 0 and 1000")]
        public int Count { get; set; } = 1;
        
        /// <summary>
        /// Minimum distance from other entities
        /// </summary>
        [Range(0.0f, 100.0f, ErrorMessage = "Minimum distance must be between 0 and 100")]
        public float MinDistance { get; set; } = 1.0f;
        
        /// <summary>
        /// Maximum distance from player spawn
        /// </summary>
        [Range(0.0f, float.MaxValue, ErrorMessage = "Maximum distance from player must be positive")]
        public float MaxDistanceFromPlayer { get; set; } = float.MaxValue;
        
        /// <summary>
        /// Entity-specific properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Placement strategy to use
        /// </summary>
        [Required(ErrorMessage = "Placement strategy is required")]
        public string PlacementStrategy { get; set; } = "random";

        /// <summary>
        /// Validates the entity configuration and returns validation errors
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
            if (!IsValidPlacementStrategy(PlacementStrategy))
            {
                errors.Add($"Unknown placement strategy: {PlacementStrategy}. Valid strategies are: random, clustered, spread, near_walls, center, far_from_player, corners");
            }

            if (MinDistance > MaxDistanceFromPlayer)
            {
                errors.Add("Minimum distance cannot be greater than maximum distance from player");
            }

            return errors;
        }

        /// <summary>
        /// Creates a deep copy of this entity configuration
        /// </summary>
        public EntityConfig Clone()
        {
            return new EntityConfig
            {
                Type = this.Type,
                Count = this.Count,
                MinDistance = this.MinDistance,
                MaxDistanceFromPlayer = this.MaxDistanceFromPlayer,
                Properties = new Dictionary<string, object>(this.Properties),
                PlacementStrategy = this.PlacementStrategy
            };
        }

        /// <summary>
        /// Checks if the placement strategy is valid
        /// </summary>
        private static bool IsValidPlacementStrategy(string strategy)
        {
            var validStrategies = new[] { "random", "clustered", "spread", "near_walls", "center", "far_from_player", "corners" };
            return !string.IsNullOrEmpty(strategy) && validStrategies.Contains(strategy.ToLower());
        }
    }
}
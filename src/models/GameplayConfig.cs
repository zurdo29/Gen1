using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration for gameplay-specific settings
    /// </summary>
    public class GameplayConfig
    {
        /// <summary>
        /// Difficulty level (easy, normal, hard)
        /// </summary>
        public string Difficulty { get; set; } = "normal";
        
        /// <summary>
        /// List of objectives for the level
        /// </summary>
        public List<string> Objectives { get; set; } = new List<string>();
        
        /// <summary>
        /// Time limit in seconds (0 = no limit)
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Time limit must be non-negative")]
        public int TimeLimit { get; set; } = 0;
        
        /// <summary>
        /// Player starting health
        /// </summary>
        [Range(1, 100, ErrorMessage = "Player health must be between 1 and 100")]
        public int PlayerHealth { get; set; } = 3;
        
        /// <summary>
        /// Player movement speed
        /// </summary>
        [Range(0.1f, 10.0f, ErrorMessage = "Player speed must be between 0.1 and 10.0")]
        public float PlayerSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Victory conditions for the level
        /// </summary>
        public List<string> VictoryConditions { get; set; } = new List<string>();

        /// <summary>
        /// Game mechanics configuration
        /// </summary>
        public Dictionary<string, object> Mechanics { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Validates the gameplay configuration
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

            // Validate difficulty
            var validDifficulties = new[] { "easy", "normal", "hard" };
            if (!string.IsNullOrEmpty(Difficulty) && !validDifficulties.Contains(Difficulty.ToLower()))
            {
                errors.Add($"Invalid difficulty '{Difficulty}'. Valid difficulties are: {string.Join(", ", validDifficulties)}");
            }

            return errors;
        }

        /// <summary>
        /// Creates a deep copy of this gameplay configuration
        /// </summary>
        public GameplayConfig Clone()
        {
            return new GameplayConfig
            {
                Difficulty = this.Difficulty,
                Objectives = new List<string>(this.Objectives),
                TimeLimit = this.TimeLimit,
                PlayerHealth = this.PlayerHealth,
                PlayerSpeed = this.PlayerSpeed,
                VictoryConditions = new List<string>(this.VictoryConditions),
                Mechanics = new Dictionary<string, object>(this.Mechanics)
            };
        }
    }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration for gameplay mechanics
    /// </summary>
    public class GameplayConfig
    {
        /// <summary>
        /// Player movement speed
        /// </summary>
        [Range(0.1f, 50.0f, ErrorMessage = "Player speed must be between 0.1 and 50")]
        public float PlayerSpeed { get; set; } = 5.0f;
        
        /// <summary>
        /// Player health points
        /// </summary>
        [Range(1, 10000, ErrorMessage = "Player health must be between 1 and 10000")]
        public int PlayerHealth { get; set; } = 100;
        
        /// <summary>
        /// Game difficulty level
        /// </summary>
        [Required(ErrorMessage = "Difficulty level is required")]
        public string Difficulty { get; set; } = "normal";
        
        /// <summary>
        /// Time limit for the level (0 = no limit)
        /// </summary>
        [Range(0.0f, 3600.0f, ErrorMessage = "Time limit must be between 0 and 3600 seconds")]
        public float TimeLimit { get; set; } = 0.0f;
        
        /// <summary>
        /// Victory conditions
        /// </summary>
        public List<string> VictoryConditions { get; set; } = new List<string> { "reach_exit" };
        
        /// <summary>
        /// Game objectives (alias for VictoryConditions for backward compatibility)
        /// </summary>
        public List<string> Objectives 
        { 
            get => VictoryConditions; 
            set => VictoryConditions = value; 
        }
        
        /// <summary>
        /// Special gameplay mechanics
        /// </summary>
        public Dictionary<string, object> Mechanics { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Validates the gameplay configuration and returns validation errors
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
            if (!IsValidDifficulty(Difficulty))
            {
                errors.Add($"Unknown difficulty level: {Difficulty}. Valid difficulties are: easy, normal, hard, extreme");
            }

            // Validate victory conditions
            if (VictoryConditions == null || VictoryConditions.Count == 0)
            {
                errors.Add("At least one victory condition must be specified");
            }
            else
            {
                foreach (var condition in VictoryConditions)
                {
                    if (!IsValidVictoryCondition(condition))
                    {
                        errors.Add($"Unknown victory condition: {condition}. Valid conditions are: reach_exit, collect_all_items, defeat_all_enemies, survive_time, reach_score");
                    }
                }
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
                PlayerSpeed = this.PlayerSpeed,
                PlayerHealth = this.PlayerHealth,
                Difficulty = this.Difficulty,
                TimeLimit = this.TimeLimit,
                VictoryConditions = new List<string>(this.VictoryConditions),
                Mechanics = new Dictionary<string, object>(this.Mechanics)
            };
        }

        /// <summary>
        /// Checks if the difficulty level is valid
        /// </summary>
        private static bool IsValidDifficulty(string difficulty)
        {
            var validDifficulties = new[] { "easy", "normal", "hard", "extreme" };
            return !string.IsNullOrEmpty(difficulty) && validDifficulties.Contains(difficulty.ToLower());
        }

        /// <summary>
        /// Checks if the victory condition is valid
        /// </summary>
        private static bool IsValidVictoryCondition(string condition)
        {
            var validConditions = new[] { "reach_exit", "collect_all_items", "defeat_all_enemies", "survive_time", "reach_score" };
            return !string.IsNullOrEmpty(condition) && validConditions.Contains(condition.ToLower());
        }
    }
}
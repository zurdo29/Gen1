using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Validators
{
    /// <summary>
    /// Interface for validating generated levels
    /// </summary>
    public interface ILevelValidator
    {
        /// <summary>
        /// Validates a level and returns any issues found
        /// </summary>
        /// <param name="level">Level to validate</param>
        /// <param name="issues">List of validation issues</param>
        /// <returns>True if level is valid</returns>
        bool ValidateLevel(Level level, out List<string> issues);
        
        /// <summary>
        /// Checks if a level is playable
        /// </summary>
        /// <param name="level">Level to check</param>
        /// <returns>True if level is playable</returns>
        bool IsPlayable(Level level);
        
        /// <summary>
        /// Evaluates the quality of a level
        /// </summary>
        /// <param name="level">Level to evaluate</param>
        /// <returns>Quality score (0.0 to 1.0)</returns>
        float EvaluateQuality(Level level);
    }
}
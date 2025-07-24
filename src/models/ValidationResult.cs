using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Generic validation result container for errors and warnings
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Critical errors that prevent operation from proceeding
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Warnings about potential issues
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// True if validation passed (no errors)
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// True if there are any warnings
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Gets a formatted summary of all issues
        /// </summary>
        public string GetSummary()
        {
            var summary = new List<string>();
            
            if (Errors.Count > 0)
            {
                summary.Add($"Errors ({Errors.Count}):");
                summary.AddRange(Errors.Select(e => $"  - {e}"));
            }
            
            if (Warnings.Count > 0)
            {
                summary.Add($"Warnings ({Warnings.Count}):");
                summary.AddRange(Warnings.Select(w => $"  - {w}"));
            }
            
            if (IsValid && !HasWarnings)
            {
                summary.Add("Validation passed with no issues.");
            }
            
            return string.Join(Environment.NewLine, summary);
        }
    }
}
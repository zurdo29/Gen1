namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Result of configuration validation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the configuration is valid
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// Additional validation metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static ValidationResult Success(List<string>? warnings = null)
        {
            return new ValidationResult
            {
                IsValid = true,
                Warnings = warnings ?? new List<string>()
            };
        }
        
        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        public static ValidationResult Failure(List<string> errors, List<string>? warnings = null)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings ?? new List<string>()
            };
        }
    }
}
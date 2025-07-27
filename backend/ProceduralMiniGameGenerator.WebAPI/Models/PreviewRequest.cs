using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Request model for real-time preview generation
    /// </summary>
    public class PreviewRequest
    {
        /// <summary>
        /// Session identifier for tracking the preview request
        /// </summary>
        [Required]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Generation configuration for the preview
        /// </summary>
        [Required]
        public object Config { get; set; } = new();

        /// <summary>
        /// Whether to use real-time updates during generation
        /// </summary>
        public bool RealTimeUpdates { get; set; } = true;

        /// <summary>
        /// Preview quality level (affects generation speed vs quality)
        /// </summary>
        public string Quality { get; set; } = "medium";

        /// <summary>
        /// Debounce time in milliseconds for real-time updates
        /// </summary>
        public int DebounceMs { get; set; } = 300;
    }
}
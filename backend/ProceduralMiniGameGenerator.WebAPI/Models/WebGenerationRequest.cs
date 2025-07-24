using ProceduralMiniGameGenerator.Models;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Request model for web-based level generation
    /// </summary>
    public class WebGenerationRequest
    {
        /// <summary>
        /// Generation configuration
        /// </summary>
        [Required]
        public GenerationConfig Config { get; set; } = null!;
        
        /// <summary>
        /// Whether to include preview data in response
        /// </summary>
        public bool IncludePreview { get; set; } = true;
        
        /// <summary>
        /// Optional session ID for tracking
        /// </summary>
        public string? SessionId { get; set; }
        
        /// <summary>
        /// Whether to process generation in background
        /// </summary>
        public bool UseBackgroundProcessing { get; set; } = false;
    }
}
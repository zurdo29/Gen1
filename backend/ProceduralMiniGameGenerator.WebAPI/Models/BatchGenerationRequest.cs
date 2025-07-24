using ProceduralMiniGameGenerator.Models;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Request model for batch level generation
    /// </summary>
    public class BatchGenerationRequest
    {
        /// <summary>
        /// Base configuration for all generated levels
        /// </summary>
        [Required]
        public GenerationConfig BaseConfig { get; set; } = null!;
        
        /// <summary>
        /// Parameter variations to apply to the base configuration
        /// </summary>
        public List<ConfigVariation> Variations { get; set; } = new();
        
        /// <summary>
        /// Number of levels to generate per variation combination
        /// </summary>
        [Range(1, 50)]
        public int Count { get; set; } = 1;
        
        /// <summary>
        /// Optional session ID for tracking
        /// </summary>
        public string? SessionId { get; set; }
    }

    /// <summary>
    /// Represents a parameter variation for batch generation
    /// </summary>
    public class ConfigVariation
    {
        /// <summary>
        /// Parameter name to vary (e.g., "seed", "width", "generationAlgorithm")
        /// </summary>
        [Required]
        public string Parameter { get; set; } = string.Empty;
        
        /// <summary>
        /// List of values to use for this parameter
        /// </summary>
        [Required]
        public List<object> Values { get; set; } = new();
    }
}
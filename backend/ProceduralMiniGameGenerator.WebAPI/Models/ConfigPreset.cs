using ProceduralMiniGameGenerator.Models;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Configuration preset for saving and sharing
    /// </summary>
    public class ConfigPreset
    {
        /// <summary>
        /// Unique preset identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Display name for the preset
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional description of the preset
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// The generation configuration
        /// </summary>
        [Required]
        public GenerationConfig Config { get; set; } = null!;
        
        /// <summary>
        /// Tags for categorizing presets
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last modified timestamp
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether this preset is public
        /// </summary>
        public bool IsPublic { get; set; } = false;
        
        /// <summary>
        /// Creator identifier (optional)
        /// </summary>
        public string? CreatedBy { get; set; }
        
        /// <summary>
        /// Usage statistics
        /// </summary>
        public int UsageCount { get; set; } = 0;
    }
}
using ProceduralMiniGameGenerator.Models;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Request model for batch export of multiple levels
    /// </summary>
    public class BatchExportRequest
    {
        /// <summary>
        /// List of levels to export
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one level must be provided")]
        [MaxLength(50, ErrorMessage = "Maximum 50 levels can be exported in a single batch")]
        public List<Level> Levels { get; set; } = new List<Level>();
        
        /// <summary>
        /// Export format for all levels
        /// </summary>
        [Required]
        public string Format { get; set; } = "json";
        
        /// <summary>
        /// Export options applied to all levels
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Base filename pattern (will be appended with index)
        /// </summary>
        public string? BaseFileName { get; set; }
        
        /// <summary>
        /// Whether to package all exports into a single ZIP file
        /// </summary>
        public bool CreateZipPackage { get; set; } = true;
        
        /// <summary>
        /// Whether to include generation configuration in exports
        /// </summary>
        public bool IncludeGenerationConfig { get; set; } = true;
        
        /// <summary>
        /// Whether to include level statistics in exports
        /// </summary>
        public bool IncludeStatistics { get; set; } = true;
    }
}
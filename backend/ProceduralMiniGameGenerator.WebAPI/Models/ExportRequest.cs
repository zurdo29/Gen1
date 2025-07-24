using ProceduralMiniGameGenerator.Models;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Request model for level export
    /// </summary>
    public class ExportRequest
    {
        /// <summary>
        /// The level to export
        /// </summary>
        [Required]
        public Level Level { get; set; } = null!;
        
        /// <summary>
        /// Export format (json, xml, csv, unity)
        /// </summary>
        [Required]
        public string Format { get; set; } = "json";
        
        /// <summary>
        /// Export options specific to the format
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Optional filename for the export
        /// </summary>
        public string? FileName { get; set; }
        
        /// <summary>
        /// Whether to include generation configuration in export
        /// </summary>
        public bool IncludeGenerationConfig { get; set; } = true;
        
        /// <summary>
        /// Whether to include level statistics in export
        /// </summary>
        public bool IncludeStatistics { get; set; } = true;
    }
}
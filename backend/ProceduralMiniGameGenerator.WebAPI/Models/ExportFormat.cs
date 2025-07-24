namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Represents an available export format
    /// </summary>
    public class ExportFormat
    {
        /// <summary>
        /// Unique identifier for the format
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Display name for the format
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of the format
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// File extension for the format
        /// </summary>
        public string FileExtension { get; set; } = string.Empty;
        
        /// <summary>
        /// MIME type for the format
        /// </summary>
        public string MimeType { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether this format supports customization options
        /// </summary>
        public bool SupportsCustomization { get; set; } = false;
        
        /// <summary>
        /// Available customization options for this format
        /// </summary>
        public List<string> CustomizationOptions { get; set; } = new List<string>();
    }
}
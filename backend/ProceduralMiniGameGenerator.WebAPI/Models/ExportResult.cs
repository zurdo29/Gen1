namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Result of an export operation
    /// </summary>
    public class ExportResult
    {
        /// <summary>
        /// Whether the export was successful
        /// </summary>
        public bool Success => Errors.Count == 0;
        
        /// <summary>
        /// The exported file data
        /// </summary>
        public byte[]? FileData { get; set; }
        
        /// <summary>
        /// The filename for the exported file
        /// </summary>
        public string? FileName { get; set; }
        
        /// <summary>
        /// The MIME type of the exported file
        /// </summary>
        public string? MimeType { get; set; }
        
        /// <summary>
        /// Size of the exported file in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Time taken to complete the export
        /// </summary>
        public TimeSpan ExportTime { get; set; }
        
        /// <summary>
        /// List of errors that occurred during export
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// List of warnings generated during export
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
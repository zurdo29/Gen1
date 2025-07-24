namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Represents a file result for download operations
    /// </summary>
    public class FileResult
    {
        /// <summary>
        /// The file data as byte array
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();
        
        /// <summary>
        /// The filename for download
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// The MIME type of the file
        /// </summary>
        public string MimeType { get; set; } = "application/octet-stream";
    }
}
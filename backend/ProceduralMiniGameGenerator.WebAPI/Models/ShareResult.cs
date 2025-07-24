namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Result of creating a shareable configuration link
    /// </summary>
    public class ShareResult
    {
        /// <summary>
        /// Unique share identifier
        /// </summary>
        public string ShareId { get; set; } = string.Empty;
        
        /// <summary>
        /// Complete shareable URL
        /// </summary>
        public string ShareUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// When the share link expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Short description of what's being shared
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// QR code data URL for mobile sharing (optional)
        /// </summary>
        public string? QrCodeDataUrl { get; set; }
        
        /// <summary>
        /// Social media preview image data URL (optional)
        /// </summary>
        public string? PreviewImageUrl { get; set; }
        
        /// <summary>
        /// Thumbnail image data URL (optional)
        /// </summary>
        public string? ThumbnailUrl { get; set; }
        
        /// <summary>
        /// Additional sharing metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
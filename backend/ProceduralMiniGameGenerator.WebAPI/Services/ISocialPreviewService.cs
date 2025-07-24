namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for generating social media preview images
    /// </summary>
    public interface ISocialPreviewService
    {
        /// <summary>
        /// Generates a social media preview image for a configuration
        /// </summary>
        /// <param name="config">Configuration to preview</param>
        /// <param name="width">Preview image width (default: 1200)</param>
        /// <param name="height">Preview image height (default: 630)</param>
        /// <returns>Preview image as data URL</returns>
        Task<string> GeneratePreviewImageAsync(ProceduralMiniGameGenerator.Models.GenerationConfig config, int width = 1200, int height = 630);
        
        /// <summary>
        /// Generates a thumbnail preview for a configuration
        /// </summary>
        /// <param name="config">Configuration to preview</param>
        /// <param name="size">Thumbnail size (default: 300)</param>
        /// <returns>Thumbnail image as data URL</returns>
        Task<string> GenerateThumbnailAsync(ProceduralMiniGameGenerator.Models.GenerationConfig config, int size = 300);
    }
}
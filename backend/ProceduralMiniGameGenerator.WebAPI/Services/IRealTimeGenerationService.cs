using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for handling real-time generation with debouncing
    /// </summary>
    public interface IRealTimeGenerationService
    {
        /// <summary>
        /// Request a debounced preview generation
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="config">Generation configuration</param>
        /// <param name="debounceMs">Debounce delay in milliseconds</param>
        Task RequestDebouncedPreview(string sessionId, GenerationConfig config, int debounceMs = 500);

        /// <summary>
        /// Cancel any pending preview for a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        Task CancelPendingPreview(string sessionId);

        /// <summary>
        /// Get the current status of a session's preview generation
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Preview status</returns>
        Task<PreviewStatus> GetPreviewStatus(string sessionId);
    }

    /// <summary>
    /// Status of a preview generation
    /// </summary>
    public class PreviewStatus
    {
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = "idle"; // idle, pending, generating, completed, error
        public int Progress { get; set; } = 0;
        public string? Message { get; set; }
        public DateTime? LastUpdated { get; set; }
        public GenerationConfig? LastConfig { get; set; }
        public Level? LastResult { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
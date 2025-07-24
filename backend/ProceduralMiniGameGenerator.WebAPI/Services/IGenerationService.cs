using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for handling level generation operations
    /// </summary>
    public interface IGenerationService
    {
        /// <summary>
        /// Generates a level synchronously
        /// </summary>
        /// <param name="request">Generation request</param>
        /// <returns>Generated level</returns>
        Task<Level> GenerateLevelAsync(WebGenerationRequest request);
        
        /// <summary>
        /// Validates a generation configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateConfiguration(GenerationConfig config);
        
        /// <summary>
        /// Starts a background generation job
        /// </summary>
        /// <param name="request">Generation request</param>
        /// <returns>Job ID for tracking</returns>
        string StartBackgroundGeneration(WebGenerationRequest request);
        
        /// <summary>
        /// Gets the status of a background job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Job status information</returns>
        JobStatus GetJobStatus(string jobId);
        
        /// <summary>
        /// Starts a batch generation job
        /// </summary>
        /// <param name="request">Batch generation request</param>
        /// <returns>Job ID for tracking</returns>
        string StartBatchGeneration(BatchGenerationRequest request);
        
        /// <summary>
        /// Cancels a running batch generation job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>True if cancellation was successful</returns>
        bool CancelBatchGeneration(string jobId);
    }
}
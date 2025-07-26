using CoreModels = ProceduralMiniGameGenerator.Models;
using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;

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
        Task<CoreModels.Level> GenerateLevelAsync(WebApiModels.WebGenerationRequest request);
        
        /// <summary>
        /// Validates a generation configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        WebApiModels.ValidationResult ValidateConfiguration(CoreModels.GenerationConfig config);
        
        /// <summary>
        /// Starts a background generation job
        /// </summary>
        /// <param name="request">Generation request</param>
        /// <returns>Job ID for tracking</returns>
        string StartBackgroundGeneration(WebApiModels.WebGenerationRequest request);
        
        /// <summary>
        /// Gets the status of a background job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Job status information</returns>
        WebApiModels.JobStatus GetJobStatus(string jobId);
        
        /// <summary>
        /// Starts a batch generation job
        /// </summary>
        /// <param name="request">Batch generation request</param>
        /// <returns>Job ID for tracking</returns>
        string StartBatchGeneration(WebApiModels.BatchGenerationRequest request);
        
        /// <summary>
        /// Cancels a running batch generation job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>True if cancellation was successful</returns>
        bool CancelBatchGeneration(string jobId);
    }
}
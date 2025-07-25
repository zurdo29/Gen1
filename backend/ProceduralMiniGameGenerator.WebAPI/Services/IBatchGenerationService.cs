using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for batch generation operations
    /// </summary>
    public interface IBatchGenerationService
    {
        /// <summary>
        /// Starts a batch generation job
        /// </summary>
        /// <param name="request">Batch generation request</param>
        /// <returns>Result containing job ID or error</returns>
        Task<Result<string>> StartBatchGenerationAsync(BatchGenerationRequest request);

        /// <summary>
        /// Cancels a running batch generation job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result> CancelBatchGenerationAsync(string jobId);

        /// <summary>
        /// Validates a batch generation request
        /// </summary>
        /// <param name="request">Batch request to validate</param>
        /// <returns>Result indicating validation success or failure</returns>
        Result ValidateBatchRequest(BatchGenerationRequest request);

        /// <summary>
        /// Calculates the total number of levels that will be generated in a batch request
        /// </summary>
        /// <param name="request">Batch generation request</param>
        /// <returns>Total number of levels to be generated</returns>
        int CalculateTotalBatchLevels(BatchGenerationRequest request);
    }
}
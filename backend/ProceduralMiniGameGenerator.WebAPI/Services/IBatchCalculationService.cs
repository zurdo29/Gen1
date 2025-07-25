using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for batch generation calculations
    /// </summary>
    public interface IBatchCalculationService
    {
        /// <summary>
        /// Calculates the total number of levels that will be generated for a batch request
        /// </summary>
        /// <param name="request">The batch generation request</param>
        /// <returns>Total number of levels</returns>
        int CalculateTotalBatchLevels(BatchGenerationRequest request);

        /// <summary>
        /// Estimates the memory usage for a batch request
        /// </summary>
        /// <param name="request">The batch generation request</param>
        /// <returns>Estimated memory usage in MB</returns>
        double EstimateMemoryUsage(BatchGenerationRequest request);

        /// <summary>
        /// Estimates the processing time for a batch request
        /// </summary>
        /// <param name="request">The batch generation request</param>
        /// <returns>Estimated processing time</returns>
        TimeSpan EstimateProcessingTime(BatchGenerationRequest request);
    }
}
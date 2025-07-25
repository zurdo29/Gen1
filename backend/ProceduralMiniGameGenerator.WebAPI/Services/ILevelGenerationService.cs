using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for core level generation operations
    /// </summary>
    public interface ILevelGenerationService
    {
        /// <summary>
        /// Generates a level synchronously
        /// </summary>
        /// <param name="request">Generation request</param>
        /// <returns>Result containing the generated level or error</returns>
        Task<Result<Level>> GenerateLevelAsync(WebGenerationRequest request);

        /// <summary>
        /// Validates a generation configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Result indicating validation success or failure with errors</returns>
        Result ValidateConfiguration(GenerationConfig config);

        /// <summary>
        /// Determines if background processing should be used for the given configuration
        /// </summary>
        /// <param name="config">Generation configuration</param>
        /// <returns>True if background processing is recommended</returns>
        bool ShouldUseBackgroundProcessing(GenerationConfig config);

        /// <summary>
        /// Gets available generation algorithms
        /// </summary>
        /// <returns>List of supported algorithm names</returns>
        Task<Result<IReadOnlyList<string>>> GetAvailableAlgorithmsAsync();
    }
}
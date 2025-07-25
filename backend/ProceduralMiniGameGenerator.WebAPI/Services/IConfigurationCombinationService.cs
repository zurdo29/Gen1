using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for generating configuration combinations for batch processing
    /// </summary>
    public interface IConfigurationCombinationService
    {
        /// <summary>
        /// Generates all configuration combinations for a batch request
        /// </summary>
        List<BatchConfigurationItem> GenerateConfigurations(BatchGenerationRequest request);
        
        /// <summary>
        /// Calculates the total number of configurations that will be generated
        /// </summary>
        int CalculateTotalConfigurations(BatchGenerationRequest request);
    }

    /// <summary>
    /// Represents a single configuration item in a batch
    /// </summary>
    public record BatchConfigurationItem(
        GenerationConfig Config,
        int VariationIndex,
        int BatchIndex);
}
using CoreModels = ProceduralMiniGameGenerator.Models;
using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for configuration cloning and manipulation operations
    /// </summary>
    public interface IConfigurationCloningService
    {
        CoreModels.GenerationConfig CloneConfig(CoreModels.GenerationConfig original);
        void ApplyVariationToConfig(CoreModels.GenerationConfig config, List<WebApiModels.ConfigVariation> variations, List<object> values);
        List<(CoreModels.GenerationConfig config, int variationIndex, int batchIndex)> GenerateConfigurationCombinations(WebApiModels.BatchGenerationRequest request);
    }
}
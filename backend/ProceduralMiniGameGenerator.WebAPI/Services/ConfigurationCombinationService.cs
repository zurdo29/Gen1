using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for generating configuration combinations for batch processing
    /// </summary>
    public class ConfigurationCombinationService : IConfigurationCombinationService
    {
        private readonly IConfigurationCloningService _cloningService;
        private readonly IVariationApplicationService _variationService;

        public ConfigurationCombinationService(
            IConfigurationCloningService cloningService,
            IVariationApplicationService variationService)
        {
            _cloningService = cloningService;
            _variationService = variationService;
        }

        public List<BatchConfigurationItem> GenerateConfigurations(BatchGenerationRequest request)
        {
            var configurations = new List<BatchConfigurationItem>();

            if (request.Variations == null || request.Variations.Count == 0)
            {
                return GenerateSimpleBatch(request, configurations);
            }

            return GenerateVariationBatch(request, configurations);
        }

        public int CalculateTotalConfigurations(BatchGenerationRequest request)
        {
            if (request.Variations == null || request.Variations.Count == 0)
            {
                return request.Count;
            }

            var totalCombinations = request.Variations.Aggregate(1, (total, variation) => 
                total * Math.Max(variation.Values?.Count ?? 1, 1));

            return totalCombinations * request.Count;
        }

        private List<BatchConfigurationItem> GenerateSimpleBatch(
            BatchGenerationRequest request, 
            List<BatchConfigurationItem> configurations)
        {
            for (int i = 0; i < request.Count; i++)
            {
                var config = _cloningService.CloneConfiguration(request.BaseConfig);
                config.Seed = config.Seed + i;
                configurations.Add(new BatchConfigurationItem(config, 0, i));
            }
            return configurations;
        }

        private List<BatchConfigurationItem> GenerateVariationBatch(
            BatchGenerationRequest request, 
            List<BatchConfigurationItem> configurations)
        {
            var variationCombinations = GenerateVariationCombinations(request.Variations);
            
            for (int varIndex = 0; varIndex < variationCombinations.Count; varIndex++)
            {
                var variationValues = variationCombinations[varIndex];
                
                for (int batchIndex = 0; batchIndex < request.Count; batchIndex++)
                {
                    var config = _cloningService.CloneConfiguration(request.BaseConfig);
                    _variationService.ApplyVariations(config, request.Variations, variationValues);
                    config.Seed = config.Seed + batchIndex;
                    
                    configurations.Add(new BatchConfigurationItem(config, varIndex, batchIndex));
                }
            }
            
            return configurations;
        }

        private List<List<object>> GenerateVariationCombinations(List<ConfigVariation> variations)
        {
            if (variations.Count == 0) return new List<List<object>>();

            return GenerateVariationCombinationsIterative(variations).ToList();
        }

        private IEnumerable<List<object>> GenerateVariationCombinationsIterative(List<ConfigVariation> variations)
        {
            if (variations.Count == 0) yield break;

            var indices = new int[variations.Count];
            var maxIndices = variations.Select(v => v.Values.Count).ToArray();

            do
            {
                var combination = new List<object>(variations.Count);
                for (int i = 0; i < variations.Count; i++)
                {
                    combination.Add(variations[i].Values[indices[i]]);
                }
                yield return combination;
            }
            while (IncrementIndices(indices, maxIndices));
        }

        private static bool IncrementIndices(int[] indices, int[] maxIndices)
        {
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                indices[i]++;
                if (indices[i] < maxIndices[i])
                    return true;
                indices[i] = 0;
            }
            return false;
        }
    }
}
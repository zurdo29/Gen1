using System.Text.Json;
using CoreModels = ProceduralMiniGameGenerator.Models;
using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for configuration cloning and manipulation operations
    /// </summary>
    public class ConfigurationCloningService : IConfigurationCloningService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Creates a deep clone of a GenerationConfig using JSON serialization
        /// </summary>
        public CoreModels.GenerationConfig CloneConfig(CoreModels.GenerationConfig original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            try
            {
                var json = JsonSerializer.Serialize(original, JsonOptions);
                return JsonSerializer.Deserialize<CoreModels.GenerationConfig>(json, JsonOptions) 
                    ?? throw new InvalidOperationException("Failed to clone configuration - deserialization returned null");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to clone configuration due to JSON serialization error", ex);
            }
        }

        /// <summary>
        /// Applies variation values to a configuration using reflection or a strategy pattern
        /// </summary>
        public void ApplyVariationToConfig(CoreModels.GenerationConfig config, List<WebApiModels.ConfigVariation> variations, List<object> values)
        {
            var parameterAppliers = GetParameterAppliers();
            
            for (int i = 0; i < variations.Count && i < values.Count; i++)
            {
                var parameter = variations[i].Parameter;
                var value = values[i];
                
                if (parameterAppliers.TryGetValue(parameter, out var applier))
                {
                    applier(config, value);
                }
                else
                {
                    throw new ArgumentException($"Unknown parameter: {parameter}");
                }
            }
        }

        /// <summary>
        /// Generates all configuration combinations for batch generation
        /// </summary>
        public List<(CoreModels.GenerationConfig config, int variationIndex, int batchIndex)> GenerateConfigurationCombinations(WebApiModels.BatchGenerationRequest request)
        {
            var configurations = new List<(CoreModels.GenerationConfig, int, int)>();

            if (request.Variations == null || request.Variations.Count == 0)
            {
                return GenerateSimpleBatch(request, configurations);
            }

            return GenerateVariationBatch(request, configurations);
        }

        private List<(CoreModels.GenerationConfig, int, int)> GenerateSimpleBatch(
            WebApiModels.BatchGenerationRequest request, 
            List<(CoreModels.GenerationConfig, int, int)> configurations)
        {
            for (int i = 0; i < request.Count; i++)
            {
                var config = CloneConfig(request.BaseConfig);
                config.Seed = config.Seed + i;
                configurations.Add((config, 0, i));
            }
            return configurations;
        }

        private List<(CoreModels.GenerationConfig, int, int)> GenerateVariationBatch(
            WebApiModels.BatchGenerationRequest request, 
            List<(CoreModels.GenerationConfig, int, int)> configurations)
        {
            var variationCombinations = GenerateVariationCombinations(request.Variations);
            
            for (int varIndex = 0; varIndex < variationCombinations.Count; varIndex++)
            {
                var variationValues = variationCombinations[varIndex];
                
                for (int batchIndex = 0; batchIndex < request.Count; batchIndex++)
                {
                    var config = CloneConfig(request.BaseConfig);
                    ApplyVariationToConfig(config, request.Variations, variationValues);
                    config.Seed = config.Seed + batchIndex;
                    configurations.Add((config, varIndex, batchIndex));
                }
            }
            return configurations;
        }

        private Dictionary<string, Action<CoreModels.GenerationConfig, object>> GetParameterAppliers()
        {
            return new Dictionary<string, Action<CoreModels.GenerationConfig, object>>
            {
                ["seed"] = (config, value) => config.Seed = Convert.ToInt32(value),
                ["width"] = (config, value) => config.Width = Convert.ToInt32(value),
                ["height"] = (config, value) => config.Height = Convert.ToInt32(value),
                ["generationAlgorithm"] = (config, value) => config.GenerationAlgorithm = value.ToString() ?? config.GenerationAlgorithm,
                ["visualTheme.themeName"] = (config, value) => {
                    config.VisualTheme ??= new CoreModels.VisualThemeConfig();
                    config.VisualTheme.ThemeName = value.ToString() ?? config.VisualTheme.ThemeName;
                },
                ["gameplay.difficulty"] = (config, value) => {
                    config.Gameplay ??= new CoreModels.GameplayConfig();
                    config.Gameplay.Difficulty = value.ToString() ?? config.Gameplay.Difficulty;
                },
                ["gameplay.playerSpeed"] = (config, value) => {
                    config.Gameplay ??= new CoreModels.GameplayConfig();
                    config.Gameplay.PlayerSpeed = (float)Convert.ToDouble(value);
                },
                ["gameplay.timeLimit"] = (config, value) => {
                    config.Gameplay ??= new CoreModels.GameplayConfig();
                    config.Gameplay.TimeLimit = Convert.ToInt32(value);
                }
            };
        }

        private List<List<object>> GenerateVariationCombinations(List<WebApiModels.ConfigVariation> variations)
        {
            var combinations = new List<List<object>>();
            
            if (variations.Count == 0)
                return combinations;

            // Start with first variation
            foreach (var value in variations[0].Values)
            {
                combinations.Add(new List<object> { value });
            }

            // Add each subsequent variation
            for (int i = 1; i < variations.Count; i++)
            {
                var newCombinations = new List<List<object>>();
                
                foreach (var existingCombination in combinations)
                {
                    foreach (var value in variations[i].Values)
                    {
                        var newCombination = new List<object>(existingCombination) { value };
                        newCombinations.Add(newCombination);
                    }
                }
                
                combinations = newCombinations;
            }

            return combinations;
        }
    }
}
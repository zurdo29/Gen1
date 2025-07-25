using FluentValidation;
using Microsoft.Extensions.Options;
using ProceduralMiniGameGenerator.WebAPI.Configuration;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Validators
{
    /// <summary>
    /// Validator for WebGenerationRequest
    /// </summary>
    public class WebGenerationRequestValidator : AbstractValidator<WebGenerationRequest>
    {
        public WebGenerationRequestValidator(IValidator<GenerationConfig> configValidator)
        {
            RuleFor(x => x.Config)
                .NotNull()
                .WithMessage("Configuration is required")
                .SetValidator(configValidator);

            RuleFor(x => x.SessionId)
                .NotEmpty()
                .WithMessage("Session ID is required")
                .MaximumLength(100)
                .WithMessage("Session ID cannot exceed 100 characters");
        }
    }

    /// <summary>
    /// Validator for BatchGenerationRequest
    /// </summary>
    public class BatchGenerationRequestValidator : AbstractValidator<BatchGenerationRequest>
    {
        public BatchGenerationRequestValidator(
            IValidator<GenerationConfig> configValidator,
            IOptions<ApiConfiguration> apiConfig,
            IOptions<GenerationConfiguration> genConfig)
        {
            var apiSettings = apiConfig.Value;
            var genSettings = genConfig.Value;

            RuleFor(x => x.BaseConfig)
                .NotNull()
                .WithMessage("Base configuration is required")
                .SetValidator(configValidator);

            RuleFor(x => x.SessionId)
                .NotEmpty()
                .WithMessage("Session ID is required")
                .MaximumLength(100)
                .WithMessage("Session ID cannot exceed 100 characters");

            RuleFor(x => x.Count)
                .InclusiveBetween(1, apiSettings.MaxBatchCountPerVariation)
                .WithMessage($"Batch count must be between 1 and {apiSettings.MaxBatchCountPerVariation}");

            RuleFor(x => x.Variations)
                .Must(variations => variations == null || variations.Count <= genSettings.MaxVariationsPerBatch)
                .WithMessage($"Cannot have more than {genSettings.MaxVariationsPerBatch} variations");

            RuleForEach(x => x.Variations)
                .SetValidator(new ConfigVariationValidator(genSettings))
                .When(x => x.Variations != null);

            RuleFor(x => x)
                .Must(request => CalculateTotalBatchLevels(request) <= apiSettings.MaxBatchSize)
                .WithMessage($"Total batch size cannot exceed {apiSettings.MaxBatchSize} levels");
        }

        private static int CalculateTotalBatchLevels(BatchGenerationRequest request)
        {
            if (request.Variations == null || request.Variations.Count == 0)
            {
                return request.Count;
            }

            var totalCombinations = request.Variations.Aggregate(1, (total, variation) => 
                total * Math.Max(variation.Values?.Count ?? 1, 1));

            return totalCombinations * request.Count;
        }
    }

    /// <summary>
    /// Validator for ConfigVariation
    /// </summary>
    public class ConfigVariationValidator : AbstractValidator<ConfigVariation>
    {
        private static readonly Dictionary<string, Type> ValidParameters = new()
        {
            { "seed", typeof(int) },
            { "width", typeof(int) },
            { "height", typeof(int) },
            { "generationAlgorithm", typeof(string) },
            { "visualTheme.themeName", typeof(string) },
            { "gameplay.difficulty", typeof(string) },
            { "gameplay.playerSpeed", typeof(double) },
            { "gameplay.timeLimit", typeof(int) }
        };

        public ConfigVariationValidator(GenerationConfiguration settings)
        {
            RuleFor(x => x.Parameter)
                .NotEmpty()
                .WithMessage("Parameter name is required")
                .Must(param => ValidParameters.ContainsKey(param))
                .WithMessage($"Unknown parameter. Valid parameters: {string.Join(", ", ValidParameters.Keys)}");

            RuleFor(x => x.Values)
                .NotNull()
                .WithMessage("Values are required")
                .Must(values => values.Count > 0)
                .WithMessage("At least one value is required")
                .Must(values => values.Count <= settings.MaxValuesPerVariation)
                .WithMessage($"Cannot have more than {settings.MaxValuesPerVariation} values per parameter");

            RuleFor(x => x)
                .Must(variation => ValidateParameterValues(variation, settings))
                .WithMessage("Invalid parameter values");
        }

        private static bool ValidateParameterValues(ConfigVariation variation, GenerationConfiguration settings)
        {
            if (string.IsNullOrEmpty(variation.Parameter) || variation.Values == null)
                return false;

            if (!ValidParameters.TryGetValue(variation.Parameter, out var expectedType))
                return false;

            foreach (var value in variation.Values)
            {
                if (value == null) return false;

                if (!ValidateValueType(variation.Parameter, value, expectedType, settings))
                    return false;
            }

            return true;
        }

        private static bool ValidateValueType(string parameter, object value, Type expectedType, GenerationConfiguration settings)
        {
            try
            {
                if (expectedType == typeof(int))
                {
                    var intValue = Convert.ToInt32(value);
                    return parameter switch
                    {
                        "width" or "height" => intValue >= settings.MinLevelWidth && intValue <= settings.MaxLevelWidth,
                        "seed" => intValue >= 0,
                        "gameplay.timeLimit" => intValue > 0 && intValue <= settings.MaxTimeLimit,
                        _ => true
                    };
                }
                else if (expectedType == typeof(double))
                {
                    var doubleValue = Convert.ToDouble(value);
                    return parameter switch
                    {
                        "gameplay.playerSpeed" => doubleValue >= settings.MinPlayerSpeed && doubleValue <= settings.MaxPlayerSpeed,
                        _ => true
                    };
                }
                else if (expectedType == typeof(string))
                {
                    var stringValue = value.ToString();
                    if (string.IsNullOrEmpty(stringValue)) return false;

                    return parameter switch
                    {
                        "generationAlgorithm" => settings.SupportedAlgorithms.Contains(stringValue),
                        "visualTheme.themeName" => settings.SupportedThemes.Contains(stringValue),
                        "gameplay.difficulty" => settings.SupportedDifficulties.Contains(stringValue),
                        _ => true
                    };
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Validator for PreviewRequest
    /// </summary>
    public class PreviewRequestValidator : AbstractValidator<PreviewRequest>
    {
        public PreviewRequestValidator(
            IValidator<GenerationConfig> configValidator,
            IOptions<ApiConfiguration> apiConfig)
        {
            var settings = apiConfig.Value;

            RuleFor(x => x.Config)
                .NotNull()
                .WithMessage("Configuration is required")
                .SetValidator(configValidator);

            RuleFor(x => x.SessionId)
                .NotEmpty()
                .WithMessage("Session ID is required for real-time preview")
                .MaximumLength(100)
                .WithMessage("Session ID cannot exceed 100 characters");

            RuleFor(x => x.DebounceMs)
                .InclusiveBetween(0, settings.MaxPreviewDebounceMs)
                .WithMessage($"Debounce time must be between 0 and {settings.MaxPreviewDebounceMs} milliseconds");
        }
    }
}
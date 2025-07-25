using Microsoft.Extensions.Options;
using ProceduralMiniGameGenerator.WebAPI.Configuration;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Validators
{
    /// <summary>
    /// Service for validating configuration parameters
    /// </summary>
    public interface IParameterValidationService
    {
        bool IsValidParameter(string parameter);
        bool ValidateParameterValues(ConfigVariation variation);
        IReadOnlyDictionary<string, Type> GetValidParameters();
    }

    /// <summary>
    /// Implementation of parameter validation service
    /// </summary>
    public class ParameterValidationService : IParameterValidationService
    {
        private readonly GenerationConfiguration _settings;
        private readonly IReadOnlyDictionary<string, Type> _validParameters;

        public ParameterValidationService(IOptions<GenerationConfiguration> settings)
        {
            _settings = settings.Value;
            _validParameters = new Dictionary<string, Type>
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
        }

        public bool IsValidParameter(string parameter) => _validParameters.ContainsKey(parameter);

        public IReadOnlyDictionary<string, Type> GetValidParameters() => _validParameters;

        public bool ValidateParameterValues(ConfigVariation variation)
        {
            if (string.IsNullOrEmpty(variation.Parameter) || variation.Values == null)
                return false;

            if (!_validParameters.TryGetValue(variation.Parameter, out var expectedType))
                return false;

            return variation.Values.All(value => ValidateValueType(variation.Parameter, value, expectedType));
        }

        private bool ValidateValueType(string parameter, object value, Type expectedType)
        {
            try
            {
                return expectedType switch
                {
                    var t when t == typeof(int) => ValidateIntValue(parameter, Convert.ToInt32(value)),
                    var t when t == typeof(double) => ValidateDoubleValue(parameter, Convert.ToDouble(value)),
                    var t when t == typeof(string) => ValidateStringValue(parameter, value.ToString()!),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateIntValue(string parameter, int value) => parameter switch
        {
            "width" or "height" => value >= _settings.MinLevelWidth && value <= _settings.MaxLevelWidth,
            "seed" => value >= 0,
            "gameplay.timeLimit" => value > 0 && value <= _settings.MaxTimeLimit,
            _ => true
        };

        private bool ValidateDoubleValue(string parameter, double value) => parameter switch
        {
            "gameplay.playerSpeed" => value >= _settings.MinPlayerSpeed && value <= _settings.MaxPlayerSpeed,
            _ => true
        };

        private bool ValidateStringValue(string parameter, string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            return parameter switch
            {
                "generationAlgorithm" => _settings.SupportedAlgorithms.Contains(value),
                "visualTheme.themeName" => _settings.SupportedThemes.Contains(value),
                "gameplay.difficulty" => _settings.SupportedDifficulties.Contains(value),
                _ => true
            };
        }
    }
}
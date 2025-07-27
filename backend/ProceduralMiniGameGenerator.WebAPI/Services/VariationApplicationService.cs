using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for applying parameter variations using strategy pattern
    /// </summary>
    public class VariationApplicationService : IVariationApplicationService
    {
        private readonly Dictionary<string, IParameterApplicator> _applicators;

        public VariationApplicationService()
        {
            _applicators = new Dictionary<string, IParameterApplicator>
            {
                ["seed"] = new SeedApplicator(),
                ["width"] = new WidthApplicator(),
                ["height"] = new HeightApplicator(),
                ["generationAlgorithm"] = new AlgorithmApplicator(),
                ["visualTheme.themeName"] = new ThemeNameApplicator(),
                ["gameplay.difficulty"] = new DifficultyApplicator(),
                ["gameplay.playerSpeed"] = new PlayerSpeedApplicator(),
                ["gameplay.timeLimit"] = new TimeLimitApplicator()
            };
        }

        public void ApplyVariations(GenerationConfig config, List<ConfigVariation> variations, List<object> values)
        {
            for (int i = 0; i < variations.Count && i < values.Count; i++)
            {
                var parameter = variations[i].Parameter;
                var value = values[i];
                
                if (_applicators.TryGetValue(parameter, out var applicator))
                {
                    applicator.Apply(config, value);
                }
            }
        }

        public void RegisterApplicator(string parameter, IParameterApplicator applicator)
        {
            _applicators[parameter] = applicator;
        }
    }

    // Parameter applicator implementations with safe conversions
    public class SeedApplicator : IParameterApplicator
    {
        public void Apply(GenerationConfig config, object value)
        {
            if (ParameterApplicatorExtensions.TryConvertToInt32(value, out var intValue))
                config.Seed = intValue;
        }
    }

    public class WidthApplicator : IParameterApplicator
    {
        public void Apply(GenerationConfig config, object value)
        {
            if (ParameterApplicatorExtensions.TryConvertToInt32(value, out var intValue) && intValue > 0)
                config.Width = intValue;
        }
    }

    public class HeightApplicator : IParameterApplicator
    {
        public void Apply(GenerationConfig config, object value)
        {
            if (ParameterApplicatorExtensions.TryConvertToInt32(value, out var intValue) && intValue > 0)
                config.Height = intValue;
        }
    }

    public class AlgorithmApplicator : IParameterApplicator
    {
        public void Apply(GenerationConfig config, object value) => 
            config.GenerationAlgorithm = value.ToString() ?? config.GenerationAlgorithm;
    }

    public class ThemeNameApplicator : IParameterApplicator
    {
        public void Apply(GenerationConfig config, object value)
        {
            config.VisualTheme ??= new VisualThemeConfig();
            config.VisualTheme.ThemeName = value.ToString() ?? config.VisualTheme.ThemeName;
        }
    }

    public class DifficultyApplicator : IParameterApplicator
    {
        public void Apply(GenerationConfig config, object value)
        {
            config.Gameplay ??= new GameplayConfig();
            config.Gameplay.Difficulty = value.ToString() ?? config.Gameplay.Difficulty;
        }
    }

    public class PlayerSpeedApplicator : IParameterApplicator
    {
        public void Apply(GenerationConfig config, object value)
        {
            config.Gameplay ??= new GameplayConfig();
            if (ParameterApplicatorExtensions.TryConvertToDouble(value, out var doubleValue) && doubleValue > 0)
                config.Gameplay.PlayerSpeed = (float)doubleValue;
        }
    }

    public class TimeLimitApplicator : IParameterApplicator
    {
        public void Apply(GenerationConfig config, object value)
        {
            config.Gameplay ??= new GameplayConfig();
            if (ParameterApplicatorExtensions.TryConvertToInt32(value, out var intValue) && intValue > 0)
                config.Gameplay.TimeLimit = intValue;
        }
    }

    // Helper methods for safe type conversion
    public static class ParameterApplicatorExtensions
    {
        public static bool TryConvertToInt32(object value, out int result)
        {
            result = 0;
            return value switch
            {
                int intValue => (result = intValue) == intValue,
                string strValue => int.TryParse(strValue, out result),
                double doubleValue when doubleValue >= int.MinValue && doubleValue <= int.MaxValue => 
                    (result = (int)doubleValue) == (int)doubleValue,
                float floatValue when floatValue >= int.MinValue && floatValue <= int.MaxValue => 
                    (result = (int)floatValue) == (int)floatValue,
                _ => false
            };
        }

        public static bool TryConvertToDouble(object value, out double result)
        {
            result = 0.0;
            return value switch
            {
                double doubleValue => (result = doubleValue) == doubleValue,
                float floatValue => (result = floatValue) == floatValue,
                int intValue => (result = intValue) == intValue,
                string strValue => double.TryParse(strValue, out result),
                _ => false
            };
        }
    }
}
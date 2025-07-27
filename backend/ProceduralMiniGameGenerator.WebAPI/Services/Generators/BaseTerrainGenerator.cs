using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Generators;

namespace ProceduralMiniGameGenerator.WebAPI.Services.Generators
{
    /// <summary>
    /// Base class for terrain generators with common functionality
    /// </summary>
    public abstract class BaseTerrainGenerator : ITerrainGenerator
    {
        public abstract TileMap GenerateTerrain(GenerationConfig config, int seed);
        public abstract string GetAlgorithmName();
        public abstract Dictionary<string, object> GetDefaultParameters();
        
        public virtual bool SupportsParameters(Dictionary<string, object> parameters)
        {
            var errors = ValidateParameters(parameters);
            return errors.Count == 0;
        }

        public virtual List<string> ValidateParameters(Dictionary<string, object> parameters)
        {
            var errors = new List<string>();
            
            if (parameters == null)
            {
                errors.Add("Parameters cannot be null");
                return errors;
            }

            return ValidateSpecificParameters(parameters);
        }

        /// <summary>
        /// Override this method to provide generator-specific parameter validation
        /// </summary>
        protected abstract List<string> ValidateSpecificParameters(Dictionary<string, object> parameters);

        /// <summary>
        /// Helper method to validate numeric parameter ranges
        /// </summary>
        protected void ValidateNumericRange<T>(Dictionary<string, object> parameters, string paramName, 
            T minValue, T maxValue, List<string> errors) where T : IComparable<T>
        {
            if (!parameters.ContainsKey(paramName))
            {
                errors.Add($"Missing required parameter: {paramName}");
                return;
            }

            if (parameters[paramName] is not T value)
            {
                errors.Add($"Parameter '{paramName}' must be of type {typeof(T).Name}");
                return;
            }

            if (value.CompareTo(minValue) < 0 || value.CompareTo(maxValue) > 0)
            {
                errors.Add($"Parameter '{paramName}' must be between {minValue} and {maxValue}");
            }
        }
    }
}
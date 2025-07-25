using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for applying parameter variations to configurations
    /// </summary>
    public interface IVariationApplicationService
    {
        /// <summary>
        /// Applies variation values to a configuration
        /// </summary>
        void ApplyVariations(GenerationConfig config, List<ConfigVariation> variations, List<object> values);
        
        /// <summary>
        /// Registers a new parameter applicator
        /// </summary>
        void RegisterApplicator(string parameter, IParameterApplicator applicator);
    }

    /// <summary>
    /// Interface for applying a specific parameter variation
    /// </summary>
    public interface IParameterApplicator
    {
        void Apply(GenerationConfig config, object value);
    }
}
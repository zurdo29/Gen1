using CoreModels = ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services.ParameterAppliers
{
    /// <summary>
    /// Interface for applying parameter values to configuration objects
    /// </summary>
    public interface IParameterApplier
    {
        /// <summary>
        /// The parameter name this applier handles
        /// </summary>
        string ParameterName { get; }
        
        /// <summary>
        /// Applies the value to the configuration
        /// </summary>
        void Apply(CoreModels.GenerationConfig config, object value);
        
        /// <summary>
        /// Validates that the value is appropriate for this parameter
        /// </summary>
        bool IsValidValue(object value);
    }
}
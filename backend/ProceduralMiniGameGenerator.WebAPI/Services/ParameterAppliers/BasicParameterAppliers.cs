using CoreModels = ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services.ParameterAppliers
{
    public class SeedParameterApplier : IParameterApplier
    {
        public string ParameterName => "seed";
        
        public void Apply(CoreModels.GenerationConfig config, object value)
        {
            if (!IsValidValue(value))
                throw new ArgumentException($"Invalid seed value: {value}");
                
            config.Seed = Convert.ToInt32(value);
        }
        
        public bool IsValidValue(object value) => 
            int.TryParse(value?.ToString(), out _);
    }

    public class WidthParameterApplier : IParameterApplier
    {
        public string ParameterName => "width";
        
        public void Apply(CoreModels.GenerationConfig config, object value)
        {
            if (!IsValidValue(value))
                throw new ArgumentException($"Invalid width value: {value}");
                
            var width = Convert.ToInt32(value);
            if (width <= 0)
                throw new ArgumentException("Width must be positive");
                
            config.Width = width;
        }
        
        public bool IsValidValue(object value) => 
            int.TryParse(value?.ToString(), out var result) && result > 0;
    }

    public class HeightParameterApplier : IParameterApplier
    {
        public string ParameterName => "height";
        
        public void Apply(CoreModels.GenerationConfig config, object value)
        {
            if (!IsValidValue(value))
                throw new ArgumentException($"Invalid height value: {value}");
                
            var height = Convert.ToInt32(value);
            if (height <= 0)
                throw new ArgumentException("Height must be positive");
                
            config.Height = height;
        }
        
        public bool IsValidValue(object value) => 
            int.TryParse(value?.ToString(), out var result) && result > 0;
    }

    public class GenerationAlgorithmParameterApplier : IParameterApplier
    {
        public string ParameterName => "generationAlgorithm";
        
        public void Apply(CoreModels.GenerationConfig config, object value)
        {
            if (!IsValidValue(value))
                throw new ArgumentException($"Invalid generation algorithm: {value}");
                
            config.GenerationAlgorithm = value.ToString()!;
        }
        
        public bool IsValidValue(object value) => 
            !string.IsNullOrWhiteSpace(value?.ToString());
    }
}
using ProceduralMiniGameGenerator.Models;
using System.Text.Json;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for deep cloning generation configurations using JSON serialization
    /// </summary>
    public class ConfigurationCloningService : IConfigurationCloningService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigurationCloningService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public GenerationConfig CloneConfiguration(GenerationConfig original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            try
            {
                // Use JSON serialization for deep cloning - more efficient and less error-prone
                var json = JsonSerializer.Serialize(original, _jsonOptions);
                var cloned = JsonSerializer.Deserialize<GenerationConfig>(json, _jsonOptions);
                
                return cloned ?? throw new InvalidOperationException("Failed to clone configuration");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to clone configuration: {ex.Message}", ex);
            }
        }
    }
}
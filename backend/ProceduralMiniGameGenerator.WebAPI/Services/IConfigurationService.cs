using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for managing configuration presets and sharing
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets all available presets
        /// </summary>
        Task<List<ConfigPreset>> GetPresetsAsync();
        
        /// <summary>
        /// Gets a specific preset by ID
        /// </summary>
        Task<ConfigPreset?> GetPresetAsync(string id);
        
        /// <summary>
        /// Saves a new preset
        /// </summary>
        Task<ConfigPreset> SavePresetAsync(ConfigPreset preset);
        
        /// <summary>
        /// Updates an existing preset
        /// </summary>
        Task<ConfigPreset?> UpdatePresetAsync(string id, ConfigPreset preset);
        
        /// <summary>
        /// Deletes a preset
        /// </summary>
        Task<bool> DeletePresetAsync(string id);
        
        /// <summary>
        /// Creates a shareable link for a configuration
        /// </summary>
        Task<ShareResult> CreateShareLinkAsync(GenerationConfig config, TimeSpan? expiry = null);
        
        /// <summary>
        /// Retrieves a shared configuration by share ID
        /// </summary>
        Task<GenerationConfig?> GetSharedConfigurationAsync(string shareId);
        
        /// <summary>
        /// Validates a configuration and returns validation result
        /// </summary>
        Task<Models.ValidationResult> ValidateConfigurationAsync(GenerationConfig config);
    }
}
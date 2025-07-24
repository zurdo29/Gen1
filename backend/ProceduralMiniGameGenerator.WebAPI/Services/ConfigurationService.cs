using Microsoft.Extensions.Caching.Memory;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using System.Text.Json;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Implementation of configuration service using in-memory storage
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IMemoryCache _cache;
        private readonly ILoggerService _logger;
        private readonly IConfiguration _configuration;
        private readonly IQRCodeService _qrCodeService;
        private readonly ISocialPreviewService _socialPreviewService;
        
        private const string PRESETS_CACHE_KEY = "config_presets";
        private const string SHARED_CONFIGS_CACHE_KEY = "shared_configs";
        
        public ConfigurationService(
            IMemoryCache cache,
            ILoggerService logger,
            IConfiguration configuration,
            IQRCodeService qrCodeService,
            ISocialPreviewService socialPreviewService)
        {
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            _qrCodeService = qrCodeService;
            _socialPreviewService = socialPreviewService;
        }
        
        public async Task<List<ConfigPreset>> GetPresetsAsync()
        {
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, "Retrieving configuration presets");
            
            if (!_cache.TryGetValue(PRESETS_CACHE_KEY, out List<ConfigPreset>? presets))
            {
                presets = GetDefaultPresets();
                _cache.Set(PRESETS_CACHE_KEY, presets, TimeSpan.FromHours(24));
                
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    $"Loaded {presets.Count} default presets into cache");
            }
            
            return presets ?? new List<ConfigPreset>();
        }
        
        public async Task<ConfigPreset?> GetPresetAsync(string id)
        {
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Retrieving preset with ID: {id}");
            
            var presets = await GetPresetsAsync();
            var preset = presets.FirstOrDefault(p => p.Id == id);
            
            if (preset != null)
            {
                preset.UsageCount++;
                await SavePresetsToCache(presets);
            }
            
            return preset;
        }
        
        public async Task<ConfigPreset> SavePresetAsync(ConfigPreset preset)
        {
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Saving new preset: {preset.Name}");
            
            preset.Id = Guid.NewGuid().ToString();
            preset.CreatedAt = DateTime.UtcNow;
            preset.LastModified = DateTime.UtcNow;
            
            var presets = await GetPresetsAsync();
            presets.Add(preset);
            
            await SavePresetsToCache(presets);
            
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Successfully saved preset: {preset.Name} with ID: {preset.Id}");
            
            return preset;
        }
        
        public async Task<ConfigPreset?> UpdatePresetAsync(string id, ConfigPreset preset)
        {
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Updating preset with ID: {id}");
            
            var presets = await GetPresetsAsync();
            var existingPreset = presets.FirstOrDefault(p => p.Id == id);
            
            if (existingPreset == null)
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning, 
                    $"Preset not found for update: {id}");
                return null;
            }
            
            // Update properties
            existingPreset.Name = preset.Name;
            existingPreset.Description = preset.Description;
            existingPreset.Config = preset.Config;
            existingPreset.Tags = preset.Tags;
            existingPreset.IsPublic = preset.IsPublic;
            existingPreset.LastModified = DateTime.UtcNow;
            
            await SavePresetsToCache(presets);
            
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Successfully updated preset: {id}");
            
            return existingPreset;
        }
        
        public async Task<bool> DeletePresetAsync(string id)
        {
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Deleting preset with ID: {id}");
            
            var presets = await GetPresetsAsync();
            var preset = presets.FirstOrDefault(p => p.Id == id);
            
            if (preset == null)
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning, 
                    $"Preset not found for deletion: {id}");
                return false;
            }
            
            presets.Remove(preset);
            await SavePresetsToCache(presets);
            
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Successfully deleted preset: {id}");
            
            return true;
        }
        
        public async Task<ShareResult> CreateShareLinkAsync(GenerationConfig config, TimeSpan? expiry = null)
        {
            var shareId = Guid.NewGuid().ToString("N")[..12]; // Short ID
            var expiryTime = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(30));
            
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Creating share link with ID: {shareId}");
            
            // Store the shared configuration
            var sharedConfigs = GetSharedConfigs();
            sharedConfigs[shareId] = new SharedConfigEntry
            {
                Config = config,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiryTime
            };
            
            _cache.Set(SHARED_CONFIGS_CACHE_KEY, sharedConfigs, TimeSpan.FromDays(31));
            
            var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
            var shareUrl = $"{baseUrl}/api/configuration/share/{shareId}";
            
            // Generate QR code for the share URL
            string? qrCodeDataUrl = null;
            try
            {
                qrCodeDataUrl = await _qrCodeService.GenerateQRCodeDataUrlAsync(shareUrl);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning,
                    "Failed to generate QR code for share link",
                    new { ShareId = shareId, Error = ex.Message });
            }
            
            // Generate social media preview image
            string? previewImageUrl = null;
            try
            {
                previewImageUrl = await _socialPreviewService.GeneratePreviewImageAsync(config);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning,
                    "Failed to generate preview image for share link",
                    new { ShareId = shareId, Error = ex.Message });
            }
            
            // Generate thumbnail
            string? thumbnailUrl = null;
            try
            {
                thumbnailUrl = await _socialPreviewService.GenerateThumbnailAsync(config);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning,
                    "Failed to generate thumbnail for share link",
                    new { ShareId = shareId, Error = ex.Message });
            }
            
            var result = new ShareResult
            {
                ShareId = shareId,
                ShareUrl = shareUrl,
                ExpiresAt = expiryTime,
                Description = $"Level configuration ({config.Width}x{config.Height}, {config.GenerationAlgorithm})",
                QrCodeDataUrl = qrCodeDataUrl,
                PreviewImageUrl = previewImageUrl,
                ThumbnailUrl = thumbnailUrl,
                Metadata = new Dictionary<string, object>
                {
                    { "configSize", $"{config.Width}x{config.Height}" },
                    { "algorithm", config.GenerationAlgorithm },
                    { "createdAt", DateTime.UtcNow.ToString("O") }
                }
            };
            
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Successfully created share link: {shareId}");
            
            return result;
        }
        
        public async Task<GenerationConfig?> GetSharedConfigurationAsync(string shareId)
        {
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Retrieving shared configuration: {shareId}");
            
            var sharedConfigs = GetSharedConfigs();
            
            if (!sharedConfigs.TryGetValue(shareId, out var entry))
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning, 
                    $"Shared configuration not found: {shareId}");
                return null;
            }
            
            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning, 
                    $"Shared configuration expired: {shareId}");
                
                // Clean up expired entry
                sharedConfigs.Remove(shareId);
                _cache.Set(SHARED_CONFIGS_CACHE_KEY, sharedConfigs, TimeSpan.FromDays(31));
                
                return null;
            }
            
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                $"Successfully retrieved shared configuration: {shareId}");
            
            return entry.Config;
        }
        
        public async Task<Models.ValidationResult> ValidateConfigurationAsync(GenerationConfig config)
        {
            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                "Validating configuration");
            
            try
            {
                // Basic validation - can be enhanced later
                var errors = new List<string>();
                var warnings = new List<string>();
                
                if (config == null)
                {
                    errors.Add("Configuration cannot be null");
                }
                else
                {
                    if (config.Width < 10 || config.Width > 1000)
                        errors.Add("Width must be between 10 and 1000");
                    
                    if (config.Height < 10 || config.Height > 1000)
                        errors.Add("Height must be between 10 and 1000");
                    
                    if (string.IsNullOrEmpty(config.GenerationAlgorithm))
                        errors.Add("Generation algorithm is required");
                }
                
                var result = errors.Count == 0 
                    ? Models.ValidationResult.Success(warnings)
                    : Models.ValidationResult.Failure(errors, warnings);
                
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    $"Configuration validation completed. Valid: {result.IsValid}, Errors: {result.Errors.Count}, Warnings: {result.Warnings.Count}");
                
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Configuration validation failed");
                
                return Models.ValidationResult.Failure(new List<string> { $"Validation failed: {ex.Message}" });
            }
        }
        
        private async Task SavePresetsToCache(List<ConfigPreset> presets)
        {
            _cache.Set(PRESETS_CACHE_KEY, presets, TimeSpan.FromHours(24));
        }
        
        private Dictionary<string, SharedConfigEntry> GetSharedConfigs()
        {
            return _cache.GetOrCreate(SHARED_CONFIGS_CACHE_KEY, _ => new Dictionary<string, SharedConfigEntry>())
                ?? new Dictionary<string, SharedConfigEntry>();
        }
        
        private List<ConfigPreset> GetDefaultPresets()
        {
            return new List<ConfigPreset>
            {
                new ConfigPreset
                {
                    Id = "default-small",
                    Name = "Small Level",
                    Description = "A small 25x25 level perfect for quick testing",
                    Config = new GenerationConfig
                    {
                        Width = 25,
                        Height = 25,
                        GenerationAlgorithm = "perlin",
                        AlgorithmParameters = new Dictionary<string, object>
                        {
                            { "scale", 0.1 },
                            { "octaves", 3 }
                        }
                    },
                    Tags = new List<string> { "small", "testing", "quick" },
                    IsPublic = true
                },
                new ConfigPreset
                {
                    Id = "default-medium",
                    Name = "Medium Level",
                    Description = "A balanced 50x50 level with moderate complexity",
                    Config = new GenerationConfig
                    {
                        Width = 50,
                        Height = 50,
                        GenerationAlgorithm = "perlin",
                        AlgorithmParameters = new Dictionary<string, object>
                        {
                            { "scale", 0.1 },
                            { "octaves", 4 }
                        }
                    },
                    Tags = new List<string> { "medium", "balanced", "standard" },
                    IsPublic = true
                },
                new ConfigPreset
                {
                    Id = "default-maze",
                    Name = "Maze Level",
                    Description = "A maze-style level with corridors and rooms",
                    Config = new GenerationConfig
                    {
                        Width = 40,
                        Height = 40,
                        GenerationAlgorithm = "maze",
                        AlgorithmParameters = new Dictionary<string, object>
                        {
                            { "corridor_width", 1 },
                            { "room_probability", 0.3 }
                        }
                    },
                    Tags = new List<string> { "maze", "corridors", "puzzle" },
                    IsPublic = true
                }
            };
        }
        
        private class SharedConfigEntry
        {
            public GenerationConfig Config { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
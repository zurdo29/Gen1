using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Configuration;
using ProceduralMiniGameGenerator.WebAPI.Exceptions;
using ProceduralMiniGameGenerator.WebAPI.Models;
using System.Diagnostics;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for core level generation operations
    /// </summary>
    public class LevelGenerationService : ILevelGenerationService
    {
        private readonly ILogger<LevelGenerationService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfigurationParser _configurationParser;
        private readonly IGenerationManager _generationManager;
        private readonly IValidator<GenerationConfig> _configValidator;
        private readonly ApiConfiguration _apiConfig;
        private readonly GenerationConfiguration _genConfig;

        public LevelGenerationService(
            ILogger<LevelGenerationService> logger,
            IMemoryCache cache,
            IConfigurationParser configurationParser,
            IGenerationManager generationManager,
            IValidator<GenerationConfig> configValidator,
            IOptions<ApiConfiguration> apiConfig,
            IOptions<GenerationConfiguration> genConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _configurationParser = configurationParser ?? throw new ArgumentNullException(nameof(configurationParser));
            _generationManager = generationManager ?? throw new ArgumentNullException(nameof(generationManager));
            _configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
            _apiConfig = apiConfig?.Value ?? throw new ArgumentNullException(nameof(apiConfig));
            _genConfig = genConfig?.Value ?? throw new ArgumentNullException(nameof(genConfig));
        }

        /// <summary>
        /// Generates a level synchronously
        /// </summary>
        public async Task<Result<Level>> GenerateLevelAsync(WebGenerationRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Starting synchronous level generation for {SessionId} with operation {OperationId}",
                    request.SessionId, operationId);

                // Validate configuration
                var validationResult = ValidateConfiguration(request.Config);
                if (validationResult.IsFailure)
                {
                    return Result<Level>.Failure(validationResult.Error);
                }

                // Check cache first
                var cacheKey = GenerateCacheKey(request.Config);
                if (_cache.TryGetValue(cacheKey, out Level? cachedLevel) && cachedLevel != null)
                {
                    _logger.LogInformation("Returning cached level for operation {OperationId}", operationId);
                    return Result<Level>.Success(cachedLevel);
                }

                // Generate level
                var level = await Task.Run(() => _generationManager.GenerateLevel(request.Config));
                
                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _apiConfig.CacheExpiration,
                    SlidingExpiration = _apiConfig.CacheSlidingExpiration,
                    Size = CalculateLevelSize(level),
                    Priority = CacheItemPriority.Normal
                };
                
                _cache.Set(cacheKey, level, cacheOptions);
                
                stopwatch.Stop();
                
                _logger.LogInformation("Synchronous level generation completed for {SessionId} in {Duration}ms with {TotalTiles} tiles and {TotalEntities} entities",
                    request.SessionId, stopwatch.ElapsedMilliseconds, 
                    level.Terrain.Width * level.Terrain.Height, level.Entities.Count);

                return Result<Level>.Success(level);
            }
            catch (GenerationException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Generation failed for {SessionId} after {Duration}ms: {ErrorCode}",
                    request.SessionId, stopwatch.ElapsedMilliseconds, ex.ErrorCode);
                return Result<Level>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error during generation for {SessionId} after {Duration}ms",
                    request.SessionId, stopwatch.ElapsedMilliseconds);
                return Result<Level>.Failure("An unexpected error occurred during level generation");
            }
        }

        /// <summary>
        /// Validates a generation configuration
        /// </summary>
        public Result ValidateConfiguration(GenerationConfig config)
        {
            try
            {
                var validationResult = _configValidator.Validate(config);
                
                if (validationResult.IsValid)
                {
                    return Result.Success();
                }

                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                var errorMessage = $"Configuration validation failed: {string.Join(", ", errors)}";
                
                _logger.LogWarning("Configuration validation failed: {Errors}", string.Join("; ", errors));
                
                return Result.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during configuration validation");
                return Result.Failure("Configuration validation failed due to an internal error");
            }
        }

        /// <summary>
        /// Determines if background processing should be used for the given configuration
        /// </summary>
        public bool ShouldUseBackgroundProcessing(GenerationConfig config)
        {
            var totalTiles = config.Width * config.Height;
            var totalEntities = config.Entities?.Sum(e => e.Count) ?? 0;
            var parameterCount = config.AlgorithmParameters?.Count ?? 0;
            
            var shouldUseBackground = totalTiles > _apiConfig.BackgroundProcessingTileThreshold ||
                                    totalEntities > _apiConfig.BackgroundProcessingEntityThreshold ||
                                    parameterCount > _apiConfig.BackgroundProcessingParameterThreshold;

            _logger.LogDebug("Background processing decision for config: TotalTiles={TotalTiles}, TotalEntities={TotalEntities}, Parameters={Parameters}, UseBackground={UseBackground}",
                totalTiles, totalEntities, parameterCount, shouldUseBackground);

            return shouldUseBackground;
        }

        /// <summary>
        /// Gets available generation algorithms
        /// </summary>
        public async Task<Result<IReadOnlyList<string>>> GetAvailableAlgorithmsAsync()
        {
            try
            {
                // Return configured algorithms
                var algorithms = _genConfig.SupportedAlgorithms.ToList().AsReadOnly();
                
                _logger.LogDebug("Retrieved {AlgorithmCount} available algorithms", algorithms.Count);
                
                return await Task.FromResult(Result<IReadOnlyList<string>>.Success(algorithms));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available algorithms");
                return Result<IReadOnlyList<string>>.Failure("Failed to retrieve available algorithms");
            }
        }

        /// <summary>
        /// Generates a cache key for the given configuration
        /// </summary>
        private static string GenerateCacheKey(GenerationConfig config)
        {
            // Create a deterministic cache key based on configuration
            var keyComponents = new[]
            {
                config.Width.ToString(),
                config.Height.ToString(),
                config.Seed.ToString(),
                config.GenerationAlgorithm,
                string.Join(",", config.TerrainTypes ?? Enumerable.Empty<string>()),
                string.Join(",", config.Entities?.Select(e => $"{e.Type}:{e.Count}") ?? Enumerable.Empty<string>()),
                config.VisualTheme?.ThemeName ?? "default",
                string.Join(",", config.AlgorithmParameters?.Select(kvp => $"{kvp.Key}:{kvp.Value}") ?? Enumerable.Empty<string>())
            };

            var keyString = string.Join("|", keyComponents);
            return $"level:{keyString.GetHashCode():X}";
        }

        /// <summary>
        /// Calculates the approximate size of a level for caching purposes
        /// </summary>
        private static long CalculateLevelSize(Level level)
        {
            // Rough estimation of level size in bytes
            const int baseSize = 1024; // Base object overhead
            var terrainSize = level.Terrain.Width * level.Terrain.Height * 4; // Assuming 4 bytes per tile
            var entitySize = level.Entities.Count * 256; // Rough estimate per entity
            var nameSize = (level.Name?.Length ?? 0) * 2; // Unicode characters
            
            return baseSize + terrainSize + entitySize + nameSize;
        }
    }
}
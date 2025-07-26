using Hangfire;
using Microsoft.Extensions.Caching.Memory;
using CoreModels = ProceduralMiniGameGenerator.Models;
using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Generators;
using System.Diagnostics;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for handling level generation operations
    /// </summary>
    public class GenerationService : IGenerationService
    {
        private readonly ILoggerService _loggerService;
        private readonly IMemoryCache _cache;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IConfigurationParser _configurationParser;
        private readonly IGenerationManager _generationManager;

        public GenerationService(
            ILoggerService loggerService,
            IMemoryCache cache,
            IBackgroundJobClient backgroundJobClient,
            IConfigurationParser configurationParser,
            IGenerationManager generationManager)
        {
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            _configurationParser = configurationParser ?? throw new ArgumentNullException(nameof(configurationParser));
            _generationManager = generationManager ?? throw new ArgumentNullException(nameof(generationManager));
        }

        /// <summary>
        /// Generates a level synchronously
        /// </summary>
        public async Task<CoreModels.Level> GenerateLevelAsync(WebApiModels.WebGenerationRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Starting synchronous level generation",
                    new { 
                        OperationId = operationId,
                        SessionId = request.SessionId,
                        ConfigSize = $"{request.Config.Width}x{request.Config.Height}",
                        Algorithm = request.Config.GenerationAlgorithm,
                        Seed = request.Config.Seed
                    });

                // Validate configuration first
                var validationResult = ValidateConfiguration(request.Config);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"Invalid configuration: {string.Join(", ", validationResult.Errors)}");
                }

                // Check cache first
                var cacheKey = GenerateCacheKey(request.Config);
                if (_cache.TryGetValue(cacheKey, out CoreModels.Level? cachedLevel) && CoreModels.cachedLevel != null)
                {
                    await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                        "Returning cached level",
                        new { OperationId = operationId, CacheKey = cacheKey });
                    return cachedLevel;
                }

                // Generate level
                var level = await Task.Run(() => _generationManager.GenerateLevel(request.Config));
                
                // Cache the result for 10 minutes
                _cache.Set(cacheKey, level, TimeSpan.FromMinutes(10));
                
                stopwatch.Stop();
                
                await _loggerService.LogPerformanceAsync(
                    "SynchronousLevelGeneration",
                    stopwatch.Elapsed,
                    new {
                        OperationId = operationId,
                        TotalTiles = level.Terrain.Width * level.Terrain.Height,
                        TotalEntities = level.Entities.Count,
                        Algorithm = request.Config.GenerationAlgorithm
                    });

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Synchronous level generation completed successfully",
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level.Name
                    });

                return level;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _loggerService.LogErrorAsync(ex,
                    "Synchronous level generation failed",
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        SessionId = request.SessionId
                    });
                throw;
            }
        }

        /// <summary>
        /// Validates a generation configuration
        /// </summary>
        public WebApiModels.ValidationResult ValidateConfiguration(CoreModels.GenerationConfig config)
        {
            try
            {
                // Use the configuration parser's validation
                var isValid = _configurationParser.ValidateConfig(config, out var errors);
                
                if (isValid)
                {
                    return WebApiModels.ValidationResult.Success();
                }
                else
                {
                    return WebApiModels.ValidationResult.Failure(errors);
                }
            }
            catch (Exception ex)
            {
                return WebApiModels.ValidationResult.Failure(new List<string> { $"Validation failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Starts a background generation job
        /// </summary>
        public string StartBackgroundGeneration(WebApiModels.WebGenerationRequest request)
        {
            var jobId = Guid.NewGuid().ToString();
            
            // Create initial job status
            var jobStatus = new WebApiModels.JobStatus
            {
                JobId = jobId,
                Status = WebApiModels.JobStatusType.Pending,
                Progress = 0,
                CreatedAt = DateTime.UtcNow,
                Metadata = new WebApiModels.JobMetadata
                {
                    SessionId = request.SessionId ?? string.Empty,
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["configSize"] = $"{request.Config.Width}x{request.Config.Height}",
                        ["algorithm"] = request.Config.GenerationAlgorithm
                    }
                }
            };
            
            // Store initial status in cache
            _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(1));
            
            // Enqueue background job
            _backgroundJobClient.Enqueue<GenerationService>(x => x.ProcessBackgroundGeneration(jobId, request));
            
            return jobId;
        }

        /// <summary>
        /// Gets the status of a background job
        /// </summary>
        public WebApiModels.JobStatus GetJobStatus(string jobId)
        {
            if (_cache.TryGetValue($"job:{jobId}", out WebApiModels.JobStatus? status) && status != null)
            {
                return status;
            }
            
            // Job not found or expired
            return new WebApiModels.JobStatus
            {
                JobId = jobId,
                JobType = WebApiModels.JobType.Generation,
                Status = WebApiModels.JobStatusType.Failed,
                Progress = 0,
                ErrorMessage = "Job not found or has expired"
            };
        }

        /// <summary>
        /// Processes a background generation job (called by Hangfire)
        /// </summary>
        [Queue("generation")]
        public async Task ProcessBackgroundGeneration(string jobId, WebApiModels.WebGenerationRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Update job status to running
                var jobStatus = GetJobStatus(jobId);
                jobStatus.Status = WebApiModels.JobStatusType.Running;
                jobStatus.Progress = 10;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(1));

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Starting background level generation",
                    new { 
                        JobId = jobId,
                        SessionId = request.SessionId,
                        ConfigSize = $"{request.Config.Width}x{request.Config.Height}",
                        Algorithm = request.Config.GenerationAlgorithm
                    });

                // Validate configuration
                jobStatus.Progress = 20;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(1));
                
                var validationResult = ValidateConfiguration(request.Config);
                if (!validationResult.IsValid)
                {
                    jobStatus.Status = WebApiModels.JobStatusType.Failed;
                    jobStatus.ErrorMessage = $"Invalid configuration: {string.Join(", ", validationResult.Errors)}";
                    jobStatus.CompletedAt = DateTime.UtcNow;
                    _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(1));
                    return;
                }

                // Generate level
                jobStatus.Progress = 50;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(1));
                
                var level = await Task.Run(() => _generationManager.GenerateLevel(request.Config));
                
                // Complete job
                jobStatus.Status = WebApiModels.JobStatusType.Completed;
                jobStatus.Progress = 100;
                jobStatus.Result = level;
                jobStatus.CompletedAt = DateTime.UtcNow;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(1));
                
                stopwatch.Stop();
                
                await _loggerService.LogPerformanceAsync(
                    "BackgroundLevelGeneration",
                    stopwatch.Elapsed,
                    new {
                        JobId = jobId,
                        TotalTiles = level.Terrain.Width * level.Terrain.Height,
                        TotalEntities = level.Entities.Count,
                        Algorithm = request.Config.GenerationAlgorithm
                    });

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Background level generation completed successfully",
                    new { 
                        JobId = jobId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level.Name
                    });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                var jobStatus = GetJobStatus(jobId);
                jobStatus.Status = WebApiModels.JobStatusType.Failed;
                jobStatus.ErrorMessage = ex.Message;
                jobStatus.CompletedAt = DateTime.UtcNow;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(1));
                
                await _loggerService.LogErrorAsync(ex,
                    "Background level generation failed",
                    new { 
                        JobId = jobId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        SessionId = request.SessionId
                    });
            }
        }

        /// <summary>
        /// Starts a batch generation job
        /// </summary>
        public string StartBatchGeneration(WebApiModels.BatchGenerationRequest request)
        {
            var jobId = Guid.NewGuid().ToString();
            
            // Calculate total levels to generate
            var totalLevels = CalculateTotalBatchLevels(request);
            
            // Create initial job status
            var jobStatus = new WebApiModels.JobStatus
            {
                JobId = jobId,
                JobType = WebApiModels.JobType.BatchGeneration,
                Status = WebApiModels.JobStatusType.Pending,
                Progress = 0,
                CreatedAt = DateTime.UtcNow,
                Metadata = new WebApiModels.JobMetadata
                {
                    SessionId = request.SessionId ?? string.Empty,
                    TotalItems = totalLevels,
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["baseConfigSize"] = $"{request.BaseConfig.Width}x{request.BaseConfig.Height}",
                        ["algorithm"] = request.BaseConfig.GenerationAlgorithm,
                        ["totalLevels"] = totalLevels.ToString(),
                        ["variationCount"] = request.Variations.Count.ToString(),
                        ["batchCount"] = request.Count.ToString(),
                        ["type"] = "batch"
                    }
                }
            };
            
            // Store initial status in cache
            _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(2));
            
            // Enqueue background job
            _backgroundJobClient.Enqueue<GenerationService>(x => x.ProcessBatchGeneration(jobId, request));
            
            return jobId;
        }

        /// <summary>
        /// Cancels a running batch generation job
        /// </summary>
        public bool CancelBatchGeneration(string jobId)
        {
            var jobStatus = GetJobStatus(jobId);

            if (jobStatus.Status == WebApiModels.JobStatusType.NotFound)
            {
                return false;
            }

            if (jobStatus.Status == WebApiModels.JobStatusType.Completed || jobStatus.Status == WebApiModels.JobStatusType.Failed || jobStatus.Status == WebApiModels.JobStatusType.Cancelled)
            {
                return false; // Cannot cancel completed/failed/already cancelled jobs
            }
            
            // Update job status to cancelled
            jobStatus.Status = WebApiModels.JobStatusType.Cancelled;
            jobStatus.CompletedAt = DateTime.UtcNow;
            jobStatus.ErrorMessage = "Job was cancelled by user request";
            _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(1));
            
            // Note: In a real implementation, you'd also need to signal the background job to stop
            // This would require additional coordination mechanisms
            
            return true;
        }

        /// <summary>
        /// Processes a batch generation job (called by Hangfire)
        /// </summary>
        [Queue("batch-generation")]
        public async Task ProcessBatchGeneration(string jobId, WebApiModels.BatchGenerationRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new List<object>();
            
            try
            {
                // Update job status to running
                var jobStatus = GetJobStatus(jobId);
                jobStatus.Status = WebApiModels.JobStatusType.Running;
                jobStatus.Progress = 5;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(2));

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Starting batch level generation",
                    new { 
                        JobId = jobId,
                        SessionId = request.SessionId,
                        TotalLevels = CalculateTotalBatchLevels(request),
                        VariationCount = request.Variations.Count,
                        BatchCount = request.Count
                    });

                // Validate base configuration
                jobStatus.Progress = 10;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(2));
                
                var validationResult = ValidateConfiguration(request.BaseConfig);
                if (!validationResult.IsValid)
                {
                    jobStatus.Status = WebApiModels.JobStatusType.Failed;
                    jobStatus.ErrorMessage = $"Invalid base configuration: {string.Join(", ", validationResult.Errors)}";
                    jobStatus.CompletedAt = DateTime.UtcNow;
                    _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(2));
                    return;
                }

                // Generate all configuration combinations
                var configurations = GenerateConfigurationCombinations(request);
                var totalConfigurations = configurations.Count;
                var completedCount = 0;

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generated configuration combinations",
                    new { 
                        JobId = jobId,
                        TotalConfigurations = totalConfigurations
                    });

                // Generate levels for each configuration
                foreach (var (config, variationIndex, batchIndex) in configurations)
                {
                    // Check if job was cancelled
                    var currentStatus = GetJobStatus(jobId);
                    if (currentStatus.Status == WebApiModels.JobStatusType.Cancelled)
                    {
                        await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                            "Batch generation cancelled",
                            new { JobId = jobId, CompletedCount = completedCount, TotalCount = totalConfigurations });
                        return;
                    }

                    try
                    {
                        var levelStopwatch = Stopwatch.StartNew();
                        var level = await Task.Run(() => _generationManager.GenerateLevel(config));
                        levelStopwatch.Stop();

                        // Create result object
                        var result = new
                        {
                            id = Guid.NewGuid().ToString(),
                            level = level,
                            variationIndex = variationIndex,
                            batchIndex = batchIndex,
                            generatedAt = DateTime.UtcNow,
                            generationTime = levelStopwatch.ElapsedMilliseconds
                        };

                        results.Add(result);
                        completedCount++;

                        // Update progress
                        var progress = 10 + (int)((completedCount / (double)totalConfigurations) * 85);
                        jobStatus.Progress = Math.Min(progress, 95);
                        _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(2));

                        await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                            "Generated level in batch",
                            new { 
                                JobId = jobId,
                                LevelId = result.id,
                                VariationIndex = variationIndex,
                                BatchIndex = batchIndex,
                                GenerationTimeMs = levelStopwatch.ElapsedMilliseconds,
                                Progress = $"{completedCount}/{totalConfigurations}"
                            });
                    }
                    catch (Exception levelEx)
                    {
                        await _loggerService.LogErrorAsync(levelEx,
                            "Failed to generate level in batch",
                            new { 
                                JobId = jobId,
                                VariationIndex = variationIndex,
                                BatchIndex = batchIndex,
                                Config = config
                            });

                        // Continue with next level instead of failing entire batch
                        completedCount++;
                    }
                }

                // Complete job
                jobStatus.Status = WebApiModels.JobStatusType.Completed;
                jobStatus.Progress = 100;
                jobStatus.Result = results;
                jobStatus.CompletedAt = DateTime.UtcNow;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(2));
                
                stopwatch.Stop();
                
                await _loggerService.LogPerformanceAsync(
                    "BatchLevelGeneration",
                    stopwatch.Elapsed,
                    new {
                        JobId = jobId,
                        TotalLevels = results.Count,
                        SuccessfulLevels = results.Count,
                        FailedLevels = totalConfigurations - results.Count,
                        AverageGenerationTime = results.Count > 0 ? results.Average(r => ((dynamic)r).generationTime) : 0
                    });

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Batch level generation completed successfully",
                    new { 
                        JobId = jobId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        TotalLevels = results.Count,
                        SuccessRate = $"{results.Count}/{totalConfigurations}"
                    });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                var jobStatus = GetJobStatus(jobId);
                jobStatus.Status = WebApiModels.JobStatusType.Failed;
                jobStatus.ErrorMessage = ex.Message;
                jobStatus.Result = results; // Include partial results
                jobStatus.CompletedAt = DateTime.UtcNow;
                _cache.Set($"job:{jobId}", jobStatus, TimeSpan.FromHours(2));
                
                await _loggerService.LogErrorAsync(ex,
                    "Batch level generation failed",
                    new { 
                        JobId = jobId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        SessionId = request.SessionId,
                        PartialResults = results.Count
                    });
            }
        }

        /// <summary>
        /// Generates all configuration combinations for batch generation
        /// </summary>
        private List<(CoreModels.GenerationConfig config, int variationIndex, int batchIndex)> GenerateConfigurationCombinations(WebApiModels.BatchGenerationRequest request)
        {
            var configurations = new List<(CoreModels.GenerationConfig, int, int)>();

            if (request.Variations == null || request.Variations.Count == 0)
            {
                // No variations, just generate multiple copies of base config with different seeds
                for (int i = 0; i < request.Count; i++)
                {
                    var config = CloneConfig(request.BaseConfig);
                    config.Seed = config.Seed + i; // Vary seed for each batch
                    configurations.Add((config, 0, i));
                }
            }
            else
            {
                // Generate all combinations of variations
                var variationCombinations = GenerateVariationCombinations(request.Variations);
                
                for (int varIndex = 0; varIndex < variationCombinations.Count; varIndex++)
                {
                    var variationValues = variationCombinations[varIndex];
                    
                    for (int batchIndex = 0; batchIndex < request.Count; batchIndex++)
                    {
                        var config = CloneConfig(request.BaseConfig);
                        
                        // Apply variation values to config
                        ApplyVariationToConfig(config, request.Variations, variationValues);
                        
                        // Vary seed for each batch within the same variation
                        config.Seed = config.Seed + batchIndex;
                        
                        configurations.Add((config, varIndex, batchIndex));
                    }
                }
            }

            return configurations;
        }

        /// <summary>
        /// Generates all combinations of variation values
        /// </summary>
        private List<List<object>> GenerateVariationCombinations(List<WebApiModels.ConfigVariation> variations)
        {
            var combinations = new List<List<object>>();
            
            if (variations.Count == 0)
            {
                return combinations;
            }

            // Start with first variation
            foreach (var value in variations[0].Values)
            {
                combinations.Add(new List<object> { value });
            }

            // Add each subsequent variation
            for (int i = 1; i < variations.Count; i++)
            {
                var newCombinations = new List<List<object>>();
                
                foreach (var existingCombination in combinations)
                {
                    foreach (var value in variations[i].Values)
                    {
                        var newCombination = new List<object>(existingCombination) { value };
                        newCombinations.Add(newCombination);
                    }
                }
                
                combinations = newCombinations;
            }

            return combinations;
        }

        /// <summary>
        /// Applies variation values to a configuration
        /// </summary>
        private void ApplyVariationToConfig(CoreModels.GenerationConfig config, List<WebApiModels.ConfigVariation> variations, List<object> values)
        {
            for (int i = 0; i < variations.Count && i < values.Count; i++)
            {
                var parameter = variations[i].Parameter;
                var value = values[i];
                
                // Apply the parameter value to the config
                switch (parameter)
                {
                    case "seed":
                        config.Seed = Convert.ToInt32(value);
                        break;
                    case "width":
                        config.Width = Convert.ToInt32(value);
                        break;
                    case "height":
                        config.Height = Convert.ToInt32(value);
                        break;
                    case "generationAlgorithm":
                        config.GenerationAlgorithm = value.ToString() ?? config.GenerationAlgorithm;
                        break;
                    case "visualTheme.themeName":
                        if (config.VisualTheme == null) config.VisualTheme = new CoreModels.VisualThemeConfig();
                        config.VisualTheme.ThemeName = value.ToString() ?? config.VisualTheme.ThemeName;
                        break;
                    case "gameplay.difficulty":
                        if (config.Gameplay == null) config.Gameplay = new CoreModels.GameplayConfig();
                        config.Gameplay.Difficulty = value.ToString() ?? config.Gameplay.Difficulty;
                        break;
                    case "gameplay.playerSpeed":
                        if (config.Gameplay == null) config.Gameplay = new CoreModels.GameplayConfig();
                        config.Gameplay.PlayerSpeed = (float)Convert.ToDouble(value);
                        break;
                    case "gameplay.timeLimit":
                        if (config.Gameplay == null) config.Gameplay = new CoreModels.GameplayConfig();
                        config.Gameplay.TimeLimit = Convert.ToInt32(value);
                        break;
                }
            }
        }

        /// <summary>
        /// Creates a deep clone of a GenerationConfig
        /// </summary>
        private CoreModels.GenerationConfig CloneConfig(CoreModels.GenerationConfig original)
        {
            // Simple cloning - in production you might want to use a proper deep cloning library
            return new CoreModels.GenerationConfig
            {
                Width = original.Width,
                Height = original.Height,
                Seed = original.Seed,
                GenerationAlgorithm = original.GenerationAlgorithm,
                AlgorithmParameters = original.AlgorithmParameters != null 
                    ? new Dictionary<string, object>(original.AlgorithmParameters) 
                    : new Dictionary<string, object>(),
                TerrainTypes = original.TerrainTypes != null 
                    ? new List<string>(original.TerrainTypes) 
                    : new List<string>(),
                Entities = original.Entities?.Select(e => new CoreModels.EntityConfig
                {
                    Type = e.Type,
                    Count = e.Count,
                    MinDistance = e.MinDistance,
                    MaxDistanceFromPlayer = e.MaxDistanceFromPlayer,
                    Properties = e.Properties != null ? new Dictionary<string, object>(e.Properties) : new Dictionary<string, object>(),
                    PlacementStrategy = e.PlacementStrategy
                }).ToList() ?? new List<CoreModels.EntityConfig>(),
                VisualTheme = original.VisualTheme != null ? new CoreModels.VisualThemeConfig
                {
                    ThemeName = original.VisualTheme.ThemeName,
                    ColorPalette = original.VisualTheme.ColorPalette != null 
                        ? new Dictionary<string, string>(original.VisualTheme.ColorPalette) 
                        : new Dictionary<string, string>(),
                    TileSprites = original.VisualTheme.TileSprites != null 
                        ? new Dictionary<string, string>(original.VisualTheme.TileSprites) 
                        : new Dictionary<string, string>(),
                    EntitySprites = original.VisualTheme.EntitySprites != null 
                        ? new Dictionary<string, string>(original.VisualTheme.EntitySprites) 
                        : new Dictionary<string, string>(),
                    EffectSettings = original.VisualTheme.EffectSettings != null 
                        ? new Dictionary<string, object>(original.VisualTheme.EffectSettings) 
                        : new Dictionary<string, object>()
                } : new CoreModels.VisualThemeConfig(),
                Gameplay = original.Gameplay != null ? new CoreModels.GameplayConfig
                {
                    PlayerSpeed = original.Gameplay.PlayerSpeed,
                    PlayerHealth = original.Gameplay.PlayerHealth,
                    Difficulty = original.Gameplay.Difficulty,
                    TimeLimit = original.Gameplay.TimeLimit,
                    VictoryConditions = original.Gameplay.VictoryConditions != null 
                        ? new List<string>(original.Gameplay.VictoryConditions) 
                        : new List<string>(),
                    Mechanics = original.Gameplay.Mechanics != null 
                        ? new Dictionary<string, object>(original.Gameplay.Mechanics) 
                        : new Dictionary<string, object>()
                } : new CoreModels.GameplayConfig()
            };
        }

        /// <summary>
        /// Calculates the total number of levels in a batch request
        /// </summary>
        private int CalculateTotalBatchLevels(WebApiModels.BatchGenerationRequest request)
        {
            if (request.Variations == null || request.Variations.Count == 0)
            {
                return request.Count;
            }

            var totalCombinations = request.Variations.Aggregate(1, (total, variation) => 
                total * Math.Max(variation.Values?.Count ?? 1, 1));

            return totalCombinations * request.Count;
        }

        /// <summary>
        /// Generates a cache key for a configuration
        /// </summary>
        private string GenerateCacheKey(CoreModels.GenerationConfig config)
        {
            var keyData = $"{config.Width}x{config.Height}_{config.GenerationAlgorithm}_{config.Seed}";
            return $"level:{keyData.GetHashCode()}";
        }
    }
}
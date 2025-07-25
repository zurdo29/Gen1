using FluentValidation;
using Hangfire;
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
    /// Service for batch generation operations
    /// </summary>
    public class BatchGenerationService : IBatchGenerationService
    {
        private readonly ILogger<BatchGenerationService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IJobStatusService _jobStatusService;
        private readonly ILevelGenerationService _levelGenerationService;
        private readonly IGenerationManager _generationManager;
        private readonly IValidator<BatchGenerationRequest> _batchValidator;
        private readonly ApiConfiguration _apiConfig;
        private readonly GenerationConfiguration _genConfig;

        public BatchGenerationService(
            ILogger<BatchGenerationService> logger,
            IBackgroundJobClient backgroundJobClient,
            IJobStatusService jobStatusService,
            ILevelGenerationService levelGenerationService,
            IGenerationManager generationManager,
            IValidator<BatchGenerationRequest> batchValidator,
            IOptions<ApiConfiguration> apiConfig,
            IOptions<GenerationConfiguration> genConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            _jobStatusService = jobStatusService ?? throw new ArgumentNullException(nameof(jobStatusService));
            _levelGenerationService = levelGenerationService ?? throw new ArgumentNullException(nameof(levelGenerationService));
            _generationManager = generationManager ?? throw new ArgumentNullException(nameof(generationManager));
            _batchValidator = batchValidator ?? throw new ArgumentNullException(nameof(batchValidator));
            _apiConfig = apiConfig?.Value ?? throw new ArgumentNullException(nameof(apiConfig));
            _genConfig = genConfig?.Value ?? throw new ArgumentNullException(nameof(genConfig));
        }

        /// <summary>
        /// Starts a batch generation job
        /// </summary>
        public async Task<Result<string>> StartBatchGenerationAsync(BatchGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Starting batch generation for session {SessionId}", request.SessionId);

                // Validate the request
                var validationResult = ValidateBatchRequest(request);
                if (validationResult.IsFailure)
                {
                    return Result<string>.Failure(validationResult.Error);
                }

                var jobId = Guid.NewGuid().ToString();
                var totalLevels = CalculateTotalBatchLevels(request);

                // Create job status
                var createResult = await _jobStatusService.CreateJobStatusAsync(jobId, "batch", new Dictionary<string, object>
                {
                    ["sessionId"] = request.SessionId ?? string.Empty,
                    ["baseConfigSize"] = $"{request.BaseConfig.Width}x{request.BaseConfig.Height}",
                    ["algorithm"] = request.BaseConfig.GenerationAlgorithm,
                    ["totalLevels"] = totalLevels,
                    ["variationCount"] = request.Variations?.Count ?? 0,
                    ["batchCount"] = request.Count
                });

                if (createResult.IsFailure)
                {
                    return Result<string>.Failure($"Failed to create job status: {createResult.Error}");
                }

                // Enqueue background job
                _backgroundJobClient.Enqueue<BatchGenerationService>(x => x.ProcessBatchGenerationAsync(jobId, request));

                _logger.LogInformation("Batch generation job {JobId} started for {TotalLevels} levels", jobId, totalLevels);

                return Result<string>.Success(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting batch generation for session {SessionId}", request.SessionId);
                return Result<string>.Failure("Failed to start batch generation");
            }
        }

        /// <summary>
        /// Cancels a running batch generation job
        /// </summary>
        public async Task<Result> CancelBatchGenerationAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("Cancelling batch generation job {JobId}", jobId);

                var cancelResult = await _jobStatusService.CancelJobAsync(jobId, "Cancelled by user request");
                
                if (cancelResult.IsFailure)
                {
                    return Result.Failure(cancelResult.Error);
                }

                // Note: In a real implementation, you'd also need to signal the background job to stop
                // This would require additional coordination mechanisms like CancellationToken

                _logger.LogInformation("Batch generation job {JobId} cancelled successfully", jobId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling batch generation job {JobId}", jobId);
                return Result.Failure("Failed to cancel batch generation");
            }
        }

        /// <summary>
        /// Validates a batch generation request
        /// </summary>
        public Result ValidateBatchRequest(BatchGenerationRequest request)
        {
            try
            {
                var validationResult = _batchValidator.Validate(request);
                
                if (validationResult.IsValid)
                {
                    return Result.Success();
                }

                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                var errorMessage = $"Batch request validation failed: {string.Join(", ", errors)}";
                
                _logger.LogWarning("Batch request validation failed: {Errors}", string.Join("; ", errors));
                
                return Result.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch request validation");
                return Result.Failure("Batch request validation failed due to an internal error");
            }
        }

        /// <summary>
        /// Calculates the total number of levels that will be generated in a batch request
        /// </summary>
        public int CalculateTotalBatchLevels(BatchGenerationRequest request)
        {
            if (request.Variations == null || request.Variations.Count == 0)
            {
                return request.Count;
            }

            // Calculate combinations of all variations
            var totalCombinations = request.Variations.Aggregate(1, (total, variation) => 
                total * Math.Max(variation.Values?.Count ?? 1, 1));

            return totalCombinations * request.Count;
        }

        /// <summary>
        /// Processes a batch generation job (called by Hangfire)
        /// </summary>
        [Queue("batch-generation")]
        public async Task ProcessBatchGenerationAsync(string jobId, BatchGenerationRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new List<object>();
            
            try
            {
                _logger.LogInformation("Starting batch generation processing for job {JobId}", jobId);

                // Update job status to running
                await _jobStatusService.UpdateJobStatusAsync(jobId, "running", 5, "Starting batch generation");

                // Validate base configuration
                var validationResult = _levelGenerationService.ValidateConfiguration(request.BaseConfig);
                if (validationResult.IsFailure)
                {
                    await _jobStatusService.FailJobAsync(jobId, $"Invalid base configuration: {validationResult.Error}");
                    return;
                }

                await _jobStatusService.UpdateJobStatusAsync(jobId, "running", 10, "Configuration validated");

                // Generate all configuration combinations
                var configurations = GenerateConfigurationCombinations(request);
                var totalConfigurations = configurations.Count;
                var completedCount = 0;

                _logger.LogInformation("Generated {TotalConfigurations} configuration combinations for job {JobId}", 
                    totalConfigurations, jobId);

                // Generate levels for each configuration
                foreach (var (config, variationIndex, batchIndex) in configurations)
                {
                    // Check if job was cancelled
                    var statusResult = await _jobStatusService.GetJobStatusAsync(jobId);
                    if (statusResult.IsSuccess && statusResult.Value.Status == "cancelled")
                    {
                        _logger.LogInformation("Batch generation job {JobId} was cancelled", jobId);
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
                        await _jobStatusService.UpdateJobStatusAsync(jobId, "running", Math.Min(progress, 95), 
                            $"Generated {completedCount}/{totalConfigurations} levels");

                        _logger.LogDebug("Generated level {LevelId} for job {JobId} ({CompletedCount}/{TotalCount})", 
                            result.id, jobId, completedCount, totalConfigurations);
                    }
                    catch (Exception levelEx)
                    {
                        _logger.LogError(levelEx, "Failed to generate level for job {JobId} (variation {VariationIndex}, batch {BatchIndex})", 
                            jobId, variationIndex, batchIndex);

                        // Continue with next level instead of failing entire batch
                        completedCount++;
                    }
                }

                // Complete job
                await _jobStatusService.CompleteJobAsync(jobId, results);
                
                stopwatch.Stop();
                
                _logger.LogInformation("Batch generation job {JobId} completed successfully: {SuccessfulLevels}/{TotalLevels} levels in {Duration}ms",
                    jobId, results.Count, totalConfigurations, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "Batch generation job {JobId} failed after {Duration}ms", 
                    jobId, stopwatch.ElapsedMilliseconds);
                
                await _jobStatusService.FailJobAsync(jobId, ex.Message, results.Count > 0 ? results : null);
            }
        }

        /// <summary>
        /// Generates all configuration combinations for batch generation
        /// </summary>
        private List<(GenerationConfig config, int variationIndex, int batchIndex)> GenerateConfigurationCombinations(BatchGenerationRequest request)
        {
            var configurations = new List<(GenerationConfig, int, int)>();

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
        private List<List<object>> GenerateVariationCombinations(List<ConfigVariation> variations)
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
        private void ApplyVariationToConfig(GenerationConfig config, List<ConfigVariation> variations, List<object> values)
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
                        if (config.VisualTheme == null) config.VisualTheme = new VisualThemeConfig();
                        config.VisualTheme.ThemeName = value.ToString() ?? config.VisualTheme.ThemeName;
                        break;
                    case "gameplay.difficulty":
                        if (config.Gameplay == null) config.Gameplay = new GameplayConfig();
                        config.Gameplay.Difficulty = value.ToString() ?? config.Gameplay.Difficulty;
                        break;
                    case "gameplay.playerSpeed":
                        if (config.Gameplay == null) config.Gameplay = new GameplayConfig();
                        config.Gameplay.PlayerSpeed = Convert.ToDouble(value);
                        break;
                    case "gameplay.timeLimit":
                        if (config.Gameplay == null) config.Gameplay = new GameplayConfig();
                        config.Gameplay.TimeLimit = Convert.ToInt32(value);
                        break;
                }
            }
        }

        /// <summary>
        /// Creates a deep clone of a GenerationConfig
        /// </summary>
        private GenerationConfig CloneConfig(GenerationConfig original)
        {
            return new GenerationConfig
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
                Entities = original.Entities?.Select(e => new EntityConfig
                {
                    Type = e.Type,
                    Count = e.Count,
                    MinDistance = e.MinDistance,
                    MaxDistanceFromPlayer = e.MaxDistanceFromPlayer,
                    Properties = e.Properties != null ? new Dictionary<string, object>(e.Properties) : new Dictionary<string, object>(),
                    PlacementStrategy = e.PlacementStrategy
                }).ToList() ?? new List<EntityConfig>(),
                VisualTheme = original.VisualTheme != null ? new VisualThemeConfig
                {
                    ThemeName = original.VisualTheme.ThemeName,
                    ColorPalette = original.VisualTheme.ColorPalette != null 
                        ? new Dictionary<string, string>(original.VisualTheme.ColorPalette) 
                        : new Dictionary<string, string>(),
                    TileSprites = original.VisualTheme.TileSprites != null 
                        ? new Dictionary<string, string>(original.VisualTheme.TileSprites) 
                        : new Dictionary<string, string>()
                } : null,
                Gameplay = original.Gameplay != null ? new GameplayConfig
                {
                    Difficulty = original.Gameplay.Difficulty,
                    PlayerSpeed = original.Gameplay.PlayerSpeed,
                    TimeLimit = original.Gameplay.TimeLimit,
                    Objectives = original.Gameplay.Objectives != null 
                        ? new List<string>(original.Gameplay.Objectives) 
                        : new List<string>()
                } : null
            };
        }
    }
}
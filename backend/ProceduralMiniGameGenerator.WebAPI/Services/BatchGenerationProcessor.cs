using System.Diagnostics;
using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;
using CoreModels = ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Constants;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Handles batch generation processing logic
    /// </summary>
    public class BatchGenerationProcessor
    {
        private readonly ILoggerService _loggerService;
        private readonly IJobManagementService _jobManagementService;
        private readonly IConfigurationCloningService _configurationCloningService;
        private readonly IGenerationManager _generationManager;

        public BatchGenerationProcessor(
            ILoggerService loggerService,
            IJobManagementService jobManagementService,
            IConfigurationCloningService configurationCloningService,
            IGenerationManager generationManager)
        {
            _loggerService = loggerService;
            _jobManagementService = jobManagementService;
            _configurationCloningService = configurationCloningService;
            _generationManager = generationManager;
        }

        public async Task ProcessBatchGeneration(string jobId, WebApiModels.BatchGenerationRequest request)
        {
            var context = new WebApiModels.BatchProcessingContext(jobId, request);
            
            try
            {
                await ExecuteBatchWorkflow(context);
            }
            catch (Exception ex)
            {
                await HandleBatchJobFailure(context, ex);
            }
        }

        private async Task ExecuteBatchWorkflow(WebApiModels.BatchProcessingContext context)
        {
            await InitializeBatchJob(context);
            
            if (!await ValidateBaseConfiguration(context))
                return;

            var configurations = GenerateConfigurations(context);
            await ProcessConfigurations(context, configurations);
            await CompleteBatchJob(context);
        }

        private async Task InitializeBatchJob(WebApiModels.BatchProcessingContext context)
        {
            await _jobManagementService.UpdateJobProgress(context.JobId, BatchProcessingConstants.Progress.Started);
            
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                "Starting batch level generation",
                new { 
                    JobId = context.JobId,
                    SessionId = context.Request.SessionId,
                    TotalLevels = context.TotalLevels,
                    VariationCount = context.Request.Variations?.Count ?? 0,
                    BatchCount = context.Request.Count
                });
        }

        private async Task<bool> ValidateBaseConfiguration(WebApiModels.BatchProcessingContext context)
        {
            await _jobManagementService.UpdateJobProgress(context.JobId, BatchProcessingConstants.Progress.Validated);
            
            // Validation logic would go here
            // For now, assume validation passes
            return true;
        }

        private List<(CoreModels.GenerationConfig config, int variationIndex, int batchIndex)> GenerateConfigurations(WebApiModels.BatchProcessingContext context)
        {
            var configurations = _configurationCloningService.GenerateConfigurationCombinations(context.Request);
            LogConfigurationGeneration(context.JobId, configurations.Count);
            return configurations;
        }

        private async Task ProcessConfigurations(WebApiModels.BatchProcessingContext context, List<(CoreModels.GenerationConfig config, int variationIndex, int batchIndex)> configurations)
        {
            var totalConfigurations = configurations.Count;
            var completedCount = 0;

            foreach (var (config, variationIndex, batchIndex) in configurations)
            {
                if (await IsJobCancelled(context.JobId, completedCount, totalConfigurations))
                    return;

                await ProcessSingleConfiguration(context.JobId, config, variationIndex, batchIndex, context.Results);
                completedCount++;
                await UpdateBatchProgress(context.JobId, completedCount, totalConfigurations);
            }
        }

        private async Task CompleteBatchJob(WebApiModels.BatchProcessingContext context)
        {
            await _jobManagementService.CompleteJob(context.JobId, context.Results);
            context.Stopwatch.Stop();
            
            await LogBatchCompletion(context.JobId, context.Results, context.Stopwatch.ElapsedMilliseconds);
        }

        private async Task HandleBatchJobFailure(WebApiModels.BatchProcessingContext context, Exception ex)
        {
            context.Stopwatch.Stop();
            await _jobManagementService.FailJob(context.JobId, ex.Message);
            
            await _loggerService.LogErrorAsync(ex,
                "Batch level generation failed",
                new { 
                    JobId = context.JobId,
                    DurationMs = context.Stopwatch.ElapsedMilliseconds,
                    SessionId = context.Request.SessionId,
                    PartialResults = context.Results.Count
                });
        }



        private async Task<bool> IsJobCancelled(string jobId, int completedCount, int totalCount)
        {
            var currentStatus = _jobManagementService.GetJobStatus(jobId);
            if (currentStatus.Status == WebApiModels.JobStatusType.Cancelled)
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Batch generation cancelled",
                    new { JobId = jobId, CompletedCount = completedCount, TotalCount = totalCount });
                return true;
            }
            return false;
        }

        private async Task ProcessSingleConfiguration(string jobId, CoreModels.GenerationConfig config, int variationIndex, int batchIndex, List<object> results)
        {
            try
            {
                var levelStopwatch = Stopwatch.StartNew();
                var level = await Task.Run(() => _generationManager.GenerateLevel(config));
                levelStopwatch.Stop();

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
                await LogSuccessfulGeneration(jobId, result, levelStopwatch.ElapsedMilliseconds);
            }
            catch (Exception levelEx)
            {
                await _loggerService.LogErrorAsync(levelEx,
                    "Failed to generate level in batch",
                    new { JobId = jobId, VariationIndex = variationIndex, BatchIndex = batchIndex, Config = config });
            }
        }

        private async Task UpdateBatchProgress(string jobId, int completedCount, int totalConfigurations)
        {
            var progress = BatchProcessingConstants.Progress.GenerationStart + 
                          (int)((completedCount / (double)totalConfigurations) * 
                          (BatchProcessingConstants.Progress.GenerationEnd - BatchProcessingConstants.Progress.GenerationStart));
            
            await _jobManagementService.UpdateJobProgress(jobId, Math.Min(progress, BatchProcessingConstants.Progress.GenerationEnd));
        }



        private async Task LogConfigurationGeneration(string jobId, int totalConfigurations)
        {
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                "Generated configuration combinations",
                new { JobId = jobId, TotalConfigurations = totalConfigurations });
        }

        private async Task LogSuccessfulGeneration(string jobId, object result, long generationTimeMs)
        {
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                "Generated level in batch",
                new { 
                    JobId = jobId,
                    LevelId = ((dynamic)result).id,
                    VariationIndex = ((dynamic)result).variationIndex,
                    BatchIndex = ((dynamic)result).batchIndex,
                    GenerationTimeMs = generationTimeMs
                });
        }

        private async Task LogBatchCompletion(string jobId, List<object> results, long durationMs)
        {
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                "Batch level generation completed successfully",
                new { 
                    JobId = jobId,
                    DurationMs = durationMs,
                    TotalLevels = results.Count
                });
        }


    }
}
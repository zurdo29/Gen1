using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProceduralMiniGameGenerator.WebAPI.Configuration;
using ProceduralMiniGameGenerator.WebAPI.Exceptions;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for managing job status and tracking using memory cache
    /// </summary>
    public class JobStatusService : IJobStatusService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<JobStatusService> _logger;
        private readonly ApiConfiguration _config;
        private const string JobKeyPrefix = "job:";

        public JobStatusService(
            IMemoryCache cache,
            ILogger<JobStatusService> logger,
            IOptions<ApiConfiguration> config)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Gets the status of a background job
        /// </summary>
        public async Task<Result<JobStatus>> GetJobStatusAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    return Result<JobStatus>.Failure("Job ID cannot be null or empty");
                }

                var cacheKey = GetJobCacheKey(jobId);
                
                if (_cache.TryGetValue(cacheKey, out JobStatus? status) && status != null)
                {
                    _logger.LogDebug("Retrieved job status for {JobId}: {Status}", jobId, status.Status);
                    return await Task.FromResult(Result<JobStatus>.Success(status));
                }
                
                _logger.LogDebug("Job {JobId} not found or expired", jobId);
                return Result<JobStatus>.Failure("Job not found or has expired");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job status for {JobId}", jobId);
                return Result<JobStatus>.Failure("Failed to retrieve job status");
            }
        }

        /// <summary>
        /// Creates a new job status entry
        /// </summary>
        public async Task<Result> CreateJobStatusAsync(string jobId, string jobType, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    return Result.Failure("Job ID cannot be null or empty");
                }

                if (string.IsNullOrEmpty(jobType))
                {
                    return Result.Failure("Job type cannot be null or empty");
                }

                // Parse job type string to enum
                if (!Enum.TryParse<JobType>(jobType, true, out var parsedJobType))
                {
                    parsedJobType = JobType.Generation; // Default fallback
                }

                var jobStatus = new JobStatus
                {
                    JobId = jobId,
                    Status = JobStatusType.Pending,
                    Progress = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                // Add job type to metadata for backward compatibility
                jobStatus.Metadata["jobType"] = jobType;

                var cacheKey = GetJobCacheKey(jobId);
                var expiration = jobType == "batch" ? _config.BatchJobStatusCacheExpiration : _config.JobStatusCacheExpiration;
                
                _cache.Set(cacheKey, jobStatus, expiration);
                
                _logger.LogInformation("Created job status for {JobId} of type {JobType}", jobId, jobType);
                
                return await Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job status for {JobId}", jobId);
                return Result.Failure("Failed to create job status");
            }
        }

        /// <summary>
        /// Updates job status
        /// </summary>
        public async Task<Result> UpdateJobStatusAsync(string jobId, string status, int progress, string? message = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    return Result.Failure("Job ID cannot be null or empty");
                }

                if (string.IsNullOrEmpty(status))
                {
                    return Result.Failure("Status cannot be null or empty");
                }

                if (progress < 0 || progress > 100)
                {
                    return Result.Failure("Progress must be between 0 and 100");
                }

                // Parse status string to enum
                if (!Enum.TryParse<JobStatusType>(status, true, out var parsedStatus))
                {
                    return Result.Failure($"Invalid status: {status}");
                }

                var getResult = await GetJobStatusAsync(jobId);
                if (getResult.IsFailure)
                {
                    return Result.Failure(getResult.Error);
                }

                var jobStatus = getResult.Value;
                jobStatus.Status = parsedStatus;
                jobStatus.Progress = progress;
                jobStatus.UpdatedAt = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(message))
                {
                    jobStatus.Message = message;
                }

                var cacheKey = GetJobCacheKey(jobId);
                var jobType = jobStatus.Metadata.TryGetValue("jobType", out var type) ? type.ToString() : "generation";
                var expiration = jobType == "batch" ? _config.BatchJobStatusCacheExpiration : _config.JobStatusCacheExpiration;
                
                _cache.Set(cacheKey, jobStatus, expiration);
                
                _logger.LogDebug("Updated job {JobId} status to {Status} with {Progress}% progress", 
                    jobId, status, progress);
                
                return await Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job status for {JobId}", jobId);
                return Result.Failure("Failed to update job status");
            }
        }

        /// <summary>
        /// Completes a job with success
        /// </summary>
        public async Task<Result> CompleteJobAsync(string jobId, object? result = null)
        {
            try
            {
                var getResult = await GetJobStatusAsync(jobId);
                if (getResult.IsFailure)
                {
                    return Result.Failure(getResult.Error);
                }

                var jobStatus = getResult.Value;
                jobStatus.Status = JobStatusType.Completed;
                jobStatus.Progress = 100;
                jobStatus.CompletedAt = DateTime.UtcNow;
                jobStatus.UpdatedAt = DateTime.UtcNow;
                jobStatus.Result = result;

                var cacheKey = GetJobCacheKey(jobId);
                var jobType = jobStatus.Metadata.TryGetValue("jobType", out var type) ? type.ToString() : "generation";
                var expiration = jobType == "batch" ? _config.BatchJobStatusCacheExpiration : _config.JobStatusCacheExpiration;
                
                _cache.Set(cacheKey, jobStatus, expiration);
                
                _logger.LogInformation("Completed job {JobId} successfully", jobId);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing job {JobId}", jobId);
                return Result.Failure("Failed to complete job");
            }
        }

        /// <summary>
        /// Fails a job with error information
        /// </summary>
        public async Task<Result> FailJobAsync(string jobId, string errorMessage, object? partialResult = null)
        {
            try
            {
                if (string.IsNullOrEmpty(errorMessage))
                {
                    return Result.Failure("Error message cannot be null or empty");
                }

                var getResult = await GetJobStatusAsync(jobId);
                if (getResult.IsFailure)
                {
                    return Result.Failure(getResult.Error);
                }

                var jobStatus = getResult.Value;
                jobStatus.Status = JobStatusType.Failed;
                jobStatus.ErrorMessage = errorMessage;
                jobStatus.CompletedAt = DateTime.UtcNow;
                jobStatus.UpdatedAt = DateTime.UtcNow;
                
                if (partialResult != null)
                {
                    jobStatus.Result = partialResult;
                }

                var cacheKey = GetJobCacheKey(jobId);
                var jobType = jobStatus.Metadata.TryGetValue("jobType", out var type) ? type.ToString() : "generation";
                var expiration = jobType == "batch" ? _config.BatchJobStatusCacheExpiration : _config.JobStatusCacheExpiration;
                
                _cache.Set(cacheKey, jobStatus, expiration);
                
                _logger.LogWarning("Failed job {JobId} with error: {ErrorMessage}", jobId, errorMessage);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error failing job {JobId}", jobId);
                return Result.Failure("Failed to update job failure status");
            }
        }

        /// <summary>
        /// Cancels a job
        /// </summary>
        public async Task<Result> CancelJobAsync(string jobId, string reason = "Cancelled by user request")
        {
            try
            {
                var getResult = await GetJobStatusAsync(jobId);
                if (getResult.IsFailure)
                {
                    return Result.Failure(getResult.Error);
                }

                var jobStatus = getResult.Value;
                
                // Check if job can be cancelled
                if (jobStatus.Status == JobStatusType.Completed || jobStatus.Status == JobStatusType.Failed || jobStatus.Status == JobStatusType.Cancelled)
                {
                    return Result.Failure($"Job cannot be cancelled in status '{jobStatus.Status}'");
                }

                jobStatus.Status = JobStatusType.Cancelled;
                jobStatus.ErrorMessage = reason;
                jobStatus.CompletedAt = DateTime.UtcNow;
                jobStatus.UpdatedAt = DateTime.UtcNow;

                var cacheKey = GetJobCacheKey(jobId);
                var jobType = jobStatus.Metadata.TryGetValue("jobType", out var type) ? type.ToString() : "generation";
                var expiration = jobType == "batch" ? _config.BatchJobStatusCacheExpiration : _config.JobStatusCacheExpiration;
                
                _cache.Set(cacheKey, jobStatus, expiration);
                
                _logger.LogInformation("Cancelled job {JobId}: {Reason}", jobId, reason);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
                return Result.Failure("Failed to cancel job");
            }
        }

        /// <summary>
        /// Cleans up expired job statuses
        /// </summary>
        public async Task<int> CleanupExpiredJobsAsync()
        {
            try
            {
                // Note: IMemoryCache doesn't provide a way to enumerate all keys
                // In a real implementation, you'd use a persistent store with cleanup capabilities
                // For now, we rely on the cache's built-in expiration
                
                _logger.LogDebug("Job cleanup requested - relying on cache expiration");
                
                return await Task.FromResult(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during job cleanup");
                return 0;
            }
        }

        /// <summary>
        /// Generates cache key for job status
        /// </summary>
        private static string GetJobCacheKey(string jobId)
        {
            return $"{JobKeyPrefix}{jobId}";
        }
    }
}
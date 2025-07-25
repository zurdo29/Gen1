using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for managing job status and tracking
    /// </summary>
    public interface IJobStatusService
    {
        /// <summary>
        /// Gets the status of a background job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Result containing job status or error</returns>
        Task<Result<JobStatus>> GetJobStatusAsync(string jobId);

        /// <summary>
        /// Creates a new job status entry
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="jobType">Type of job</param>
        /// <param name="metadata">Additional job metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result> CreateJobStatusAsync(string jobId, JobType jobType, JobMetadata? metadata = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates job status
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="status">New status</param>
        /// <param name="progress">Progress percentage (0-100)</param>
        /// <param name="message">Optional status message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result> UpdateJobStatusAsync(string jobId, JobStatusType status, int progress, string? message = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes a job with success
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="result">Job result data</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result> CompleteJobAsync(string jobId, object? result = null);

        /// <summary>
        /// Fails a job with error information
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="errorMessage">Error message</param>
        /// <param name="partialResult">Partial result if any</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result> FailJobAsync(string jobId, string errorMessage, object? partialResult = null);

        /// <summary>
        /// Cancels a job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="reason">Cancellation reason</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result> CancelJobAsync(string jobId, string reason = "Cancelled by user request");

        /// <summary>
        /// Cleans up expired job statuses
        /// </summary>
        /// <returns>Number of cleaned up jobs</returns>
        Task<int> CleanupExpiredJobsAsync();
    }
}
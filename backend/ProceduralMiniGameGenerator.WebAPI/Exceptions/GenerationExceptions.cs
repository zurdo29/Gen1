using ProceduralMiniGameGenerator.WebAPI.Constants;

namespace ProceduralMiniGameGenerator.WebAPI.Exceptions
{
    /// <summary>
    /// Base exception for generation-related errors
    /// </summary>
    public abstract class GenerationException : Exception
    {
        public string ErrorCode { get; }

        protected GenerationException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected GenerationException(string errorCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when generation configuration is invalid
    /// </summary>
    public class InvalidConfigurationException : GenerationException
    {
        public IReadOnlyList<string> ValidationErrors { get; }

        public InvalidConfigurationException(IEnumerable<string> validationErrors) 
            : base(Constants.ErrorCodes.InvalidConfiguration, $"Configuration validation failed: {string.Join(", ", validationErrors)}")
        {
            ValidationErrors = validationErrors.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Exception thrown when generation process fails
    /// </summary>
    public class GenerationFailedException : GenerationException
    {
        public string? SessionId { get; }
        public string? Algorithm { get; }

        public GenerationFailedException(string message, string? sessionId = null, string? algorithm = null) 
            : base(Constants.ErrorCodes.GenerationFailed, message)
        {
            SessionId = sessionId;
            Algorithm = algorithm;
        }

        public GenerationFailedException(string message, Exception innerException, string? sessionId = null, string? algorithm = null) 
            : base(Constants.ErrorCodes.GenerationFailed, message, innerException)
        {
            SessionId = sessionId;
            Algorithm = algorithm;
        }
    }

    /// <summary>
    /// Exception thrown when batch generation limits are exceeded
    /// </summary>
    public class BatchLimitExceededException : GenerationException
    {
        public int RequestedCount { get; }
        public int MaxAllowed { get; }

        public BatchLimitExceededException(int requestedCount, int maxAllowed) 
            : base(Constants.ErrorCodes.BatchLimitExceeded, $"Requested batch size {requestedCount} exceeds maximum allowed {maxAllowed}")
        {
            RequestedCount = requestedCount;
            MaxAllowed = maxAllowed;
        }
    }

    /// <summary>
    /// Exception thrown when a job is not found
    /// </summary>
    public class JobNotFoundException : GenerationException
    {
        public string JobId { get; }

        public JobNotFoundException(string jobId) 
            : base(Constants.ErrorCodes.JobNotFound, $"Job with ID '{jobId}' was not found or has expired")
        {
            JobId = jobId;
        }
    }

    /// <summary>
    /// Exception thrown when trying to cancel a job that cannot be cancelled
    /// </summary>
    public class JobCancellationException : GenerationException
    {
        public string JobId { get; }
        public string CurrentStatus { get; }

        public JobCancellationException(string jobId, string currentStatus) 
            : base(Constants.ErrorCodes.JobCancellationFailed, $"Job '{jobId}' cannot be cancelled in status '{currentStatus}'")
        {
            JobId = jobId;
            CurrentStatus = currentStatus;
        }
    }
}
namespace ProceduralMiniGameGenerator.WebAPI.Constants
{
    /// <summary>
    /// Centralized error codes for consistent error handling
    /// </summary>
    public static class ErrorCodes
    {
        // Configuration errors
        public const string InvalidConfiguration = "INVALID_CONFIGURATION";
        public const string ConfigurationValidationFailed = "CONFIGURATION_VALIDATION_FAILED";

        // Generation errors
        public const string GenerationFailed = "GENERATION_FAILED";
        public const string GenerationTimeout = "GENERATION_TIMEOUT";
        public const string AlgorithmNotSupported = "ALGORITHM_NOT_SUPPORTED";

        // Batch processing errors
        public const string BatchLimitExceeded = "BATCH_LIMIT_EXCEEDED";
        public const string BatchProcessingFailed = "BATCH_PROCESSING_FAILED";

        // Job management errors
        public const string JobNotFound = "JOB_NOT_FOUND";
        public const string JobCancellationFailed = "JOB_CANCELLATION_FAILED";
        public const string JobAlreadyCompleted = "JOB_ALREADY_COMPLETED";

        // Resource errors
        public const string InsufficientMemory = "INSUFFICIENT_MEMORY";
        public const string ResourceExhausted = "RESOURCE_EXHAUSTED";

        // Validation errors
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string InvalidParameter = "INVALID_PARAMETER";
        public const string ParameterOutOfRange = "PARAMETER_OUT_OF_RANGE";
    }
}
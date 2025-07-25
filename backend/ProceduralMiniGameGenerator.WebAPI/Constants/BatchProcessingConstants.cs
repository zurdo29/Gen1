namespace ProceduralMiniGameGenerator.WebAPI.Constants
{
    /// <summary>
    /// Constants for batch processing operations
    /// </summary>
    public static class BatchProcessingConstants
    {
        /// <summary>
        /// Progress percentages for different stages
        /// </summary>
        public static class Progress
        {
            public const int Started = 5;
            public const int Validated = 10;
            public const int GenerationStart = 10;
            public const int GenerationEnd = 95;
            public const int Completed = 100;
        }

        /// <summary>
        /// Job status values
        /// </summary>
        public static class JobStatus
        {
            public const string Pending = "pending";
            public const string Running = "running";
            public const string Completed = "completed";
            public const string Failed = "failed";
            public const string Cancelled = "cancelled";
        }

        /// <summary>
        /// Job types
        /// </summary>
        public static class JobTypes
        {
            public const string Batch = "batch";
            public const string Single = "single";
        }

        /// <summary>
        /// Hangfire queue names
        /// </summary>
        public static class Queues
        {
            public const string BatchGeneration = "batch-generation";
            public const string SingleGeneration = "single-generation";
        }
    }
}
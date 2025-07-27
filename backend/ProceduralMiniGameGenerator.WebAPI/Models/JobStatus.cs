namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Represents the status of a background job
    /// </summary>
    public class JobStatus
    {
        public string JobId { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public JobStatusType Status { get; set; }
        public int Progress { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public object? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Strongly-typed job metadata
    /// </summary>
    public class JobMetadata
    {
        public string? SessionId { get; set; }
        public int? TotalItems { get; set; }
        public int? ProcessedItems { get; set; }
        public Dictionary<string, string>? AdditionalData { get; set; }

        /// <summary>
        /// Converts JobMetadata to Dictionary for compatibility
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>();
            
            if (SessionId != null)
                result["SessionId"] = SessionId;
            if (TotalItems.HasValue)
                result["TotalItems"] = TotalItems.Value;
            if (ProcessedItems.HasValue)
                result["ProcessedItems"] = ProcessedItems.Value;
            if (AdditionalData != null)
            {
                foreach (var kvp in AdditionalData)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }
    }

    /// <summary>
    /// Job type enumeration
    /// </summary>
    public enum JobType
    {
        Generation,
        BatchGeneration,
        Preview,
        Export
    }

    /// <summary>
    /// Job status enumeration
    /// </summary>
    public enum JobStatusType
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled,

        NotFound
    }
}
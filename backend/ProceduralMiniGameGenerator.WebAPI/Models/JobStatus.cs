namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Status of a background job
    /// </summary>
    public class JobStatus
    {
        /// <summary>
        /// Unique job identifier
        /// </summary>
        public string JobId { get; set; } = string.Empty;
        
        /// <summary>
        /// Current job status
        /// </summary>
        public string Status { get; set; } = "pending"; // "pending", "running", "completed", "failed"
        
        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Progress { get; set; } = 0;
        
        /// <summary>
        /// Error message if job failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Job result data when completed
        /// </summary>
        public object? Result { get; set; }
        
        /// <summary>
        /// Job creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Job completion timestamp
        /// </summary>
        public DateTime? CompletedAt { get; set; }
        
        /// <summary>
        /// Additional metadata about the job
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
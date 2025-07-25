namespace ProceduralMiniGameGenerator.WebAPI.Configuration
{
using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Configuration settings for the API
    /// </summary>
    public class ApiConfiguration
    {
        public const string SectionName = "Api";

        /// <summary>
        /// Allowed CORS origins
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one CORS origin must be specified")]
        public string[] CorsOrigins { get; set; } = new[] { "http://localhost:3000", "http://localhost:5173" };

        /// <summary>
        /// Maximum number of levels in a batch generation request
        /// </summary>
        [Range(1, 10000, ErrorMessage = "MaxBatchSize must be between 1 and 10000")]
        public int MaxBatchSize { get; set; } = 1000;

        /// <summary>
        /// Maximum number of levels per variation in batch generation
        /// </summary>
        public int MaxBatchCountPerVariation { get; set; } = 50;

        /// <summary>
        /// Cache expiration time for generated levels
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Sliding expiration time for cache entries
        /// </summary>
        public TimeSpan CacheSlidingExpiration { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Maximum cache size in MB
        /// </summary>
        public int MaxCacheSizeMB { get; set; } = 100;

        /// <summary>
        /// Job status cache expiration time
        /// </summary>
        public TimeSpan JobStatusCacheExpiration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Batch job status cache expiration time
        /// </summary>
        public TimeSpan BatchJobStatusCacheExpiration { get; set; } = TimeSpan.FromHours(2);

        /// <summary>
        /// Threshold for using background processing (total tiles)
        /// </summary>
        public int BackgroundProcessingTileThreshold { get; set; } = 10000;

        /// <summary>
        /// Threshold for using background processing (total entities)
        /// </summary>
        public int BackgroundProcessingEntityThreshold { get; set; } = 1000;

        /// <summary>
        /// Threshold for using background processing (algorithm parameters)
        /// </summary>
        public int BackgroundProcessingParameterThreshold { get; set; } = 10;

        /// <summary>
        /// Default debounce time for real-time preview in milliseconds
        /// </summary>
        public int DefaultPreviewDebounceMs { get; set; } = 500;

        /// <summary>
        /// Maximum debounce time for real-time preview in milliseconds
        /// </summary>
        public int MaxPreviewDebounceMs { get; set; } = 5000;
    }

    /// <summary>
    /// Configuration for generation limits and validation
    /// </summary>
    public class GenerationConfiguration
    {
        public const string SectionName = "Generation";

        /// <summary>
        /// Minimum allowed level width
        /// </summary>
        public int MinLevelWidth { get; set; } = 5;

        /// <summary>
        /// Maximum allowed level width
        /// </summary>
        public int MaxLevelWidth { get; set; } = 200;

        /// <summary>
        /// Minimum allowed level height
        /// </summary>
        public int MinLevelHeight { get; set; } = 5;

        /// <summary>
        /// Maximum allowed level height
        /// </summary>
        public int MaxLevelHeight { get; set; } = 200;

        /// <summary>
        /// Maximum number of entities per level
        /// </summary>
        public int MaxEntitiesPerLevel { get; set; } = 10000;

        /// <summary>
        /// Maximum number of variations per batch request
        /// </summary>
        public int MaxVariationsPerBatch { get; set; } = 20;

        /// <summary>
        /// Maximum number of values per variation parameter
        /// </summary>
        public int MaxValuesPerVariation { get; set; } = 20;

        /// <summary>
        /// Supported generation algorithms
        /// </summary>
        public string[] SupportedAlgorithms { get; set; } = new[] { "perlin", "cellular", "maze", "rooms" };

        /// <summary>
        /// Supported visual themes
        /// </summary>
        public string[] SupportedThemes { get; set; } = new[] { "forest", "desert", "cave", "space", "underwater" };

        /// <summary>
        /// Supported difficulty levels
        /// </summary>
        public string[] SupportedDifficulties { get; set; } = new[] { "easy", "medium", "hard", "expert" };

        /// <summary>
        /// Minimum player speed
        /// </summary>
        public double MinPlayerSpeed { get; set; } = 0.1;

        /// <summary>
        /// Maximum player speed
        /// </summary>
        public double MaxPlayerSpeed { get; set; } = 20.0;

        /// <summary>
        /// Maximum time limit in seconds
        /// </summary>
        public int MaxTimeLimit { get; set; } = 3600; // 1 hour
    }
}
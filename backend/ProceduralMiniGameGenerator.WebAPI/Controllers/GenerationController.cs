using Microsoft.AspNetCore.Mvc;
using CoreModels = ProceduralMiniGameGenerator.Models;
using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Controllers
{
    /// <summary>
    /// Controller for level generation operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GenerationController : ControllerBase
    {
        private readonly IGenerationService _generationService;
        private readonly ILoggerService _loggerService;
        private readonly IRealTimeGenerationService _realTimeGenerationService;

        public GenerationController(
            IGenerationService generationService,
            ILoggerService loggerService,
            IRealTimeGenerationService realTimeGenerationService)
        {
            _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
            _realTimeGenerationService = realTimeGenerationService ?? throw new ArgumentNullException(nameof(realTimeGenerationService));
        }

        /// <summary>
        /// Generates a level based on the provided configuration
        /// </summary>
        /// <param name="request">Generation request with configuration</param>
        /// <returns>Generated level or job ID for background processing</returns>
        /// <response code="200">Level generated successfully</response>
        /// <response code="202">Generation started in background, returns job ID</response>
        /// <response code="400">Invalid configuration</response>
        /// <response code="500">Generation failed</response>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(CoreModels.Level), 200)]
        [ProducesResponseType(typeof(BackgroundJobResponse), 202)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateLevel([FromBody] WebApiModels.WebGenerationRequest request)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generation request received",
                    new { 
                        SessionId = request.SessionId,
                        ConfigSize = $"{request.Config.Width}x{request.Config.Height}",
                        Algorithm = request.Config.GenerationAlgorithm,
                        UseBackgroundProcessing = request.UseBackgroundProcessing
                    });

                // Validate the request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate configuration
                var validationResult = _generationService.ValidateConfiguration(request.Config);
                if (!validationResult.IsValid)
                {
                    var problemDetails = new ValidationProblemDetails();
                    foreach (var error in validationResult.Errors)
                    {
                        problemDetails.Errors.Add("Config", new[] { error });
                    }
                    return BadRequest(problemDetails);
                }

                // Determine if we should use background processing
                bool useBackground = request.UseBackgroundProcessing || 
                                   ShouldUseBackgroundProcessing(request.Config);

                if (useBackground)
                {
                    // Start background job
                    var jobId = _generationService.StartBackgroundGeneration(request);
                    
                    await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                        "Background generation job started",
                        new { JobId = jobId, SessionId = request.SessionId });

                    return Accepted(new BackgroundJobResponse
                    {
                        JobId = jobId,
                        Status = "pending",
                        Message = "Generation started in background. Use the job ID to check status.",
                        StatusUrl = Url.Action(nameof(GetJobStatus), new { jobId })
                    });
                }
                else
                {
                    // Generate synchronously
                    var level = await _generationService.GenerateLevelAsync(request);
                    
                    await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                        "Synchronous generation completed",
                        new { 
                            SessionId = request.SessionId,
                            LevelName = level.Name,
                            EntityCount = level.Entities.Count
                        });

                    return Ok(level);
                }
            }
            catch (ArgumentException ex)
            {
                await _loggerService.LogErrorAsync(ex, "Invalid generation request", new { Request = request });
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Generation failed", new { Request = request });
                return StatusCode(500, new { error = "Internal server error during generation" });
            }
        }

        /// <summary>
        /// Validates a generation configuration without generating a level
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        /// <response code="200">Configuration is valid</response>
        /// <response code="400">Configuration is invalid</response>
        [HttpPost("validate-config")]
        [ProducesResponseType(typeof(WebApiModels.ValidationResult), 200)]
        [ProducesResponseType(typeof(WebApiModels.ValidationResult), 400)]
        public async Task<IActionResult> ValidateConfiguration([FromBody] CoreModels.GenerationConfig config)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration validation request received",
                    new { 
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm
                    });

                if (config == null)
                {
                    return BadRequest(WebApiModels.ValidationResult.Failure(new List<string> { "Configuration cannot be null" }));
                }

                var validationResult = _generationService.ValidateConfiguration(config);
                
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration validation completed",
                    new { 
                        IsValid = validationResult.IsValid,
                        ErrorCount = validationResult.Errors.Count
                    });

                if (validationResult.IsValid)
                {
                    return Ok(validationResult);
                }
                else
                {
                    return BadRequest(validationResult);
                }
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Configuration validation failed", new { Config = config });
                return StatusCode(500, WebApiModels.ValidationResult.Failure(new List<string> { "Internal server error during validation" }));
            }
        }

        /// <summary>
        /// Gets the status of a background generation job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Job status information</returns>
        /// <response code="200">Job status retrieved successfully</response>
        /// <response code="404">Job not found</response>
        [HttpGet("job/{jobId}/status")]
        [ProducesResponseType(typeof(WebApiModels.JobStatus), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetJobStatus([Required] string jobId)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                    "Job status request received",
                    new { JobId = jobId });

                var jobStatus = _generationService.GetJobStatus(jobId);
                
                if (jobStatus.Status == JobStatusType.NotFound)
                {
                    return NotFound(new { error = "Job not found or has expired" });
                }

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                    "Job status retrieved",
                    new { 
                        JobId = jobId,
                        Status = jobStatus.Status,
                        Progress = jobStatus.Progress
                    });

                return Ok(jobStatus);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to get job status", new { JobId = jobId });
                return StatusCode(500, new { error = "Internal server error while retrieving job status" });
            }
        }

        /// <summary>
        /// Gets a list of available generation algorithms
        /// </summary>
        /// <returns>List of algorithm names</returns>
        /// <response code="200">Algorithms retrieved successfully</response>
        [HttpGet("algorithms")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<IActionResult> GetAvailableAlgorithms()
        {
            try
            {
                // For now, return the known algorithms
                var algorithms = new List<string> { "perlin", "cellular", "maze", "rooms" };
                
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                    "Available algorithms requested",
                    new { AlgorithmCount = algorithms.Count });

                return Ok(algorithms);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to get available algorithms");
                return StatusCode(500, new { error = "Internal server error while retrieving algorithms" });
            }
        }

        /// <summary>
        /// Request a real-time preview generation with debouncing
        /// </summary>
        /// <param name="request">Preview request with configuration and session info</param>
        /// <returns>Acknowledgment that preview was requested</returns>
        /// <response code="200">Preview request accepted</response>
        /// <response code="400">Invalid configuration</response>
        [HttpPost("preview")]
        [ProducesResponseType(typeof(PreviewRequestResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<IActionResult> RequestPreview([FromBody] PreviewRequest request)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Real-time preview request received",
                    new { 
                        SessionId = request.SessionId,
                        ConfigSize = $"{request.Config.Width}x{request.Config.Height}",
                        DebounceMs = request.DebounceMs
                    });

                // Validate the request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrEmpty(request.SessionId))
                {
                    return BadRequest(new { error = "SessionId is required for real-time preview" });
                }

                // Start debounced preview generation
                await _realTimeGenerationService.RequestDebouncedPreview(
                    request.SessionId, 
                    request.Config, 
                    request.DebounceMs);

                return Ok(new PreviewRequestResponse
                {
                    SessionId = request.SessionId,
                    Status = "requested",
                    Message = "Preview generation requested. Connect to SignalR hub for real-time updates."
                });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Preview request failed", new { Request = request });
                return StatusCode(500, new { error = "Internal server error during preview request" });
            }
        }

        /// <summary>
        /// Get the current status of a real-time preview
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Current preview status</returns>
        /// <response code="200">Status retrieved successfully</response>
        /// <response code="404">Session not found</response>
        [HttpGet("preview/{sessionId}/status")]
        [ProducesResponseType(typeof(PreviewStatus), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPreviewStatus([Required] string sessionId)
        {
            try
            {
                var status = await _realTimeGenerationService.GetPreviewStatus(sessionId);
                
                if (status.Status == "idle" && status.LastUpdated == null)
                {
                    return NotFound(new { error = "Session not found or no preview requested" });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to get preview status", new { SessionId = sessionId });
                return StatusCode(500, new { error = "Internal server error while retrieving preview status" });
            }
        }

        /// <summary>
        /// Cancel a pending real-time preview
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Cancellation confirmation</returns>
        /// <response code="200">Preview cancelled successfully</response>
        [HttpDelete("preview/{sessionId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CancelPreview([Required] string sessionId)
        {
            try
            {
                await _realTimeGenerationService.CancelPendingPreview(sessionId);
                
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Preview cancelled",
                    new { SessionId = sessionId });

                return Ok(new { message = "Preview cancelled successfully" });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to cancel preview", new { SessionId = sessionId });
                return StatusCode(500, new { error = "Internal server error while cancelling preview" });
            }
        }

        /// <summary>
        /// Generates multiple levels based on parameter variations
        /// </summary>
        /// <param name="request">Batch generation request with base config and variations</param>
        /// <returns>Job ID for tracking batch generation progress</returns>
        /// <response code="202">Batch generation started, returns job ID</response>
        /// <response code="400">Invalid batch configuration</response>
        /// <response code="500">Batch generation failed to start</response>
        [HttpPost("generate-batch")]
        [ProducesResponseType(typeof(BackgroundJobResponse), 202)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateBatch([FromBody] WebApiModels.BatchGenerationRequest request)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Batch generation request received",
                    new { 
                        SessionId = request.SessionId,
                        BaseConfigSize = $"{request.BaseConfig.Width}x{request.BaseConfig.Height}",
                        VariationCount = request.Variations.Count,
                        BatchCount = request.Count,
                        TotalExpectedLevels = CalculateTotalBatchLevels(request)
                    });

                // Validate the request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate base configuration
                var baseValidationResult = _generationService.ValidateConfiguration(request.BaseConfig);
                if (!baseValidationResult.IsValid)
                {
                    var problemDetails = new ValidationProblemDetails();
                    foreach (var error in baseValidationResult.Errors)
                    {
                        problemDetails.Errors.Add("BaseConfig", new[] { error });
                    }
                    return BadRequest(problemDetails);
                }

                // Validate batch parameters
                var batchValidation = ValidateBatchRequest(request);
                if (!batchValidation.IsValid)
                {
                    var problemDetails = new ValidationProblemDetails();
                    foreach (var error in batchValidation.Errors)
                    {
                        problemDetails.Errors.Add("BatchRequest", new[] { error });
                    }
                    return BadRequest(problemDetails);
                }

                // Start batch generation job
                var jobId = _generationService.StartBatchGeneration(request);
                
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Batch generation job started",
                    new { 
                        JobId = jobId, 
                        SessionId = request.SessionId,
                        TotalLevels = CalculateTotalBatchLevels(request)
                    });

                return Accepted(new BackgroundJobResponse
                {
                    JobId = jobId,
                    Status = "pending",
                    Message = $"Batch generation started for {CalculateTotalBatchLevels(request)} levels. Use the job ID to check status.",
                    StatusUrl = Url.Action(nameof(GetJobStatus), new { jobId })
                });
            }
            catch (ArgumentException ex)
            {
                await _loggerService.LogErrorAsync(ex, "Invalid batch generation request", new { Request = request });
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Batch generation failed to start", new { Request = request });
                return StatusCode(500, new { error = "Internal server error during batch generation startup" });
            }
        }

        /// <summary>
        /// Cancels a running batch generation job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Cancellation confirmation</returns>
        /// <response code="200">Batch generation cancelled successfully</response>
        /// <response code="404">Job not found</response>
        /// <response code="400">Job cannot be cancelled</response>
        [HttpDelete("batch/{jobId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CancelBatch([Required] string jobId)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Batch cancellation request received",
                    new { JobId = jobId });

                var success = _generationService.CancelBatchGeneration(jobId);
                
                if (!success)
                {
                    var jobStatus = _generationService.GetJobStatus(jobId);
                    if (jobStatus.Status == JobStatusType.NotFound)
                    {
                        return NotFound(new { error = "Batch job not found or has expired" });
                    }
                    else
                    {
                        return BadRequest(new { error = "Batch job cannot be cancelled in its current state" });
                    }
                }

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Batch generation cancelled",
                    new { JobId = jobId });

                return Ok(new { message = "Batch generation cancelled successfully" });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to cancel batch generation", new { JobId = jobId });
                return StatusCode(500, new { error = "Internal server error while cancelling batch generation" });
            }
        }

        /// <summary>
        /// Determines if background processing should be used based on configuration
        /// </summary>
        private static bool ShouldUseBackgroundProcessing(CoreModels.GenerationConfig config)
        {
            // Use background processing for large levels or complex configurations
            var totalTiles = config.Width * config.Height;
            var totalEntities = config.Entities?.Sum(e => e.Count) ?? 0;
            
            return totalTiles > 10000 || // Levels larger than 100x100
                   totalEntities > 1000 || // More than 1000 entities
                   (config.AlgorithmParameters?.Count ?? 0) > 10; // Complex parameter sets
        }

        /// <summary>
        /// Calculates the total number of levels that will be generated in a batch request
        /// </summary>
        private static int CalculateTotalBatchLevels(WebApiModels.BatchGenerationRequest request)
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
        /// Validates a batch generation request
        /// </summary>
        private static WebApiModels.ValidationResult ValidateBatchRequest(WebApiModels.BatchGenerationRequest request)
        {
            var errors = new List<string>();

            // Validate batch count
            if (request.Count <= 0)
            {
                errors.Add("Batch count must be greater than 0");
            }
            else if (request.Count > 50)
            {
                errors.Add("Batch count cannot exceed 50 levels per variation");
            }

            // Validate total levels
            var totalLevels = CalculateTotalBatchLevels(request);
            if (totalLevels > 1000)
            {
                errors.Add($"Total batch size ({totalLevels}) exceeds maximum limit of 1000 levels");
            }

            // Validate variations
            if (request.Variations != null)
            {
                for (int i = 0; i < request.Variations.Count; i++)
                {
                    var variation = request.Variations[i];
                    
                    if (string.IsNullOrEmpty(variation.Parameter))
                    {
                        errors.Add($"Variation {i + 1}: Parameter name is required");
                    }

                    if (variation.Values == null || variation.Values.Count == 0)
                    {
                        errors.Add($"Variation {i + 1}: At least one value is required");
                    }
                    else if (variation.Values.Count > 20)
                    {
                        errors.Add($"Variation {i + 1}: Cannot have more than 20 values per parameter");
                    }

                    // Validate parameter exists and values are appropriate type
                    if (!string.IsNullOrEmpty(variation.Parameter))
                    {
                        var validationError = ValidateVariationParameter(variation.Parameter, variation.Values);
                        if (!string.IsNullOrEmpty(validationError))
                        {
                            errors.Add($"Variation {i + 1}: {validationError}");
                        }
                    }
                }
            }

            return errors.Count == 0 
                ? WebApiModels.ValidationResult.Success() 
                : WebApiModels.ValidationResult.Failure(errors);
        }

        /// <summary>
        /// Validates a specific variation parameter and its values
        /// </summary>
        private static string? ValidateVariationParameter(string parameter, List<object> values)
        {
            // Define valid parameters and their expected types
            var validParameters = new Dictionary<string, Type>
            {
                { "seed", typeof(int) },
                { "width", typeof(int) },
                { "height", typeof(int) },
                { "generationAlgorithm", typeof(string) },
                { "visualTheme.themeName", typeof(string) },
                { "gameplay.difficulty", typeof(string) },
                { "gameplay.playerSpeed", typeof(double) },
                { "gameplay.timeLimit", typeof(int) }
            };

            if (!validParameters.ContainsKey(parameter))
            {
                return $"Unknown parameter '{parameter}'. Valid parameters: {string.Join(", ", validParameters.Keys)}";
            }

            var expectedType = validParameters[parameter];
            
            // Validate each value
            foreach (var value in values)
            {
                if (value == null)
                {
                    return $"Parameter '{parameter}' cannot have null values";
                }

                // Type validation
                if (expectedType == typeof(int))
                {
                    if (!int.TryParse(value.ToString(), out var intValue))
                    {
                        return $"Parameter '{parameter}' requires integer values";
                    }
                    
                    // Additional validation for specific parameters
                    if (parameter == "width" || parameter == "height")
                    {
                        if (intValue < 5 || intValue > 200)
                        {
                            return $"Parameter '{parameter}' must be between 5 and 200";
                        }
                    }
                    else if (parameter == "seed")
                    {
                        if (intValue < 0)
                        {
                            return $"Parameter '{parameter}' must be non-negative";
                        }
                    }
                }
                else if (expectedType == typeof(double))
                {
                    if (!double.TryParse(value.ToString(), out var doubleValue))
                    {
                        return $"Parameter '{parameter}' requires numeric values";
                    }
                    
                    if (parameter == "gameplay.playerSpeed" && (doubleValue <= 0 || doubleValue > 20))
                    {
                        return $"Parameter '{parameter}' must be between 0 and 20";
                    }
                }
                else if (expectedType == typeof(string))
                {
                    var stringValue = value.ToString();
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        return $"Parameter '{parameter}' cannot have empty string values";
                    }

                    // Validate specific string parameters
                    if (parameter == "generationAlgorithm")
                    {
                        var validAlgorithms = new[] { "perlin", "cellular", "maze", "rooms" };
                        if (!validAlgorithms.Contains(stringValue))
                        {
                            return $"Parameter '{parameter}' must be one of: {string.Join(", ", validAlgorithms)}";
                        }
                    }
                    else if (parameter == "visualTheme.themeName")
                    {
                        var validThemes = new[] { "default", "dark", "forest", "desert", "ice" };
                        if (!validThemes.Contains(stringValue))
                        {
                            return $"Parameter '{parameter}' must be one of: {string.Join(", ", validThemes)}";
                        }
                    }
                    else if (parameter == "gameplay.difficulty")
                    {
                        var validDifficulties = new[] { "easy", "normal", "hard", "expert" };
                        if (!validDifficulties.Contains(stringValue))
                        {
                            return $"Parameter '{parameter}' must be one of: {string.Join(", ", validDifficulties)}";
                        }
                    }
                }
            }

            return null; // No validation errors
        }
    }

    /// <summary>
    /// Response model for background job creation
    /// </summary>
    public class BackgroundJobResponse
    {
        /// <summary>
        /// Unique job identifier
        /// </summary>
        public string JobId { get; set; } = string.Empty;
        
        /// <summary>
        /// Current job status
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Human-readable message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// URL to check job status
        /// </summary>
        public string? StatusUrl { get; set; }
    }

    /// <summary>
    /// Request model for real-time preview generation
    /// </summary>
    public class PreviewRequest
    {
        /// <summary>
        /// Session identifier for grouping real-time updates
        /// </summary>
        [Required]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Generation configuration
        /// </summary>
        [Required]
        public CoreModels.GenerationConfig Config { get; set; } = new();

        /// <summary>
        /// Debounce delay in milliseconds (default: 500ms)
        /// </summary>
        public int DebounceMs { get; set; } = 500;
    }

    /// <summary>
    /// Response model for preview request
    /// </summary>
    public class PreviewRequestResponse
    {
        /// <summary>
        /// Session identifier
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Request status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
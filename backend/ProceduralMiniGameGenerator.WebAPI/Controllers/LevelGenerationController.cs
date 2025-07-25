using Microsoft.AspNetCore.Mvc;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Controllers
{
    /// <summary>
    /// Controller for core level generation operations
    /// </summary>
    [ApiController]
    [Route("api/generation")]
    [Produces("application/json")]
    public class LevelGenerationController : ControllerBase
    {
        private readonly ILevelGenerationService _levelGenerationService;
        private readonly IJobStatusService _jobStatusService;
        private readonly ILogger<LevelGenerationController> _logger;

        public LevelGenerationController(
            ILevelGenerationService levelGenerationService,
            IJobStatusService jobStatusService,
            ILogger<LevelGenerationController> logger)
        {
            _levelGenerationService = levelGenerationService ?? throw new ArgumentNullException(nameof(levelGenerationService));
            _jobStatusService = jobStatusService ?? throw new ArgumentNullException(nameof(jobStatusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        [HttpPost]
        [ProducesResponseType(typeof(Level), 200)]
        [ProducesResponseType(typeof(BackgroundJobResponse), 202)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> GenerateLevel([FromBody] WebGenerationRequest request)
        {
            _logger.LogInformation("Generation request received for session {SessionId}", request.SessionId);

            // Determine if we should use background processing
            bool useBackground = request.UseBackgroundProcessing || 
                               _levelGenerationService.ShouldUseBackgroundProcessing(request.Config);

            if (useBackground)
            {
                return await StartBackgroundGeneration(request);
            }
            else
            {
                return await GenerateSynchronously(request);
            }
        }

        /// <summary>
        /// Validates a generation configuration without generating a level
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        /// <response code="200">Configuration is valid</response>
        /// <response code="400">Configuration is invalid</response>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ValidationResponse), 200)]
        [ProducesResponseType(typeof(ValidationResponse), 400)]
        public async Task<IActionResult> ValidateConfiguration([FromBody] GenerationConfig config)
        {
            _logger.LogInformation("Configuration validation request received");

            if (config == null)
            {
                var errorResponse = new ValidationResponse
                {
                    IsValid = false,
                    Errors = new[] { "Configuration cannot be null" }
                };
                return BadRequest(errorResponse);
            }

            var validationResult = _levelGenerationService.ValidateConfiguration(config);
            
            var response = new ValidationResponse
            {
                IsValid = validationResult.IsSuccess,
                Errors = validationResult.IsSuccess ? Array.Empty<string>() : new[] { validationResult.Error }
            };

            _logger.LogInformation("Configuration validation completed: {IsValid}", response.IsValid);

            return response.IsValid ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Gets a list of available generation algorithms
        /// </summary>
        /// <returns>List of algorithm names</returns>
        /// <response code="200">Algorithms retrieved successfully</response>
        /// <response code="500">Failed to retrieve algorithms</response>
        [HttpGet("algorithms")]
        [ProducesResponseType(typeof(IReadOnlyList<string>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> GetAvailableAlgorithms()
        {
            _logger.LogDebug("Available algorithms requested");

            var result = await _levelGenerationService.GetAvailableAlgorithmsAsync();
            
            return result.Match<IActionResult>(
                algorithms => Ok(algorithms),
                error => Problem(error, statusCode: 500)
            );
        }

        /// <summary>
        /// Gets the status of a background generation job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Job status information</returns>
        /// <response code="200">Job status retrieved successfully</response>
        /// <response code="404">Job not found</response>
        /// <response code="500">Failed to retrieve job status</response>
        [HttpGet("jobs/{jobId}/status")]
        [ProducesResponseType(typeof(JobStatus), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> GetJobStatus([Required] string jobId)
        {
            _logger.LogDebug("Job status request received for {JobId}", jobId);

            var result = await _jobStatusService.GetJobStatusAsync(jobId);
            
            return result.Match<IActionResult>(
                status => Ok(status),
                error => error.Contains("not found") ? NotFound(new ProblemDetails { Detail = error }) 
                                                     : Problem(error, statusCode: 500)
            );
        }

        /// <summary>
        /// Generates a level synchronously
        /// </summary>
        private async Task<IActionResult> GenerateSynchronously(WebGenerationRequest request)
        {
            var result = await _levelGenerationService.GenerateLevelAsync(request);
            
            return result.Match<IActionResult>(
                level => {
                    _logger.LogInformation("Synchronous generation completed for session {SessionId}", request.SessionId);
                    return Ok(level);
                },
                error => {
                    _logger.LogWarning("Synchronous generation failed for session {SessionId}: {Error}", request.SessionId, error);
                    return BadRequest(new ProblemDetails { Detail = error });
                }
            );
        }

        /// <summary>
        /// Starts background generation and returns job ID
        /// </summary>
        private async Task<IActionResult> StartBackgroundGeneration(WebGenerationRequest request)
        {
            // TODO: Implement background job service integration
            // For now, return a placeholder response
            var jobId = Guid.NewGuid().ToString();
            
            var createResult = await _jobStatusService.CreateJobStatusAsync(jobId, "generation", new Dictionary<string, object>
            {
                ["sessionId"] = request.SessionId ?? string.Empty,
                ["configSize"] = $"{request.Config.Width}x{request.Config.Height}",
                ["algorithm"] = request.Config.GenerationAlgorithm
            });

            if (createResult.IsFailure)
            {
                return Problem(createResult.Error, statusCode: 500);
            }

            _logger.LogInformation("Background generation job {JobId} started for session {SessionId}", jobId, request.SessionId);

            var response = new BackgroundJobResponse
            {
                JobId = jobId,
                Status = "pending",
                Message = "Generation started in background. Use the job ID to check status.",
                StatusUrl = Url.Action(nameof(GetJobStatus), new { jobId })
            };

            return Accepted(response);
        }
    }

    /// <summary>
    /// Response model for validation operations
    /// </summary>
    public class ValidationResponse
    {
        public bool IsValid { get; set; }
        public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();
    }
}
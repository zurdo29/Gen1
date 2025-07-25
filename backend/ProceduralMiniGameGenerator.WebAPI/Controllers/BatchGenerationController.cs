using Microsoft.AspNetCore.Mvc;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Controllers
{
    /// <summary>
    /// Controller for batch generation operations
    /// </summary>
    [ApiController]
    [Route("api/generation/batch")]
    [Produces("application/json")]
    public class BatchGenerationController : ControllerBase
    {
        private readonly IBatchGenerationService _batchGenerationService;
        private readonly IJobStatusService _jobStatusService;
        private readonly ILogger<BatchGenerationController> _logger;

        public BatchGenerationController(
            IBatchGenerationService batchGenerationService,
            IJobStatusService jobStatusService,
            ILogger<BatchGenerationController> logger)
        {
            _batchGenerationService = batchGenerationService ?? throw new ArgumentNullException(nameof(batchGenerationService));
            _jobStatusService = jobStatusService ?? throw new ArgumentNullException(nameof(jobStatusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates multiple levels based on parameter variations
        /// </summary>
        /// <param name="request">Batch generation request with base config and variations</param>
        /// <returns>Job ID for tracking batch generation progress</returns>
        /// <response code="202">Batch generation started, returns job ID</response>
        /// <response code="400">Invalid batch configuration</response>
        /// <response code="500">Batch generation failed to start</response>
        [HttpPost]
        [ProducesResponseType(typeof(BackgroundJobResponse), 202)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> GenerateBatch([FromBody] BatchGenerationRequest request)
        {
            _logger.LogInformation("Batch generation request received for session {SessionId} with {TotalLevels} expected levels",
                request.SessionId, _batchGenerationService.CalculateTotalBatchLevels(request));

            var result = await _batchGenerationService.StartBatchGenerationAsync(request);

            return result.Match<IActionResult>(
                jobId => {
                    var response = new BackgroundJobResponse
                    {
                        JobId = jobId,
                        Status = "pending",
                        Message = $"Batch generation started for {_batchGenerationService.CalculateTotalBatchLevels(request)} levels. Use the job ID to check status.",
                        StatusUrl = Url.Action("GetJobStatus", "LevelGeneration", new { jobId })
                    };
                    
                    _logger.LogInformation("Batch generation job {JobId} started for session {SessionId}", jobId, request.SessionId);
                    return Accepted(response);
                },
                error => {
                    _logger.LogWarning("Batch generation failed to start for session {SessionId}: {Error}", request.SessionId, error);
                    return BadRequest(new ProblemDetails { Detail = error });
                }
            );
        }

        /// <summary>
        /// Cancels a running batch generation job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Cancellation confirmation</returns>
        /// <response code="200">Batch generation cancelled successfully</response>
        /// <response code="404">Job not found</response>
        /// <response code="400">Job cannot be cancelled</response>
        /// <response code="500">Failed to cancel job</response>
        [HttpDelete("{jobId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> CancelBatch([Required] string jobId)
        {
            _logger.LogInformation("Batch cancellation request received for job {JobId}", jobId);

            var result = await _batchGenerationService.CancelBatchGenerationAsync(jobId);

            return result.Match<IActionResult>(
                () => {
                    _logger.LogInformation("Batch generation job {JobId} cancelled successfully", jobId);
                    return Ok(new { message = "Batch generation cancelled successfully" });
                },
                error => {
                    if (error.Contains("not found"))
                    {
                        return NotFound(new ProblemDetails { Detail = error });
                    }
                    else if (error.Contains("cannot be cancelled"))
                    {
                        return BadRequest(new ProblemDetails { Detail = error });
                    }
                    else
                    {
                        _logger.LogError("Failed to cancel batch generation job {JobId}: {Error}", jobId, error);
                        return Problem(error, statusCode: 500);
                    }
                }
            );
        }

        /// <summary>
        /// Validates a batch generation request without starting generation
        /// </summary>
        /// <param name="request">Batch generation request to validate</param>
        /// <returns>Validation result with total level count</returns>
        /// <response code="200">Batch request is valid</response>
        /// <response code="400">Batch request is invalid</response>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(BatchValidationResponse), 200)]
        [ProducesResponseType(typeof(BatchValidationResponse), 400)]
        public async Task<IActionResult> ValidateBatchRequest([FromBody] BatchGenerationRequest request)
        {
            _logger.LogInformation("Batch validation request received for session {SessionId}", request.SessionId);

            if (request == null)
            {
                var errorResponse = new BatchValidationResponse
                {
                    IsValid = false,
                    Errors = new[] { "Batch request cannot be null" },
                    TotalLevels = 0
                };
                return BadRequest(errorResponse);
            }

            var validationResult = _batchGenerationService.ValidateBatchRequest(request);
            var totalLevels = _batchGenerationService.CalculateTotalBatchLevels(request);

            var response = new BatchValidationResponse
            {
                IsValid = validationResult.IsSuccess,
                Errors = validationResult.IsSuccess ? Array.Empty<string>() : new[] { validationResult.Error },
                TotalLevels = totalLevels,
                VariationCombinations = request.Variations?.Count > 0 
                    ? request.Variations.Aggregate(1, (total, variation) => total * Math.Max(variation.Values?.Count ?? 1, 1))
                    : 1
            };

            _logger.LogInformation("Batch validation completed for session {SessionId}: {IsValid}, {TotalLevels} levels", 
                request.SessionId, response.IsValid, response.TotalLevels);

            return await Task.FromResult(response.IsValid ? Ok(response) : BadRequest(response));
        }
    }

    /// <summary>
    /// Response model for batch validation operations
    /// </summary>
    public class BatchValidationResponse
    {
        public bool IsValid { get; set; }
        public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();
        public int TotalLevels { get; set; }
        public int VariationCombinations { get; set; }
    }
}
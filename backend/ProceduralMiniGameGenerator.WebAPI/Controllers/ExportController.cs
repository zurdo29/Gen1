using Microsoft.AspNetCore.Mvc;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Controllers
{
    /// <summary>
    /// Controller for level export functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;
        private readonly ILoggerService _loggerService;

        public ExportController(IExportService exportService, ILoggerService loggerService)
        {
            _exportService = exportService;
            _loggerService = loggerService;
        }

        /// <summary>
        /// Gets all available export formats
        /// </summary>
        /// <returns>List of available export formats with their capabilities</returns>
        /// <response code="200">Returns the list of available export formats</response>
        [HttpGet("formats")]
        [ProducesResponseType(typeof(List<ExportFormat>), 200)]
        public async Task<ActionResult<List<ExportFormat>>> GetAvailableFormats()
        {
            try
            {
                await _loggerService.LogAsync(LogLevel.Information, "Getting available export formats");
                var formats = await _exportService.GetAvailableFormatsAsync();
                return Ok(formats);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to get available export formats");
                return StatusCode(500, new { error = "Failed to retrieve export formats", details = ex.Message });
            }
        }

        /// <summary>
        /// Exports a single level to the specified format
        /// </summary>
        /// <param name="request">Export request containing level data and format specifications</param>
        /// <returns>File download with the exported level data</returns>
        /// <response code="200">Returns the exported level file</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="500">Export operation failed</response>
        [HttpPost("level")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> ExportLevel([FromBody] ExportRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _loggerService.LogAsync(LogLevel.Information, 
                    "Starting level export", 
                    new { Format = request.Format, LevelName = request.Level?.Name });

                var result = await _exportService.ExportLevelAsync(request);

                if (!result.Success)
                {
                    var problemDetails = new ValidationProblemDetails();
                    foreach (var error in result.Errors)
                    {
                        problemDetails.Errors.Add("export", new[] { error });
                    }
                    
                    foreach (var warning in result.Warnings)
                    {
                        problemDetails.Errors.Add("warning", new[] { warning });
                    }

                    return BadRequest(problemDetails);
                }

                if (result.FileData == null)
                {
                    return StatusCode(500, new { error = "Export completed but no file data was generated" });
                }

                await _loggerService.LogAsync(LogLevel.Information, 
                    "Level export completed successfully", 
                    new { 
                        Format = request.Format, 
                        FileName = result.FileName, 
                        FileSize = result.FileSize,
                        Duration = result.ExportTime
                    });

                return File(result.FileData, result.MimeType ?? "application/octet-stream", result.FileName);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Level export failed", 
                    new { Format = request.Format, LevelName = request.Level?.Name });
                
                return StatusCode(500, new { 
                    error = "Export operation failed", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Starts a batch export operation for multiple levels
        /// </summary>
        /// <param name="request">Batch export request containing multiple levels and format specifications</param>
        /// <returns>Job ID for tracking the batch export progress</returns>
        /// <response code="202">Batch export job started successfully</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="500">Failed to start batch export</response>
        [HttpPost("batch")]
        [ProducesResponseType(typeof(object), 202)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> ExportBatch([FromBody] BatchExportRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.Levels == null || request.Levels.Count == 0)
                {
                    return BadRequest(new { error = "At least one level must be provided for batch export" });
                }

                if (request.Levels.Count > 50)
                {
                    return BadRequest(new { error = "Maximum 50 levels can be exported in a single batch" });
                }

                await _loggerService.LogAsync(LogLevel.Information, 
                    "Starting batch export", 
                    new { LevelCount = request.Levels.Count, Format = request.Format });

                var jobId = await _exportService.ExportBatchAsync(request);

                return Accepted(new { 
                    jobId = jobId, 
                    message = "Batch export started successfully",
                    statusUrl = Url.Action(nameof(GetBatchExportStatus), new { jobId }),
                    downloadUrl = Url.Action(nameof(DownloadBatchExport), new { jobId })
                });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to start batch export", 
                    new { LevelCount = request.Levels?.Count, Format = request.Format });
                
                return StatusCode(500, new { 
                    error = "Failed to start batch export", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Gets the status of a batch export job
        /// </summary>
        /// <param name="jobId">The job identifier returned from the batch export request</param>
        /// <returns>Current status and progress of the batch export job</returns>
        /// <response code="200">Returns the current job status</response>
        /// <response code="404">Job not found</response>
        /// <response code="500">Failed to retrieve job status</response>
        [HttpGet("batch/{jobId}/status")]
        [ProducesResponseType(typeof(JobStatus), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<ActionResult<JobStatus>> GetBatchExportStatus([Required] string jobId)
        {
            try
            {
                var status = await _exportService.GetBatchExportStatusAsync(jobId);

                if (status.Status == "not_found")
                {
                    return NotFound(new { error = "Batch export job not found", jobId });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to get batch export status", new { JobId = jobId });
                
                return StatusCode(500, new { 
                    error = "Failed to retrieve job status", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Downloads the result of a completed batch export
        /// </summary>
        /// <param name="jobId">The job identifier returned from the batch export request</param>
        /// <returns>ZIP file containing all exported levels</returns>
        /// <response code="200">Returns the batch export ZIP file</response>
        /// <response code="404">Job not found or not completed</response>
        /// <response code="500">Failed to download batch export</response>
        [HttpGet("batch/{jobId}/download")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> DownloadBatchExport([Required] string jobId)
        {
            try
            {
                // Check job status first
                var status = await _exportService.GetBatchExportStatusAsync(jobId);

                if (status.Status == "not_found")
                {
                    return NotFound(new { error = "Batch export job not found", jobId });
                }

                if (status.Status != "completed")
                {
                    return BadRequest(new { 
                        error = "Batch export is not completed yet", 
                        currentStatus = status.Status,
                        progress = status.Progress 
                    });
                }

                var fileResult = await _exportService.DownloadBatchExportAsync(jobId);

                if (fileResult == null)
                {
                    return NotFound(new { error = "Batch export result not found", jobId });
                }

                await _loggerService.LogAsync(LogLevel.Information, 
                    "Batch export downloaded", 
                    new { JobId = jobId, FileSize = fileResult.Data.Length });

                return File(fileResult.Data, fileResult.MimeType, fileResult.FileName);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to download batch export", new { JobId = jobId });
                
                return StatusCode(500, new { 
                    error = "Failed to download batch export", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Validates an export request without performing the actual export
        /// </summary>
        /// <param name="request">Export request to validate</param>
        /// <returns>Validation result with any errors or warnings</returns>
        /// <response code="200">Validation completed</response>
        /// <response code="400">Invalid request parameters</response>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ValidationResult), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        public async Task<ActionResult<ValidationResult>> ValidateExportRequest([FromBody] ExportRequest request)
        {
            try
            {
                var validationResult = new ValidationResult();

                // Basic validation
                if (request.Level == null)
                {
                    validationResult.Errors.Add("Level cannot be null");
                }

                if (string.IsNullOrEmpty(request.Format))
                {
                    validationResult.Errors.Add("Export format must be specified");
                }
                else
                {
                    var availableFormats = await _exportService.GetAvailableFormatsAsync();
                    var formatExists = availableFormats.Any(f => 
                        f.Id.Equals(request.Format, StringComparison.OrdinalIgnoreCase));
                    
                    if (!formatExists)
                    {
                        validationResult.Errors.Add($"Unsupported export format: {request.Format}");
                    }
                }

                // Level-specific validation
                if (request.Level != null)
                {
                    if (request.Level.Terrain == null)
                    {
                        validationResult.Warnings.Add("Level has no terrain data");
                    }
                    else if (request.Level.Terrain.Width <= 0 || request.Level.Terrain.Height <= 0)
                    {
                        validationResult.Errors.Add("Level terrain has invalid dimensions");
                    }

                    if (request.Level.Entities == null || request.Level.Entities.Count == 0)
                    {
                        validationResult.Warnings.Add("Level has no entities");
                    }
                }

                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Export validation failed");
                
                return StatusCode(500, new { 
                    error = "Validation failed", 
                    details = ex.Message 
                });
            }
        }
    }
}
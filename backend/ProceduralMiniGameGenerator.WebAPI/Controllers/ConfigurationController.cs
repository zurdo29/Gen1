using Microsoft.AspNetCore.Mvc;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;
using System.ComponentModel.DataAnnotations;

namespace ProceduralMiniGameGenerator.WebAPI.Controllers
{
    /// <summary>
    /// Controller for configuration preset management and sharing
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILoggerService _loggerService;

        public ConfigurationController(
            IConfigurationService configurationService,
            ILoggerService loggerService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Gets all available configuration presets
        /// </summary>
        /// <returns>List of configuration presets</returns>
        /// <response code="200">Presets retrieved successfully</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("presets")]
        [ProducesResponseType(typeof(List<ConfigPreset>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPresets()
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration presets requested");

                var presets = await _configurationService.GetPresetsAsync();

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration presets retrieved successfully",
                    new { PresetCount = presets.Count });

                return Ok(presets);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to retrieve configuration presets");
                return StatusCode(500, new { error = "Internal server error while retrieving presets" });
            }
        }

        /// <summary>
        /// Gets a specific configuration preset by ID
        /// </summary>
        /// <param name="id">Preset identifier</param>
        /// <returns>Configuration preset</returns>
        /// <response code="200">Preset retrieved successfully</response>
        /// <response code="404">Preset not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("presets/{id}")]
        [ProducesResponseType(typeof(ConfigPreset), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPreset([Required] string id)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration preset requested",
                    new { PresetId = id });

                var preset = await _configurationService.GetPresetAsync(id);

                if (preset == null)
                {
                    await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning,
                        "Configuration preset not found",
                        new { PresetId = id });

                    return NotFound(new { error = "Preset not found" });
                }

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration preset retrieved successfully",
                    new { PresetId = id, PresetName = preset.Name });

                return Ok(preset);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to retrieve configuration preset", new { PresetId = id });
                return StatusCode(500, new { error = "Internal server error while retrieving preset" });
            }
        }

        /// <summary>
        /// Saves a new configuration preset
        /// </summary>
        /// <param name="preset">Configuration preset to save</param>
        /// <returns>Saved configuration preset with generated ID</returns>
        /// <response code="201">Preset created successfully</response>
        /// <response code="400">Invalid preset data</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("presets")]
        [ProducesResponseType(typeof(ConfigPreset), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SavePreset([FromBody] ConfigPreset preset)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration preset save requested",
                    new { PresetName = preset?.Name });

                // Validate the request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (preset == null)
                {
                    return BadRequest(new { error = "Preset cannot be null" });
                }

                // Validate the configuration within the preset
                var configValidation = await _configurationService.ValidateConfigurationAsync(preset.Config);
                if (!configValidation.IsValid)
                {
                    var problemDetails = new ValidationProblemDetails();
                    foreach (var error in configValidation.Errors)
                    {
                        problemDetails.Errors.Add("Config", new[] { error });
                    }
                    return BadRequest(problemDetails);
                }

                var savedPreset = await _configurationService.SavePresetAsync(preset);

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration preset saved successfully",
                    new { PresetId = savedPreset.Id, PresetName = savedPreset.Name });

                return CreatedAtAction(nameof(GetPreset), new { id = savedPreset.Id }, savedPreset);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to save configuration preset", new { Preset = preset });
                return StatusCode(500, new { error = "Internal server error while saving preset" });
            }
        }

        /// <summary>
        /// Updates an existing configuration preset
        /// </summary>
        /// <param name="id">Preset identifier</param>
        /// <param name="preset">Updated preset data</param>
        /// <returns>Updated configuration preset</returns>
        /// <response code="200">Preset updated successfully</response>
        /// <response code="400">Invalid preset data</response>
        /// <response code="404">Preset not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("presets/{id}")]
        [ProducesResponseType(typeof(ConfigPreset), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdatePreset([Required] string id, [FromBody] ConfigPreset preset)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration preset update requested",
                    new { PresetId = id, PresetName = preset?.Name });

                // Validate the request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (preset == null)
                {
                    return BadRequest(new { error = "Preset cannot be null" });
                }

                // Validate the configuration within the preset
                var configValidation = await _configurationService.ValidateConfigurationAsync(preset.Config);
                if (!configValidation.IsValid)
                {
                    var problemDetails = new ValidationProblemDetails();
                    foreach (var error in configValidation.Errors)
                    {
                        problemDetails.Errors.Add("Config", new[] { error });
                    }
                    return BadRequest(problemDetails);
                }

                var updatedPreset = await _configurationService.UpdatePresetAsync(id, preset);

                if (updatedPreset == null)
                {
                    await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning,
                        "Configuration preset not found for update",
                        new { PresetId = id });

                    return NotFound(new { error = "Preset not found" });
                }

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration preset updated successfully",
                    new { PresetId = id, PresetName = updatedPreset.Name });

                return Ok(updatedPreset);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to update configuration preset", new { PresetId = id, Preset = preset });
                return StatusCode(500, new { error = "Internal server error while updating preset" });
            }
        }

        /// <summary>
        /// Deletes a configuration preset
        /// </summary>
        /// <param name="id">Preset identifier</param>
        /// <returns>Deletion confirmation</returns>
        /// <response code="204">Preset deleted successfully</response>
        /// <response code="404">Preset not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("presets/{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeletePreset([Required] string id)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration preset deletion requested",
                    new { PresetId = id });

                var deleted = await _configurationService.DeletePresetAsync(id);

                if (!deleted)
                {
                    await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning,
                        "Configuration preset not found for deletion",
                        new { PresetId = id });

                    return NotFound(new { error = "Preset not found" });
                }

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration preset deleted successfully",
                    new { PresetId = id });

                return NoContent();
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to delete configuration preset", new { PresetId = id });
                return StatusCode(500, new { error = "Internal server error while deleting preset" });
            }
        }

        /// <summary>
        /// Creates a shareable link for a configuration
        /// </summary>
        /// <param name="request">Share request with configuration and optional expiry</param>
        /// <returns>Share result with URL and metadata</returns>
        /// <response code="201">Share link created successfully</response>
        /// <response code="400">Invalid configuration</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("share")]
        [ProducesResponseType(typeof(ShareResult), 201)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateShareLink([FromBody] ShareRequest request)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration share link creation requested",
                    new { 
                        ConfigSize = $"{request?.Config?.Width}x{request?.Config?.Height}",
                        Algorithm = request?.Config?.GenerationAlgorithm,
                        ExpiryDays = request?.ExpiryDays
                    });

                // Validate the request
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request?.Config == null)
                {
                    return BadRequest(new { error = "Configuration cannot be null" });
                }

                // Validate the configuration
                var configValidation = await _configurationService.ValidateConfigurationAsync(request.Config);
                if (!configValidation.IsValid)
                {
                    var problemDetails = new ValidationProblemDetails();
                    foreach (var error in configValidation.Errors)
                    {
                        problemDetails.Errors.Add("Config", new[] { error });
                    }
                    return BadRequest(problemDetails);
                }

                // Determine expiry
                TimeSpan? expiry = null;
                if (request.ExpiryDays.HasValue)
                {
                    if (request.ExpiryDays.Value < 1 || request.ExpiryDays.Value > 365)
                    {
                        return BadRequest(new { error = "Expiry days must be between 1 and 365" });
                    }
                    expiry = TimeSpan.FromDays(request.ExpiryDays.Value);
                }

                var shareResult = await _configurationService.CreateShareLinkAsync(request.Config, expiry);

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Configuration share link created successfully",
                    new { ShareId = shareResult.ShareId, ExpiresAt = shareResult.ExpiresAt });

                return CreatedAtAction(nameof(GetSharedConfiguration), new { id = shareResult.ShareId }, shareResult);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to create configuration share link", new { Request = request });
                return StatusCode(500, new { error = "Internal server error while creating share link" });
            }
        }

        /// <summary>
        /// Retrieves a shared configuration by share ID
        /// </summary>
        /// <param name="id">Share identifier</param>
        /// <returns>Shared configuration</returns>
        /// <response code="200">Shared configuration retrieved successfully</response>
        /// <response code="404">Share not found or expired</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("share/{id}")]
        [ProducesResponseType(typeof(GenerationConfig), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSharedConfiguration([Required] string id)
        {
            try
            {
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Shared configuration requested",
                    new { ShareId = id });

                var config = await _configurationService.GetSharedConfigurationAsync(id);

                if (config == null)
                {
                    await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning,
                        "Shared configuration not found or expired",
                        new { ShareId = id });

                    return NotFound(new { error = "Shared configuration not found or has expired" });
                }

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Shared configuration retrieved successfully",
                    new { 
                        ShareId = id,
                        ConfigSize = $"{config.Width}x{config.Height}",
                        Algorithm = config.GenerationAlgorithm
                    });

                return Ok(config);
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Failed to retrieve shared configuration", new { ShareId = id });
                return StatusCode(500, new { error = "Internal server error while retrieving shared configuration" });
            }
        }
    }

    /// <summary>
    /// Request model for creating share links
    /// </summary>
    public class ShareRequest
    {
        /// <summary>
        /// Configuration to share
        /// </summary>
        [Required]
        public GenerationConfig Config { get; set; } = null!;

        /// <summary>
        /// Optional expiry in days (1-365, default is 30)
        /// </summary>
        public int? ExpiryDays { get; set; }

        /// <summary>
        /// Optional description for the share
        /// </summary>
        public string? Description { get; set; }
    }
}
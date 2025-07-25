using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;

namespace ProceduralMiniGameGenerator.WebAPI.HealthChecks
{
    /// <summary>
    /// Health check for the generation service
    /// </summary>
    public class GenerationServiceHealthCheck : IHealthCheck
    {
        private readonly ILevelGenerationService _generationService;
        private readonly ILogger<GenerationServiceHealthCheck> _logger;

        public GenerationServiceHealthCheck(
            ILevelGenerationService generationService,
            ILogger<GenerationServiceHealthCheck> logger)
        {
            _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Test basic configuration validation
                var testConfig = new GenerationConfig
                {
                    Width = 10,
                    Height = 10,
                    Seed = 12345,
                    GenerationAlgorithm = "perlin"
                };

                var validationResult = _generationService.ValidateConfiguration(testConfig);
                
                if (validationResult.IsFailure)
                {
                    _logger.LogWarning("Generation service health check failed: Configuration validation failed");
                    return HealthCheckResult.Degraded("Configuration validation is not working properly");
                }

                // Test algorithm availability
                var algorithmsResult = await _generationService.GetAvailableAlgorithmsAsync();
                
                if (algorithmsResult.IsFailure || !algorithmsResult.Value.Any())
                {
                    _logger.LogWarning("Generation service health check failed: No algorithms available");
                    return HealthCheckResult.Degraded("No generation algorithms are available");
                }

                _logger.LogDebug("Generation service health check passed");
                return HealthCheckResult.Healthy("Generation service is working properly");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generation service health check failed with exception");
                return HealthCheckResult.Unhealthy("Generation service is not responding", ex);
            }
        }
    }
}
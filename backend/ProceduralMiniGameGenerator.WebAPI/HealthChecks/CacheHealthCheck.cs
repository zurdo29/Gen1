using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProceduralMiniGameGenerator.WebAPI.HealthChecks
{
    /// <summary>
    /// Health check for the memory cache
    /// </summary>
    public class CacheHealthCheck : IHealthCheck
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheHealthCheck> _logger;

        public CacheHealthCheck(
            IMemoryCache cache,
            ILogger<CacheHealthCheck> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Test cache operations
                var testKey = $"health-check-{Guid.NewGuid()}";
                var testValue = "health-check-value";
                
                // Test set operation
                _cache.Set(testKey, testValue, TimeSpan.FromSeconds(30));
                
                // Test get operation
                if (_cache.TryGetValue(testKey, out var retrievedValue))
                {
                    if (retrievedValue?.ToString() == testValue)
                    {
                        // Clean up test data
                        _cache.Remove(testKey);
                        
                        _logger.LogDebug("Cache health check passed");
                        return await Task.FromResult(HealthCheckResult.Healthy("Memory cache is working properly"));
                    }
                    else
                    {
                        _logger.LogWarning("Cache health check failed: Retrieved value doesn't match stored value");
                        return HealthCheckResult.Degraded("Cache is not storing/retrieving values correctly");
                    }
                }
                else
                {
                    _logger.LogWarning("Cache health check failed: Could not retrieve stored value");
                    return HealthCheckResult.Degraded("Cache is not retrieving stored values");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache health check failed with exception");
                return HealthCheckResult.Unhealthy("Memory cache is not responding", ex);
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using ProceduralMiniGameGenerator.WebAPI.Services;

namespace ProceduralMiniGameGenerator.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILoggerService _loggerService;
    private readonly IServiceProvider _serviceProvider;

    public HealthController(ILoggerService loggerService, IServiceProvider serviceProvider)
    {
        _loggerService = loggerService;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public async Task<ActionResult<object>> Get()
    {
        var healthStatus = await GetHealthStatus();
        
        if (((dynamic)healthStatus).Status == "Unhealthy")
        {
            return StatusCode(503, healthStatus);
        }
        
        return Ok(healthStatus);
    }

    [HttpGet("detailed")]
    public async Task<ActionResult<object>> GetDetailed()
    {
        var healthStatus = await GetDetailedHealthStatus();
        
        if (((dynamic)healthStatus).Status == "Unhealthy")
        {
            return StatusCode(503, healthStatus);
        }
        
        return Ok(healthStatus);
    }

    [HttpGet("ready")]
    public async Task<ActionResult<object>> GetReadiness()
    {
        var readinessStatus = await GetReadinessStatus();
        
        if (((dynamic)readinessStatus).Status == "NotReady")
        {
            return StatusCode(503, readinessStatus);
        }
        
        return Ok(readinessStatus);
    }

    private async Task<object> GetHealthStatus()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            
            return new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = version,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                MachineName = Environment.MachineName,
                ProcessId = process.Id,
                Uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime()
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync(ex, "Health check failed", null);
            return new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    private async Task<object> GetDetailedHealthStatus()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            
            // Check service dependencies
            var serviceChecks = await CheckServiceDependencies();
            
            // Memory usage
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            
            // Thread count
            var threadCount = process.Threads.Count;
            
            var overallStatus = serviceChecks.All(s => ((dynamic)s).Status == "Healthy") ? "Healthy" : "Degraded";
            
            return new
            {
                Status = overallStatus,
                Timestamp = DateTime.UtcNow,
                Version = version,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                MachineName = Environment.MachineName,
                ProcessId = process.Id,
                Uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime(),
                Memory = new
                {
                    WorkingSetMB = workingSet / (1024 * 1024),
                    PrivateMemoryMB = privateMemory / (1024 * 1024)
                },
                ThreadCount = threadCount,
                Services = serviceChecks
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync(ex, "Detailed health check failed", null);
            return new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    private async Task<object> GetReadinessStatus()
    {
        try
        {
            // Check if all critical services are ready
            var serviceChecks = await CheckServiceDependencies();
            var criticalServices = serviceChecks.Where(s => ((dynamic)s).Critical).ToList();
            
            var isReady = criticalServices.All(s => ((dynamic)s).Status == "Healthy");
            
            return new
            {
                Status = isReady ? "Ready" : "NotReady",
                Timestamp = DateTime.UtcNow,
                Services = serviceChecks
            };
        }
        catch (Exception ex)
        {
            await _loggerService.LogErrorAsync(ex, "Readiness check failed", null);
            return new
            {
                Status = "NotReady",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    private async Task<List<object>> CheckServiceDependencies()
    {
        var checks = new List<object>();
        
        // Check Generation Service
        try
        {
            var generationService = _serviceProvider.GetService<IGenerationService>();
            checks.Add(new
            {
                Service = "GenerationService",
                Status = generationService != null ? "Healthy" : "Unhealthy",
                Critical = true,
                Message = generationService != null ? "Service available" : "Service not registered"
            });
        }
        catch (Exception ex)
        {
            checks.Add(new
            {
                Service = "GenerationService",
                Status = "Unhealthy",
                Critical = true,
                Message = ex.Message
            });
        }
        
        // Check Export Service
        try
        {
            var exportService = _serviceProvider.GetService<IExportService>();
            checks.Add(new
            {
                Service = "ExportService",
                Status = exportService != null ? "Healthy" : "Unhealthy",
                Critical = true,
                Message = exportService != null ? "Service available" : "Service not registered"
            });
        }
        catch (Exception ex)
        {
            checks.Add(new
            {
                Service = "ExportService",
                Status = "Unhealthy",
                Critical = true,
                Message = ex.Message
            });
        }
        
        // Check Configuration Service
        try
        {
            var configService = _serviceProvider.GetService<IConfigurationService>();
            checks.Add(new
            {
                Service = "ConfigurationService",
                Status = configService != null ? "Healthy" : "Unhealthy",
                Critical = false,
                Message = configService != null ? "Service available" : "Service not registered"
            });
        }
        catch (Exception ex)
        {
            checks.Add(new
            {
                Service = "ConfigurationService",
                Status = "Unhealthy",
                Critical = false,
                Message = ex.Message
            });
        }
        
        // Check Plugin Loader
        try
        {
            var pluginLoader = _serviceProvider.GetService<IPluginLoader>();
            checks.Add(new
            {
                Service = "PluginLoader",
                Status = pluginLoader != null ? "Healthy" : "Unhealthy",
                Critical = false,
                Message = pluginLoader != null ? "Service available" : "Service not registered"
            });
        }
        catch (Exception ex)
        {
            checks.Add(new
            {
                Service = "PluginLoader",
                Status = "Unhealthy",
                Critical = false,
                Message = ex.Message
            });
        }
        
        // Check file system access
        try
        {
            var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            
            Directory.CreateDirectory(logsPath);
            Directory.CreateDirectory(tempPath);
            
            // Test write access
            var testFile = Path.Combine(tempPath, $"health-check-{Guid.NewGuid()}.tmp");
            await System.IO.File.WriteAllTextAsync(testFile, "health check");
            System.IO.File.Delete(testFile);
            
            checks.Add(new
            {
                Service = "FileSystem",
                Status = "Healthy",
                Critical = true,
                Message = "Read/write access confirmed"
            });
        }
        catch (Exception ex)
        {
            checks.Add(new
            {
                Service = "FileSystem",
                Status = "Unhealthy",
                Critical = true,
                Message = ex.Message
            });
        }
        
        return checks;
    }
}
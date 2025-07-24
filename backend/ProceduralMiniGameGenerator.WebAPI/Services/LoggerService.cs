using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Enhanced logging service implementation with structured logging and performance tracking
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _scopeName;
        private readonly object? _scopeContext;
        
        public LoggerService(ILogger<LoggerService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        private LoggerService(ILogger<LoggerService> logger, IConfiguration configuration, string scopeName, object? scopeContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scopeName = scopeName;
            _scopeContext = scopeContext;
        }
        
        public async Task LogAsync(LogLevel level, string message, object? context = null)
        {
            var logContext = CreateLogContext(context);
            
            using var scope = _logger.BeginScope(logContext);
            _logger.Log(level, "{Message} {@Context}", message, logContext);
            
            await Task.CompletedTask;
        }
        
        public async Task LogGenerationAsync(string configId, string step, TimeSpan duration, object? metadata = null)
        {
            var context = new
            {
                ConfigId = configId,
                Step = step,
                DurationMs = duration.TotalMilliseconds,
                Metadata = metadata,
                Timestamp = DateTimeOffset.UtcNow,
                Category = "Generation"
            };
            
            await LogAsync(LogLevel.Information, $"Generation step '{step}' completed in {duration.TotalMilliseconds:F2}ms", context);
        }
        
        public async Task LogErrorAsync(Exception exception, string context, object? additionalData = null)
        {
            var errorContext = new
            {
                Context = context,
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exception.Message,
                StackTrace = exception.StackTrace,
                AdditionalData = additionalData,
                Timestamp = DateTimeOffset.UtcNow,
                Category = "Error"
            };
            
            using var scope = _logger.BeginScope(errorContext);
            _logger.LogError(exception, "Error in {Context}: {Message} {@ErrorContext}", 
                context, exception.Message, errorContext);
            
            await Task.CompletedTask;
        }
        
        public async Task LogPerformanceAsync(string operation, TimeSpan duration, object? metrics = null)
        {
            var performanceContext = new
            {
                Operation = operation,
                DurationMs = duration.TotalMilliseconds,
                Metrics = metrics,
                Timestamp = DateTimeOffset.UtcNow,
                Category = "Performance"
            };
            
            var logLevel = duration.TotalMilliseconds > GetPerformanceThreshold(operation) 
                ? LogLevel.Warning 
                : LogLevel.Information;
            
            await LogAsync(logLevel, $"Operation '{operation}' took {duration.TotalMilliseconds:F2}ms", performanceContext);
        }
        
        public async Task LogRequestAsync(string requestId, string method, string path, int statusCode, TimeSpan duration)
        {
            var requestContext = new
            {
                RequestId = requestId,
                Method = method,
                Path = path,
                StatusCode = statusCode,
                DurationMs = duration.TotalMilliseconds,
                Timestamp = DateTimeOffset.UtcNow,
                Category = "Request"
            };
            
            var logLevel = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            
            await LogAsync(logLevel, $"{method} {path} responded {statusCode} in {duration.TotalMilliseconds:F2}ms", requestContext);
        }
        
        public ILoggerService CreateScoped(string scope, object? context = null)
        {
            return new LoggerService(_logger, _configuration, scope, context);
        }
        
        private object CreateLogContext(object? additionalContext)
        {
            var baseContext = new Dictionary<string, object?>
            {
                ["Timestamp"] = DateTimeOffset.UtcNow,
                ["MachineName"] = Environment.MachineName,
                ["ProcessId"] = Environment.ProcessId
            };
            
            if (!string.IsNullOrEmpty(_scopeName))
            {
                baseContext["Scope"] = _scopeName;
            }
            
            if (_scopeContext != null)
            {
                baseContext["ScopeContext"] = _scopeContext;
            }
            
            if (additionalContext != null)
            {
                baseContext["AdditionalContext"] = additionalContext;
            }
            
            return baseContext;
        }
        
        private double GetPerformanceThreshold(string operation)
        {
            // Get performance thresholds from configuration
            var thresholds = _configuration.GetSection("Logging:PerformanceThresholds");
            
            return operation.ToLowerInvariant() switch
            {
                var op when op.Contains("generation") => double.TryParse(thresholds["Generation"], out var gen) ? gen : 5000, // 5 seconds
                var op when op.Contains("export") => double.TryParse(thresholds["Export"], out var exp) ? exp : 3000, // 3 seconds
                var op when op.Contains("validation") => double.TryParse(thresholds["Validation"], out var val) ? val : 1000, // 1 second
                _ => double.TryParse(thresholds["Default"], out var def) ? def : 2000 // 2 seconds
            };
        }
    }
}
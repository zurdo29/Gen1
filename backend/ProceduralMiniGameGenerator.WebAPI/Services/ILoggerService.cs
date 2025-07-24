using Microsoft.Extensions.Logging;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Enhanced logging service interface for web application with structured logging and performance tracking
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// Logs a message with structured context
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        /// <param name="context">Additional context data</param>
        Task LogAsync(LogLevel level, string message, object? context = null);
        
        /// <summary>
        /// Logs generation operation with performance metrics
        /// </summary>
        /// <param name="configId">Configuration identifier</param>
        /// <param name="step">Generation step name</param>
        /// <param name="duration">Time taken for the step</param>
        /// <param name="metadata">Additional metadata</param>
        Task LogGenerationAsync(string configId, string step, TimeSpan duration, object? metadata = null);
        
        /// <summary>
        /// Logs error with context preservation
        /// </summary>
        /// <param name="exception">Exception to log</param>
        /// <param name="context">Error context</param>
        /// <param name="additionalData">Additional data for debugging</param>
        Task LogErrorAsync(Exception exception, string context, object? additionalData = null);
        
        /// <summary>
        /// Logs performance metrics for operations
        /// </summary>
        /// <param name="operation">Operation name</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="metrics">Additional performance metrics</param>
        Task LogPerformanceAsync(string operation, TimeSpan duration, object? metrics = null);
        
        /// <summary>
        /// Logs request/response information
        /// </summary>
        /// <param name="requestId">Unique request identifier</param>
        /// <param name="method">HTTP method</param>
        /// <param name="path">Request path</param>
        /// <param name="statusCode">Response status code</param>
        /// <param name="duration">Request duration</param>
        Task LogRequestAsync(string requestId, string method, string path, int statusCode, TimeSpan duration);
        
        /// <summary>
        /// Creates a scoped logger with additional context
        /// </summary>
        /// <param name="scope">Scope name</param>
        /// <param name="context">Scope context</param>
        /// <returns>Scoped logger service</returns>
        ILoggerService CreateScoped(string scope, object? context = null);
    }
}
using System;
using System.Threading.Tasks;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Enhanced logging service interface for structured logging and performance tracking
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// Logs a message asynchronously with the specified level
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        /// <param name="context">Optional context object</param>
        Task LogAsync(LogLevel level, string message, object? context = null);

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Debug message</param>
        /// <param name="context">Optional context object</param>
        Task LogDebugAsync(string message, object? context = null);

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Info message</param>
        /// <param name="context">Optional context object</param>
        Task LogInfoAsync(string message, object? context = null);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="context">Optional context object</param>
        Task LogWarningAsync(string message, object? context = null);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="context">Optional context object</param>
        Task LogErrorAsync(string message, object? context = null);

        /// <summary>
        /// Logs an error message with exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception details</param>
        /// <param name="context">Optional context object</param>
        Task LogErrorAsync(string message, Exception exception, object? context = null);

        /// <summary>
        /// Logs performance metrics for operations
        /// </summary>
        /// <param name="operation">Operation name</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="metrics">Optional metrics object</param>
        Task LogPerformanceAsync(string operation, TimeSpan duration, object? metrics = null);

        /// <summary>
        /// Logs generation-specific events
        /// </summary>
        /// <param name="configId">Configuration ID</param>
        /// <param name="operation">Generation operation</param>
        /// <param name="duration">Operation duration</param>
        /// <param name="metrics">Generation metrics</param>
        Task LogGenerationAsync(string configId, string operation, TimeSpan duration, object? metrics = null);

        /// <summary>
        /// Logs error with context preservation (alternative signature)
        /// </summary>
        /// <param name="exception">Exception details</param>
        /// <param name="context">Error context</param>
        /// <param name="additionalData">Additional data</param>
        Task LogErrorAsync(Exception exception, string context, object? additionalData = null);
    }
}
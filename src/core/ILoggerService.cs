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
    }
}
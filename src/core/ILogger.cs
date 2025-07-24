using System;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Interface for logging system messages
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message with the specified level
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        void Log(LogLevel level, string message);
        
        /// <summary>
        /// Logs a message with exception details
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        /// <param name="exception">Exception details</param>
        void Log(LogLevel level, string message, Exception exception);
        
        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Debug message</param>
        void Debug(string message);
        
        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Info message</param>
        void Info(string message);
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        void Warning(string message);
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Error message</param>
        void Error(string message);
        
        /// <summary>
        /// Logs an error message with exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="exception">Exception details</param>
        void Error(string message, Exception exception);
    }
}
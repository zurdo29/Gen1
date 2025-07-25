using System;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Simple logging service interface for the console application
    /// </summary>
    public interface ISimpleLoggerService
    {
        /// <summary>
        /// Logs an information message
        /// </summary>
        void LogInfo(string message, object context = null);
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        void LogWarning(string message, object context = null);
        
        /// <summary>
        /// Logs an error message
        /// </summary>
        void LogError(string message, Exception exception = null, object context = null);
        
        /// <summary>
        /// Logs performance metrics
        /// </summary>
        void LogPerformance(string operation, TimeSpan duration, object metrics = null);
        
        /// <summary>
        /// Logs generation step completion
        /// </summary>
        void LogGeneration(string operationId, string step, TimeSpan duration, object metadata = null);
        
        /// <summary>
        /// Creates a scoped logger with additional context
        /// </summary>
        ISimpleLoggerService CreateScoped(string scope, object context = null);

        /// <summary>
        /// Logs error with context preservation (async version)
        /// </summary>
        System.Threading.Tasks.Task LogErrorAsync(Exception exception, string context, object additionalData = null);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Console-based implementation of ISimpleLoggerService and ILoggerService for the console application
    /// </summary>
    public class ConsoleLoggerService : ISimpleLoggerService, ILoggerService
    {
        private readonly string _scope;
        private readonly object _context;

        public ConsoleLoggerService(string scope = null, object context = null)
        {
            _scope = scope;
            _context = context;
        }

        /// <summary>
        /// Logs an information message
        /// </summary>
        public void LogInfo(string message, object context = null)
        {
            LogMessage("INFO", message, context);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public void LogWarning(string message, object context = null)
        {
            LogMessage("WARNING", message, context);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public void LogError(string message, Exception exception = null, object context = null)
        {
            LogMessage("ERROR", message, context);
            if (exception != null)
            {
                Console.WriteLine($"  Exception: {exception.GetType().Name}: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    Console.WriteLine($"  Stack Trace: {exception.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Logs performance metrics
        /// </summary>
        public void LogPerformance(string operation, TimeSpan duration, object metrics = null)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var scopePrefix = !string.IsNullOrEmpty(_scope) ? $"[{_scope}] " : "";
            
            Console.WriteLine($"[{timestamp}] [PERFORMANCE] {scopePrefix}{operation}: {duration.TotalMilliseconds:F2}ms");
            
            if (metrics != null)
            {
                Console.WriteLine($"  Metrics: {SerializeContext(metrics)}");
            }
        }

        /// <summary>
        /// Logs generation step completion
        /// </summary>
        public void LogGeneration(string operationId, string step, TimeSpan duration, object metadata = null)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var scopePrefix = !string.IsNullOrEmpty(_scope) ? $"[{_scope}] " : "";
            
            Console.WriteLine($"[{timestamp}] [GENERATION] {scopePrefix}{step} completed in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Operation ID: {operationId}");
            
            if (metadata != null)
            {
                Console.WriteLine($"  Metadata: {SerializeContext(metadata)}");
            }
        }

        /// <summary>
        /// Creates a scoped logger with additional context
        /// </summary>
        public ISimpleLoggerService CreateScoped(string scope, object context = null)
        {
            var combinedScope = !string.IsNullOrEmpty(_scope) ? $"{_scope}.{scope}" : scope;
            return new ConsoleLoggerService(combinedScope, context);
        }

        /// <summary>
        /// Helper method to log messages with consistent formatting
        /// </summary>
        private void LogMessage(string level, string message, object context = null)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var scopePrefix = !string.IsNullOrEmpty(_scope) ? $"[{_scope}] " : "";
            
            // Use string interpolation for better performance
            Console.WriteLine($"[{timestamp}] [{level}] {scopePrefix}{message}");
            
            if (context != null)
            {
                Console.WriteLine($"  Context: {SerializeContext(context)}");
            }
        }

        // Legacy async methods for backward compatibility (these will delegate to sync methods)
        
        /// <summary>
        /// Logs a message with structured context (async version for compatibility)
        /// </summary>
        public Task LogAsync(LogLevel level, string message, object? context = null)
        {
            string levelString = level switch
            {
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO", 
                LogLevel.Warning => "WARNING",
                LogLevel.Error => "ERROR",
                _ => level.ToString().ToUpper()
            };
            
            LogMessage(levelString, message, context);
            return Task.CompletedTask;
        }



        /// <summary>
        /// Logs error with context preservation
        /// </summary>
        public Task LogErrorAsync(Exception exception, string context, object? additionalData = null)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var scopePrefix = !string.IsNullOrEmpty(_scope) ? $"[{_scope}] " : "";
            
            Console.WriteLine($"[{timestamp}] [ERROR] {scopePrefix}{context}");
            Console.WriteLine($"  Exception: {exception.GetType().Name}: {exception.Message}");
            
            if (exception.StackTrace != null)
            {
                Console.WriteLine($"  Stack Trace: {exception.StackTrace}");
            }
            
            if (additionalData != null)
            {
                Console.WriteLine($"  Additional Data: {SerializeContext(additionalData)}");
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs performance metrics for operations
        /// </summary>
        public Task LogPerformanceAsync(string operation, TimeSpan duration, object? metrics = null)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var scopePrefix = !string.IsNullOrEmpty(_scope) ? $"[{_scope}] " : "";
            
            Console.WriteLine($"[{timestamp}] [PERFORMANCE] {scopePrefix}{operation}: {duration.TotalMilliseconds:F2}ms");
            
            if (metrics != null)
            {
                Console.WriteLine($"  Metrics: {SerializeContext(metrics)}");
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs generation-specific events
        /// </summary>
        public Task LogGenerationAsync(string configId, string operation, TimeSpan duration, object? metrics = null)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var scopePrefix = !string.IsNullOrEmpty(_scope) ? $"[{_scope}] " : "";
            
            Console.WriteLine($"[{timestamp}] [GENERATION] {scopePrefix}{operation}: {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Config ID: {configId}");
            
            if (metrics != null)
            {
                Console.WriteLine($"  Metrics: {SerializeContext(metrics)}");
            }
            
            return Task.CompletedTask;
        }

        // Additional ILoggerService methods
        public Task LogDebugAsync(string message, object? context = null)
        {
            LogMessage("DEBUG", message, context);
            return Task.CompletedTask;
        }

        public Task LogInfoAsync(string message, object? context = null)
        {
            LogMessage("INFO", message, context);
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string message, object? context = null)
        {
            LogMessage("WARNING", message, context);
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, object? context = null)
        {
            LogMessage("ERROR", message, context);
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, Exception exception, object? context = null)
        {
            LogMessage("ERROR", message, context);
            if (exception != null)
            {
                Console.WriteLine($"  Exception: {exception.GetType().Name}: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    Console.WriteLine($"  Stack Trace: {exception.StackTrace}");
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs request/response information
        /// </summary>
        public Task LogRequestAsync(string requestId, string method, string path, int statusCode, TimeSpan duration)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var scopePrefix = !string.IsNullOrEmpty(_scope) ? $"[{_scope}] " : "";
            
            Console.WriteLine($"[{timestamp}] [REQUEST] {scopePrefix}{method} {path} -> {statusCode} ({duration.TotalMilliseconds:F2}ms)");
            Console.WriteLine($"  Request ID: {requestId}");
            
            return Task.CompletedTask;
        }



        /// <summary>
        /// Serializes context object to string for logging
        /// </summary>
        private string SerializeContext(object context)
        {
            if (context == null) return "null";
            
            try
            {
                // Simple serialization for console output
                if (context is string str)
                    return str;
                
                if (context.GetType().IsPrimitive || context is decimal)
                    return context.ToString();
                
                // For complex objects, use a simple property dump
                var properties = context.GetType().GetProperties();
                var parts = new List<string>();
                
                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(context);
                        parts.Add($"{prop.Name}={value ?? "null"}");
                    }
                    catch
                    {
                        parts.Add($"{prop.Name}=<error>");
                    }
                }
                
                return $"{{ {string.Join(", ", parts)} }}";
            }
            catch
            {
                return context.ToString();
            }
        }
    }
}
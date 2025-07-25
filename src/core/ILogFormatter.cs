using System;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Interface for formatting log messages
    /// </summary>
    public interface ILogFormatter
    {
        string FormatMessage(string level, string message, string scope = null);
        string FormatException(Exception exception);
        string FormatContext(object context);
    }

    /// <summary>
    /// Console-specific log formatter
    /// </summary>
    public class ConsoleLogFormatter : ILogFormatter
    {
        public string FormatMessage(string level, string message, string scope = null)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var scopePrefix = !string.IsNullOrEmpty(scope) ? $"[{scope}] " : "";
            return $"[{timestamp}] [{level}] {scopePrefix}{message}";
        }

        public string FormatException(Exception exception)
        {
            return $"  Exception: {exception.GetType().Name}: {exception.Message}";
        }

        public string FormatContext(object context)
        {
            if (context == null) return "null";
            
            try
            {
                if (context is string str) return str;
                if (context.GetType().IsPrimitive || context is decimal) return context.ToString();
                
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
using System;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Tests
{
    /// <summary>
    /// Simple console logger for testing purposes
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void Log(LogLevel level, string message)
        {
            Console.WriteLine($"[{level.ToString().ToUpper()}] {message}");
        }

        public void Log(LogLevel level, string message, Exception exception)
        {
            Console.WriteLine($"[{level.ToString().ToUpper()}] {message}: {exception.Message}");
        }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void Error(string message, Exception exception)
        {
            Log(LogLevel.Error, message, exception);
        }
    }
}
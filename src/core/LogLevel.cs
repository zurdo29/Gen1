namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Enumeration of log levels for structured logging
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Trace level for very detailed diagnostic information
        /// </summary>
        Trace = 0,
        
        /// <summary>
        /// Debug level for detailed diagnostic information
        /// </summary>
        Debug = 1,
        
        /// <summary>
        /// Information level for general application flow
        /// </summary>
        Information = 2,
        
        /// <summary>
        /// Warning level for potentially harmful situations
        /// </summary>
        Warning = 3,
        
        /// <summary>
        /// Error level for error events that might still allow the application to continue
        /// </summary>
        Error = 4,
        
        /// <summary>
        /// Critical level for critical errors that cause the application to terminate
        /// </summary>
        Critical = 5,
        
        /// <summary>
        /// None level to disable logging
        /// </summary>
        None = 6
    }
}
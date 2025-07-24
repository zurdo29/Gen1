namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Enumeration of log levels for structured logging
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level for detailed diagnostic information
        /// </summary>
        Debug = 0,
        
        /// <summary>
        /// Information level for general application flow
        /// </summary>
        Info = 1,
        
        /// <summary>
        /// Warning level for potentially harmful situations
        /// </summary>
        Warning = 2,
        
        /// <summary>
        /// Error level for error events that might still allow the application to continue
        /// </summary>
        Error = 3
    }
}
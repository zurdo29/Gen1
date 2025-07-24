using System;
using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration for build operations
    /// </summary>
    public class BuildConfiguration
    {
        /// <summary>
        /// Level to include in the build
        /// </summary>
        public Level? Level { get; set; }
        
        /// <summary>
        /// Output path for the executable
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Build settings to use
        /// </summary>
        public BuildSettings BuildSettings { get; set; } = new BuildSettings();
        
        /// <summary>
        /// Whether to include source code in the build
        /// </summary>
        public bool IncludeSource { get; set; } = false;
        
        /// <summary>
        /// Whether to create a self-contained executable
        /// </summary>
        public bool SelfContained { get; set; } = true;
        
        /// <summary>
        /// Additional files to include in the build
        /// </summary>
        public List<string> AdditionalFiles { get; set; } = new List<string>();
        
        /// <summary>
        /// Build timeout in milliseconds
        /// </summary>
        public int TimeoutMs { get; set; } = 300000; // 5 minutes default
        
        /// <summary>
        /// Whether to clean before building
        /// </summary>
        public bool CleanBuild { get; set; } = true;
        
        /// <summary>
        /// Additional build properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Result of a build operation
    /// </summary>
    public class BuildResult
    {
        /// <summary>
        /// Whether the build was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Path to the created executable (if successful)
        /// </summary>
        public string? ExecutablePath { get; set; }
        
        /// <summary>
        /// Build log output
        /// </summary>
        public string BuildLog { get; set; } = string.Empty;
        
        /// <summary>
        /// List of errors that occurred during build
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// List of warnings that occurred during build
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// Time taken for the build operation
        /// </summary>
        public TimeSpan BuildTime { get; set; }
        
        /// <summary>
        /// Size of the created executable in bytes
        /// </summary>
        public long ExecutableSize { get; set; }
        
        /// <summary>
        /// Build configuration that was used
        /// </summary>
        public BuildConfiguration? Configuration { get; set; }
        
        /// <summary>
        /// Additional result metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Result of a build validation operation
    /// </summary>
    public class BuildValidationResult
    {
        /// <summary>
        /// Whether the validation passed
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// List of validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// List of informational messages
        /// </summary>
        public List<string> Information { get; set; } = new List<string>();
        
        /// <summary>
        /// Validation context or source
        /// </summary>
        public string Context { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional validation metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets a summary of all validation messages
        /// </summary>
        public string GetSummary()
        {
            var parts = new List<string>();
            
            if (Errors.Count > 0)
                parts.Add($"{Errors.Count} error(s)");
            
            if (Warnings.Count > 0)
                parts.Add($"{Warnings.Count} warning(s)");
            
            if (Information.Count > 0)
                parts.Add($"{Information.Count} info message(s)");
            
            return parts.Count > 0 ? string.Join(", ", parts) : "No issues found";
        }
    }
}
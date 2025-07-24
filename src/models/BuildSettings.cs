using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Configuration settings for the build system
    /// </summary>
    public class BuildSettings
    {
        /// <summary>
        /// Target platform for the build
        /// </summary>
        public string TargetPlatform { get; set; } = "Windows";
        
        /// <summary>
        /// Build configuration (Debug, Release, etc.)
        /// </summary>
        public string Configuration { get; set; } = "Release";
        
        /// <summary>
        /// Whether to include debug symbols
        /// </summary>
        public bool IncludeDebugSymbols { get; set; } = false;
        
        /// <summary>
        /// Optimization level
        /// </summary>
        public string OptimizationLevel { get; set; } = "O2";
        
        /// <summary>
        /// Whether to optimize for size instead of speed
        /// </summary>
        public bool OptimizeForSize { get; set; } = false;
        
        /// <summary>
        /// Additional compiler flags
        /// </summary>
        public List<string> CompilerFlags { get; set; } = new List<string>();
        
        /// <summary>
        /// Additional build properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
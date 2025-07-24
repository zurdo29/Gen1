using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Interface for level export and import operations
    /// </summary>
    public interface ILevelExportService
    {
        /// <summary>
        /// Exports a level to JSON format with metadata and generation parameters
        /// </summary>
        /// <param name="level">Level to export</param>
        /// <param name="generationConfig">Configuration used to generate the level</param>
        /// <param name="outputPath">Output file path</param>
        /// <returns>Export result with success status and any errors</returns>
        ExportResult ExportLevel(Level level, GenerationConfig generationConfig, string outputPath);
        
        /// <summary>
        /// Exports a level to JSON string with metadata and generation parameters
        /// </summary>
        /// <param name="level">Level to export</param>
        /// <param name="generationConfig">Configuration used to generate the level</param>
        /// <returns>JSON string representation of the level</returns>
        string ExportLevelToJson(Level level, GenerationConfig generationConfig);
        
        /// <summary>
        /// Imports a level from JSON file
        /// </summary>
        /// <param name="jsonPath">Path to JSON file</param>
        /// <returns>Import result with level data and any errors</returns>
        ImportResult ImportLevel(string jsonPath);
        
        /// <summary>
        /// Imports a level from JSON string
        /// </summary>
        /// <param name="json">JSON string containing level data</param>
        /// <returns>Import result with level data and any errors</returns>
        ImportResult ImportLevelFromJson(string json);
        
        /// <summary>
        /// Validates that a JSON file contains valid level data
        /// </summary>
        /// <param name="jsonPath">Path to JSON file</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateExportedLevel(string jsonPath);
        
        /// <summary>
        /// Gets supported export formats
        /// </summary>
        /// <returns>List of supported format names</returns>
        List<string> GetSupportedFormats();
    }
    
    /// <summary>
    /// Result of a level export operation
    /// </summary>
    public class ExportResult
    {
        /// <summary>
        /// Whether the export was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Path where the level was exported
        /// </summary>
        public string ExportPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Size of the exported file in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Any errors that occurred during export
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Any warnings generated during export
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// Time taken for the export operation
        /// </summary>
        public System.TimeSpan ExportTime { get; set; }
    }
    
    /// <summary>
    /// Result of a level import operation
    /// </summary>
    public class ImportResult
    {
        /// <summary>
        /// Whether the import was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The imported level (null if import failed)
        /// </summary>
        public Level? Level { get; set; }
        
        /// <summary>
        /// The generation configuration used to create the level (if available)
        /// </summary>
        public GenerationConfig? GenerationConfig { get; set; }
        
        /// <summary>
        /// Any errors that occurred during import
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Any warnings generated during import
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// Time taken for the import operation
        /// </summary>
        public System.TimeSpan ImportTime { get; set; }
    }
}
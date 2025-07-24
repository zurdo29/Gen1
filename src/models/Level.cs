using System.Collections.Generic;
using System.Text.Json;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Represents a complete generated level
    /// </summary>
    public class Level
    {
        /// <summary>
        /// The terrain of the level
        /// </summary>
        public TileMap Terrain { get; set; } = null!;
        
        /// <summary>
        /// All entities in the level
        /// </summary>
        public List<Entity> Entities { get; set; } = new List<Entity>();
        
        /// <summary>
        /// Name of the level
        /// </summary>
        public string Name { get; set; } = "Generated Level";
        
        /// <summary>
        /// Additional metadata about the level
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Exports the level to JSON format using the enhanced export service
        /// </summary>
        /// <param name="generationConfig">Optional generation configuration used to create this level</param>
        /// <returns>JSON representation of the level</returns>
        public string ExportToJson(GenerationConfig? generationConfig = null)
        {
            var exportService = new Core.LevelExportService();
            return exportService.ExportLevelToJson(this, generationConfig ?? new GenerationConfig());
        }
        
        /// <summary>
        /// Exports the level to a file using the enhanced export service
        /// </summary>
        /// <param name="outputPath">Path where to save the exported level</param>
        /// <param name="generationConfig">Optional generation configuration used to create this level</param>
        /// <returns>Export result with success status and details</returns>
        public Core.ExportResult ExportToFile(string outputPath, GenerationConfig? generationConfig = null)
        {
            var exportService = new Core.LevelExportService();
            return exportService.ExportLevel(this, generationConfig ?? new GenerationConfig(), outputPath);
        }
        
        /// <summary>
        /// Imports a level from JSON format using the enhanced import service
        /// </summary>
        /// <param name="json">JSON string containing level data</param>
        /// <returns>Import result with level data and any errors</returns>
        public static Core.ImportResult ImportFromJson(string json)
        {
            var exportService = new Core.LevelExportService();
            return exportService.ImportLevelFromJson(json);
        }
        
        /// <summary>
        /// Imports a level from a JSON file using the enhanced import service
        /// </summary>
        /// <param name="jsonPath">Path to JSON file containing level data</param>
        /// <returns>Import result with level data and any errors</returns>
        public static Core.ImportResult ImportFromFile(string jsonPath)
        {
            var exportService = new Core.LevelExportService();
            return exportService.ImportLevel(jsonPath);
        }

    }
}
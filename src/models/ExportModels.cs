using System;
using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Root data structure for level export
    /// </summary>
    public class LevelExportData
    {
        /// <summary>
        /// Format version for compatibility checking
        /// </summary>
        public string FormatVersion { get; set; } = "1.0";
        
        /// <summary>
        /// Timestamp when the level was exported
        /// </summary>
        public DateTime ExportTimestamp { get; set; }
        
        /// <summary>
        /// The level data
        /// </summary>
        public LevelData Level { get; set; } = new LevelData();
        
        /// <summary>
        /// Configuration used to generate this level
        /// </summary>
        public GenerationConfig? GenerationConfig { get; set; }
        
        /// <summary>
        /// Statistical information about the level
        /// </summary>
        public LevelStatistics Statistics { get; set; } = new LevelStatistics();
        
        /// <summary>
        /// Additional export metadata
        /// </summary>
        public Dictionary<string, object> ExportMetadata { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Level data for export/import
    /// </summary>
    public class LevelData
    {
        /// <summary>
        /// Name of the level
        /// </summary>
        public string Name { get; set; } = "Exported Level";
        
        /// <summary>
        /// Width of the level in tiles
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// Height of the level in tiles
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// Terrain data as 2D array of tile type integers
        /// </summary>
        public int[,]? Terrain { get; set; }
        
        /// <summary>
        /// List of entities in the level
        /// </summary>
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        
        /// <summary>
        /// Additional level metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Entity data for export/import
    /// </summary>
    public class EntityData
    {
        /// <summary>
        /// Type of the entity as string
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Position of the entity
        /// </summary>
        public PositionData? Position { get; set; }
        
        /// <summary>
        /// Entity-specific properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Position data for export/import
    /// </summary>
    public class PositionData
    {
        /// <summary>
        /// X coordinate
        /// </summary>
        public float X { get; set; }
        
        /// <summary>
        /// Y coordinate
        /// </summary>
        public float Y { get; set; }
    }
    
    /// <summary>
    /// Statistical information about a level
    /// </summary>
    public class LevelStatistics
    {
        /// <summary>
        /// Total number of tiles in the level
        /// </summary>
        public int TotalTiles { get; set; }
        
        /// <summary>
        /// Number of walkable tiles
        /// </summary>
        public int WalkableTiles { get; set; }
        
        /// <summary>
        /// Number of wall tiles
        /// </summary>
        public int WallTiles { get; set; }
        
        /// <summary>
        /// Number of water tiles
        /// </summary>
        public int WaterTiles { get; set; }
        
        /// <summary>
        /// Total number of entities
        /// </summary>
        public int TotalEntities { get; set; }
        
        /// <summary>
        /// Number of player entities
        /// </summary>
        public int PlayerCount { get; set; }
        
        /// <summary>
        /// Number of enemy entities
        /// </summary>
        public int EnemyCount { get; set; }
        
        /// <summary>
        /// Number of item entities
        /// </summary>
        public int ItemCount { get; set; }
        
        /// <summary>
        /// Calculated navigability ratio (walkable tiles / total tiles)
        /// </summary>
        public float NavigabilityRatio => TotalTiles > 0 ? (float)WalkableTiles / TotalTiles : 0f;
        
        /// <summary>
        /// Calculated entity density (entities per 100 tiles)
        /// </summary>
        public float EntityDensity => TotalTiles > 0 ? (float)TotalEntities * 100 / TotalTiles : 0f;
    }
}
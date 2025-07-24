using System.Xml.Serialization;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Data structure for level export operations
    /// </summary>
    [XmlRoot("LevelExport")]
    public class LevelExportData
    {
        /// <summary>
        /// Format version for compatibility
        /// </summary>
        [XmlAttribute]
        public string FormatVersion { get; set; } = "1.0";
        
        /// <summary>
        /// Timestamp when the export was created
        /// </summary>
        [XmlAttribute]
        public DateTime ExportTimestamp { get; set; }
        
        /// <summary>
        /// Level data
        /// </summary>
        public LevelData Level { get; set; } = new LevelData();
        
        /// <summary>
        /// Level statistics
        /// </summary>
        public LevelStatistics Statistics { get; set; } = new LevelStatistics();
    }
    
    /// <summary>
    /// Level data for export
    /// </summary>
    public class LevelData
    {
        /// <summary>
        /// Level name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Level width
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// Level height
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// Terrain data as 2D array
        /// </summary>
        public int[,] Terrain { get; set; } = new int[0, 0];
        
        /// <summary>
        /// Entity data
        /// </summary>
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        
        /// <summary>
        /// Level metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Entity data for export
    /// </summary>
    public class EntityData
    {
        /// <summary>
        /// Entity type
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Entity position
        /// </summary>
        public PositionData Position { get; set; } = new PositionData();
        
        /// <summary>
        /// Entity properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Position data for export
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
    /// Level statistics for export
    /// </summary>
    public class LevelStatistics
    {
        /// <summary>
        /// Total number of tiles
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
    }
}
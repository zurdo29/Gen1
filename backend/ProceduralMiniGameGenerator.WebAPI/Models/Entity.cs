using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Entity model for WebAPI responses
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// Entity type
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Entity position
        /// </summary>
        public Position Position { get; set; } = new Position();
        
        /// <summary>
        /// Entity properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
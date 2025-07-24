namespace ProceduralMiniGameGenerator.Models
{
    /// <summary>
    /// Types of entities that can be placed in levels
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// Player character
        /// </summary>
        Player,
        
        /// <summary>
        /// Enemy character
        /// </summary>
        Enemy,
        
        /// <summary>
        /// Collectible item
        /// </summary>
        Item,
        
        /// <summary>
        /// Power-up item
        /// </summary>
        PowerUp,
        
        /// <summary>
        /// Non-player character
        /// </summary>
        NPC,
        
        /// <summary>
        /// Exit point
        /// </summary>
        Exit,
        
        /// <summary>
        /// Checkpoint
        /// </summary>
        Checkpoint,
        
        /// <summary>
        /// Obstacle
        /// </summary>
        Obstacle,
        
        /// <summary>
        /// Trigger zone
        /// </summary>
        Trigger
    }
}
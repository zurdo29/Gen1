using System;
using System.Collections.Generic;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Factory for creating entity instances based on type
    /// </summary>
    public static class EntityFactory
    {
        /// <summary>
        /// Creates an entity of the specified type
        /// </summary>
        /// <param name="entityType">Type of entity to create</param>
        /// <param name="properties">Optional properties to set on the entity</param>
        /// <returns>New entity instance</returns>
        public static Entity CreateEntity(EntityType entityType, Dictionary<string, object>? properties = null)
        {
            Entity entity = entityType switch
            {
                EntityType.Player => new PlayerEntity(),
                EntityType.Enemy => new EnemyEntity(),
                EntityType.Item => new ItemEntity(),
                EntityType.PowerUp => new PowerUpEntity(),
                EntityType.NPC => new NPCEntity(),
                EntityType.Exit => new ExitEntity(),
                EntityType.Checkpoint => new CheckpointEntity(),
                EntityType.Obstacle => new ObstacleEntity(),
                EntityType.Trigger => new TriggerEntity(),
                _ => throw new ArgumentException($"Unknown entity type: {entityType}")
            };
            
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    entity.Properties[kvp.Key] = kvp.Value;
                }
            }
            
            return entity;
        }
        
        /// <summary>
        /// Gets all available entity types
        /// </summary>
        /// <returns>Array of all entity types</returns>
        public static EntityType[] GetAllEntityTypes()
        {
            return (EntityType[])Enum.GetValues(typeof(EntityType));
        }
        
        /// <summary>
        /// Checks if an entity type is valid
        /// </summary>
        /// <param name="entityType">Entity type to check</param>
        /// <returns>True if valid</returns>
        public static bool IsValidEntityType(EntityType entityType)
        {
            return Enum.IsDefined(typeof(EntityType), entityType);
        }
    }
}
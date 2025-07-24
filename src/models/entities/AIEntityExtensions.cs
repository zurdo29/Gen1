using System;
using System.Linq;

namespace ProceduralMiniGameGenerator.Models.Entities
{
    /// <summary>
    /// Extension methods for entities to work with AI-generated content
    /// </summary>
    public static class AIEntityExtensions
    {
        /// <summary>
        /// Gets the AI-generated description for this entity
        /// </summary>
        /// <param name="entity">Entity to get description for</param>
        /// <returns>AI-generated description or null if not available</returns>
        public static string GetAIDescription(this Entity entity)
        {
            if (entity.Properties.TryGetValue("Description", out var description) &&
                entity.Properties.ContainsKey("AIGenerated"))
            {
                return description?.ToString();
            }
            return null;
        }

        /// <summary>
        /// Gets the AI-generated dialogue for this entity
        /// </summary>
        /// <param name="entity">Entity to get dialogue for</param>
        /// <returns>Array of dialogue lines or null if not available</returns>
        public static string[] GetAIDialogue(this Entity entity)
        {
            if (entity.Properties.TryGetValue("Dialogue", out var dialogue) &&
                entity.Properties.ContainsKey("AIGeneratedDialogue"))
            {
                return dialogue as string[];
            }
            return null;
        }

        /// <summary>
        /// Gets a random dialogue line from AI-generated dialogue
        /// </summary>
        /// <param name="entity">Entity to get dialogue from</param>
        /// <returns>Random dialogue line or null if not available</returns>
        public static string GetRandomDialogueLine(this Entity entity)
        {
            var dialogue = entity.GetAIDialogue();
            if (dialogue != null && dialogue.Length > 0)
            {
                var random = new Random();
                return dialogue[random.Next(dialogue.Length)];
            }
            return null;
        }

        /// <summary>
        /// Checks if this entity has AI-generated content
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>True if entity has AI-generated content</returns>
        public static bool HasAIContent(this Entity entity)
        {
            return entity.Properties.ContainsKey("AIGenerated") || 
                   entity.Properties.ContainsKey("AIGeneratedDialogue");
        }

        /// <summary>
        /// Gets the number of AI-generated dialogue lines
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>Number of dialogue lines or 0 if none</returns>
        public static int GetDialogueLineCount(this Entity entity)
        {
            if (entity.Properties.TryGetValue("DialogueCount", out var count))
            {
                if (count is int intCount)
                    return intCount;
                if (int.TryParse(count?.ToString(), out var parsedCount))
                    return parsedCount;
            }
            return 0;
        }

        /// <summary>
        /// Sets AI-generated description for this entity
        /// </summary>
        /// <param name="entity">Entity to set description for</param>
        /// <param name="description">Description to set</param>
        public static void SetAIDescription(this Entity entity, string description)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                entity.Properties["Description"] = description;
                entity.Properties["AIGenerated"] = true;
            }
        }

        /// <summary>
        /// Sets AI-generated dialogue for this entity
        /// </summary>
        /// <param name="entity">Entity to set dialogue for</param>
        /// <param name="dialogue">Dialogue lines to set</param>
        public static void SetAIDialogue(this Entity entity, string[] dialogue)
        {
            if (dialogue != null && dialogue.Length > 0)
            {
                entity.Properties["Dialogue"] = dialogue;
                entity.Properties["DialogueCount"] = dialogue.Length;
                entity.Properties["AIGeneratedDialogue"] = true;
            }
        }

        /// <summary>
        /// Gets a summary of AI-generated content for this entity
        /// </summary>
        /// <param name="entity">Entity to get summary for</param>
        /// <returns>Summary of AI content</returns>
        public static AIContentSummary GetAIContentSummary(this Entity entity)
        {
            return new AIContentSummary
            {
                HasDescription = !string.IsNullOrWhiteSpace(entity.GetAIDescription()),
                HasDialogue = entity.GetAIDialogue() != null,
                DialogueLineCount = entity.GetDialogueLineCount(),
                EntityType = entity.Type
            };
        }
    }

    /// <summary>
    /// Summary of AI-generated content for an entity
    /// </summary>
    public class AIContentSummary
    {
        public bool HasDescription { get; set; }
        public bool HasDialogue { get; set; }
        public int DialogueLineCount { get; set; }
        public EntityType EntityType { get; set; }
        public bool HasAnyContent => HasDescription || HasDialogue;
    }
}
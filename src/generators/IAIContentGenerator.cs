using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Interface for AI-powered content generation
    /// </summary>
    public interface IAIContentGenerator
    {
        /// <summary>
        /// Generates a description for an item
        /// </summary>
        /// <param name="type">Type of item</param>
        /// <param name="theme">Visual theme context</param>
        /// <returns>Generated description</returns>
        string GenerateItemDescription(EntityType type, VisualTheme theme);
        
        /// <summary>
        /// Generates dialogue lines for NPCs
        /// </summary>
        /// <param name="type">Type of NPC</param>
        /// <param name="lineCount">Number of dialogue lines to generate</param>
        /// <returns>Array of dialogue lines</returns>
        string[] GenerateNPCDialogue(EntityType type, int lineCount);
        
        /// <summary>
        /// Generates a name for a level
        /// </summary>
        /// <param name="level">Level to generate name for</param>
        /// <param name="theme">Visual theme context</param>
        /// <returns>Generated level name</returns>
        string GenerateLevelName(Level level, VisualTheme theme);
        
        /// <summary>
        /// Checks if AI services are available
        /// </summary>
        /// <returns>True if AI services can be used</returns>
        bool IsAvailable();
    }
}
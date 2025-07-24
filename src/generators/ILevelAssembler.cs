using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Interface for assembling complete levels from terrain and entities
    /// </summary>
    public interface ILevelAssembler
    {
        /// <summary>
        /// Assembles a complete level from terrain and entities
        /// </summary>
        /// <param name="terrain">Generated terrain</param>
        /// <param name="entities">Placed entities</param>
        /// <param name="config">Generation configuration</param>
        /// <returns>Assembled level</returns>
        Level AssembleLevel(TileMap terrain, List<Entity> entities, GenerationConfig config);
        
        /// <summary>
        /// Applies a visual theme to a level
        /// </summary>
        /// <param name="level">Level to apply theme to</param>
        /// <param name="theme">Visual theme to apply</param>
        void ApplyVisualTheme(Level level, VisualTheme theme);
    }
}
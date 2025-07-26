using ProceduralMiniGameGenerator.Models;
using GenerationConfig = ProceduralMiniGameGenerator.Models.GenerationConfig;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Interface for deep cloning generation configurations
    /// </summary>
    public interface IConfigurationCloningService
    {
        /// <summary>
        /// Creates a deep clone of the specified configuration
        /// </summary>
        /// <param name="original">The configuration to clone</param>
        /// <returns>A deep clone of the original configuration</returns>
        GenerationConfig CloneConfiguration(GenerationConfig original);
    }

    /// <summary>
    /// Alternative implementation using copy constructors for better performance
    /// </summary>
    public class OptimizedConfigurationCloningService : IConfigurationCloningService
    {
        public GenerationConfig CloneConfiguration(GenerationConfig original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            return new GenerationConfig
            {
                Seed = original.Seed,
                Width = original.Width,
                Height = original.Height,
                GenerationAlgorithm = original.GenerationAlgorithm,
                VisualTheme = CloneVisualTheme(original.VisualTheme),
                Gameplay = CloneGameplay(original.Gameplay)
                // Add other properties as needed
            };
        }

        private VisualThemeConfig? CloneVisualTheme(VisualThemeConfig? original)
        {
            if (original is null) return null;

            return new VisualThemeConfig
            {
                ThemeName = original.ThemeName
                // Add other properties as needed
            };
        }

        private GameplayConfig? CloneGameplay(GameplayConfig? original)
        {
            if (original is null) return null;
            
            return new GameplayConfig
            {
                Difficulty = original.Difficulty,
                PlayerSpeed = original.PlayerSpeed,
                TimeLimit = original.TimeLimit
                // Add other properties as needed
            };
        }
    }
}
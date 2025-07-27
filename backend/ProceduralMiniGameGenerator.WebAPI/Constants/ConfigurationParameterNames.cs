namespace ProceduralMiniGameGenerator.WebAPI.Constants
{
    /// <summary>
    /// Constants for configuration parameter names used in batch generation
    /// </summary>
    public static class ConfigurationParameterNames
    {
        public const string Seed = "seed";
        public const string Width = "width";
        public const string Height = "height";
        public const string GenerationAlgorithm = "generationAlgorithm";
        
        // Visual Theme Parameters
        public const string VisualThemeName = "visualTheme.themeName";
        
        // Gameplay Parameters
        public const string GameplayDifficulty = "gameplay.difficulty";
        public const string GameplayPlayerSpeed = "gameplay.playerSpeed";
        public const string GameplayTimeLimit = "gameplay.timeLimit";
    }
}
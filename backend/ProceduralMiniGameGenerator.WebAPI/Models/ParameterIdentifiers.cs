namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Strongly-typed parameter identifiers for configuration variations
    /// </summary>
    public static class ParameterIdentifiers
    {
        public const string Seed = "seed";
        public const string Width = "width";
        public const string Height = "height";
        public const string GenerationAlgorithm = "generationAlgorithm";
        public const string VisualThemeName = "visualTheme.themeName";
        public const string GameplayDifficulty = "gameplay.difficulty";
        public const string GameplayPlayerSpeed = "gameplay.playerSpeed";
        public const string GameplayTimeLimit = "gameplay.timeLimit";

        /// <summary>
        /// Gets all valid parameter identifiers
        /// </summary>
        public static readonly HashSet<string> ValidParameters = new()
        {
            Seed, Width, Height, GenerationAlgorithm,
            VisualThemeName, GameplayDifficulty, GameplayPlayerSpeed, GameplayTimeLimit
        };

        /// <summary>
        /// Validates if a parameter identifier is supported
        /// </summary>
        public static bool IsValid(string parameter) => ValidParameters.Contains(parameter);
    }
}
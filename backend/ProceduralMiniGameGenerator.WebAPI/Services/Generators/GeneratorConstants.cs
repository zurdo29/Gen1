namespace ProceduralMiniGameGenerator.WebAPI.Services.Generators
{
    /// <summary>
    /// Constants used by terrain generators
    /// </summary>
    public static class GeneratorConstants
    {
        /// <summary>
        /// Perlin noise generator constants
        /// </summary>
        public static class Perlin
        {
            public const double DefaultScale = 0.1;
            public const int DefaultOctaves = 4;
            public const double DefaultPersistence = 0.5;
            public const double DefaultLacunarity = 2.0;
            public const double WallThreshold = 0.3;
            public const double WaterThreshold = -0.2;
        }

        /// <summary>
        /// Cellular automata generator constants
        /// </summary>
        public static class Cellular
        {
            public const double InitialWallProbability = 0.45;
            public const int DefaultIterations = 5;
            public const int DefaultWallThreshold = 4;
            public const int MaxIterations = 20;
            public const int MaxWallThreshold = 8;
        }

        /// <summary>
        /// Maze generator constants
        /// </summary>
        public static class Maze
        {
            public const double DefaultConnectionProbability = 0.5;
            public const int DefaultPathWidth = 1;
            public const int MaxPathWidth = 5;
        }

        /// <summary>
        /// Room generator constants
        /// </summary>
        public static class Room
        {
            public const int DefaultRoomCount = 6;
            public const int MinRoomSize = 5;
            public const int MaxRoomSize = 15;
            public const int MaxRoomCount = 20;
            public const int MinRoomSizeLimit = 3;
            public const int MaxRoomSizeLimit = 50;
        }
    }
}
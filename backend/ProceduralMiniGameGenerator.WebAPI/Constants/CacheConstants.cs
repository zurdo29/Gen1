namespace ProceduralMiniGameGenerator.WebAPI.Constants
{
    /// <summary>
    /// Constants for caching operations
    /// </summary>
    public static class CacheConstants
    {
        /// <summary>
        /// Cache expiration timeouts
        /// </summary>
        public static class Timeouts
        {
            public static readonly TimeSpan LevelCache = TimeSpan.FromMinutes(10);
            public static readonly TimeSpan ShortJobCache = TimeSpan.FromHours(1);
            public static readonly TimeSpan LongJobCache = TimeSpan.FromHours(2);
            public static readonly TimeSpan ConfigPresets = TimeSpan.FromHours(24);
            public static readonly TimeSpan SharedConfigs = TimeSpan.FromDays(31);
            public static readonly TimeSpan RateLimit = TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Cache key prefixes
        /// </summary>
        public static class KeyPrefixes
        {
            public const string Level = "level:";
            public const string Job = "job:";
            public const string Config = "config:";
            public const string Preset = "preset:";
        }
    }
}
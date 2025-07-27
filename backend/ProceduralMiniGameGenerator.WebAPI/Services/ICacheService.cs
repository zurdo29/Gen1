namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for centralized cache operations
    /// </summary>
    public interface ICacheService
    {
        T? Get<T>(string key) where T : class;
        void Set<T>(string key, T value, TimeSpan expiration) where T : class;
        bool TryGet<T>(string key, out T? value) where T : class;
        void Remove(string key);
        string GenerateLevelCacheKey(object config);
        string GenerateJobCacheKey(string jobId);
    }
}
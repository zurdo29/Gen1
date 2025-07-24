using System.Reflection;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Interface for loading and managing plugins dynamically
    /// </summary>
    public interface IPluginLoader
    {
        /// <summary>
        /// Loads plugins of the specified type from the given assembly
        /// </summary>
        /// <typeparam name="T">Plugin interface type</typeparam>
        /// <param name="assembly">Assembly to load plugins from</param>
        /// <returns>Collection of loaded plugin instances</returns>
        Task<IEnumerable<T>> LoadPluginsAsync<T>(Assembly assembly) where T : class;
        
        /// <summary>
        /// Loads plugins of the specified type from all assemblies in the specified directory
        /// </summary>
        /// <typeparam name="T">Plugin interface type</typeparam>
        /// <param name="pluginDirectory">Directory containing plugin assemblies</param>
        /// <returns>Collection of loaded plugin instances</returns>
        Task<IEnumerable<T>> LoadPluginsFromDirectoryAsync<T>(string pluginDirectory) where T : class;
        
        /// <summary>
        /// Registers a plugin instance with the service provider
        /// </summary>
        /// <typeparam name="T">Plugin interface type</typeparam>
        /// <param name="plugin">Plugin instance to register</param>
        /// <param name="name">Optional name for the plugin</param>
        Task RegisterPluginAsync<T>(T plugin, string? name = null) where T : class;
        
        /// <summary>
        /// Gets all registered plugins of the specified type
        /// </summary>
        /// <typeparam name="T">Plugin interface type</typeparam>
        /// <returns>Collection of registered plugin instances</returns>
        IEnumerable<T> GetPlugins<T>() where T : class;
        
        /// <summary>
        /// Gets a specific plugin by name and type
        /// </summary>
        /// <typeparam name="T">Plugin interface type</typeparam>
        /// <param name="name">Plugin name</param>
        /// <returns>Plugin instance if found, null otherwise</returns>
        T? GetPlugin<T>(string name) where T : class;
        
        /// <summary>
        /// Discovers and loads plugins from the default plugin directory
        /// </summary>
        /// <returns>Number of plugins loaded</returns>
        Task<int> DiscoverAndLoadPluginsAsync();
        
        /// <summary>
        /// Gets metadata about loaded plugins
        /// </summary>
        /// <returns>Collection of plugin metadata</returns>
        IEnumerable<PluginMetadata> GetPluginMetadata();
    }
    
    /// <summary>
    /// Metadata information about a loaded plugin
    /// </summary>
    public class PluginMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Type InterfaceType { get; set; } = typeof(object);
        public Type ImplementationType { get; set; } = typeof(object);
        public string AssemblyPath { get; set; } = string.Empty;
        public DateTime LoadedAt { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
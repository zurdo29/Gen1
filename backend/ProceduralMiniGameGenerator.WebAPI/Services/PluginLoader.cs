using System.Reflection;
using System.Collections.Concurrent;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Core;
using IEntityPlacer = ProceduralMiniGameGenerator.Generators.IEntityPlacer;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Implementation of plugin loading and management system
    /// </summary>
    public class PluginLoader : IPluginLoader
    {
        private readonly ILoggerService _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> _plugins;
        private readonly ConcurrentDictionary<string, PluginMetadata> _pluginMetadata;
        private readonly string _pluginDirectory;
        
        public PluginLoader(ILoggerService logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _plugins = new ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>();
            _pluginMetadata = new ConcurrentDictionary<string, PluginMetadata>();
            _pluginDirectory = _configuration["PluginSettings:Directory"] ?? "plugins";
        }
        
        public async Task<IEnumerable<T>> LoadPluginsAsync<T>(Assembly assembly) where T : class
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var pluginType = typeof(T);
            var loadedPlugins = new List<T>();
            
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    $"Loading plugins of type {pluginType.Name} from assembly {assembly.FullName}");
                
                var types = assembly.GetTypes()
                    .Where(t => !t.IsInterface && !t.IsAbstract && pluginType.IsAssignableFrom(t))
                    .ToList();
                
                foreach (var type in types)
                {
                    try
                    {
                        var plugin = await CreatePluginInstance<T>(type, assembly);
                        if (plugin != null)
                        {
                            loadedPlugins.Add(plugin);
                            await RegisterPluginAsync(plugin, type.Name);
                            
                            await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                                $"Successfully loaded plugin {type.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogErrorAsync(ex, $"Failed to load plugin {type.Name}", 
                            new { PluginType = type.FullName, Assembly = assembly.FullName });
                    }
                }
                
                stopwatch.Stop();
                await _logger.LogPerformanceAsync($"LoadPlugins<{pluginType.Name}>", stopwatch.Elapsed, 
                    new { PluginCount = loadedPlugins.Count, Assembly = assembly.FullName });
                
                return loadedPlugins;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _logger.LogErrorAsync(ex, $"Failed to load plugins from assembly {assembly.FullName}");
                return Enumerable.Empty<T>();
            }
        }
        
        public async Task<IEnumerable<T>> LoadPluginsFromDirectoryAsync<T>(string pluginDirectory) where T : class
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var allPlugins = new List<T>();
            
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    $"Loading plugins from directory: {pluginDirectory}");
                
                if (!Directory.Exists(pluginDirectory))
                {
                    await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning, 
                        $"Plugin directory does not exist: {pluginDirectory}");
                    return Enumerable.Empty<T>();
                }
                
                var assemblyFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
                
                foreach (var assemblyFile in assemblyFiles)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(assemblyFile);
                        var plugins = await LoadPluginsAsync<T>(assembly);
                        allPlugins.AddRange(plugins);
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogErrorAsync(ex, $"Failed to load assembly {assemblyFile}");
                    }
                }
                
                stopwatch.Stop();
                await _logger.LogPerformanceAsync($"LoadPluginsFromDirectory<{typeof(T).Name}>", stopwatch.Elapsed, 
                    new { PluginCount = allPlugins.Count, Directory = pluginDirectory });
                
                return allPlugins;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _logger.LogErrorAsync(ex, $"Failed to load plugins from directory {pluginDirectory}");
                return Enumerable.Empty<T>();
            }
        }
        
        public async Task RegisterPluginAsync<T>(T plugin, string? name = null) where T : class
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));
            
            var pluginType = typeof(T);
            var pluginName = name ?? plugin.GetType().Name;
            
            try
            {
                var typePlugins = _plugins.GetOrAdd(pluginType, _ => new ConcurrentDictionary<string, object>());
                
                if (typePlugins.TryAdd(pluginName, plugin))
                {
                    // Create metadata for the plugin
                    var metadata = CreatePluginMetadata(plugin, pluginName);
                    _pluginMetadata.TryAdd($"{pluginType.Name}:{pluginName}", metadata);
                    
                    await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                        $"Registered plugin {pluginName} of type {pluginType.Name}");
                }
                else
                {
                    await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Warning, 
                        $"Plugin {pluginName} of type {pluginType.Name} is already registered");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, $"Failed to register plugin {pluginName}");
            }
        }
        
        public IEnumerable<T> GetPlugins<T>() where T : class
        {
            var pluginType = typeof(T);
            
            if (_plugins.TryGetValue(pluginType, out var typePlugins))
            {
                return typePlugins.Values.Cast<T>();
            }
            
            return Enumerable.Empty<T>();
        }
        
        public T? GetPlugin<T>(string name) where T : class
        {
            var pluginType = typeof(T);
            
            if (_plugins.TryGetValue(pluginType, out var typePlugins) && 
                typePlugins.TryGetValue(name, out var plugin))
            {
                return plugin as T;
            }
            
            return null;
        }
        
        public async Task<int> DiscoverAndLoadPluginsAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var totalPluginsLoaded = 0;
            
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    "Starting plugin discovery and loading");
                
                // Load built-in plugins first
                totalPluginsLoaded += await LoadBuiltInPluginsAsync();
                
                // Load external plugins from directory
                if (Directory.Exists(_pluginDirectory))
                {
                    totalPluginsLoaded += await LoadExternalPluginsAsync();
                }
                else
                {
                    await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                        $"Plugin directory {_pluginDirectory} does not exist, skipping external plugin loading");
                }
                
                stopwatch.Stop();
                await _logger.LogPerformanceAsync("DiscoverAndLoadPlugins", stopwatch.Elapsed, 
                    new { TotalPluginsLoaded = totalPluginsLoaded });
                
                return totalPluginsLoaded;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _logger.LogErrorAsync(ex, "Failed during plugin discovery and loading");
                return totalPluginsLoaded;
            }
        }
        
        public IEnumerable<PluginMetadata> GetPluginMetadata()
        {
            return _pluginMetadata.Values.ToList();
        }
        
        private async Task<int> LoadBuiltInPluginsAsync()
        {
            var pluginsLoaded = 0;
            
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    "Loading built-in plugins");
                
                // Load the main assembly containing the generators
                var mainAssembly = Assembly.LoadFrom("ProceduralMiniGameGenerator.dll");
                
                // Load terrain generators
                var terrainGenerators = await LoadPluginsAsync<ITerrainGenerator>(mainAssembly);
                pluginsLoaded += terrainGenerators.Count();
                
                // Load entity placers
                var entityPlacers = await LoadPluginsAsync<IEntityPlacer>(mainAssembly);
                pluginsLoaded += entityPlacers.Count();
                
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    $"Loaded {pluginsLoaded} built-in plugins");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Failed to load built-in plugins");
            }
            
            return pluginsLoaded;
        }
        
        private async Task<int> LoadExternalPluginsAsync()
        {
            var pluginsLoaded = 0;
            
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    $"Loading external plugins from {_pluginDirectory}");
                
                // Load terrain generators from external assemblies
                var terrainGenerators = await LoadPluginsFromDirectoryAsync<ITerrainGenerator>(_pluginDirectory);
                pluginsLoaded += terrainGenerators.Count();
                
                // Load entity placers from external assemblies
                var entityPlacers = await LoadPluginsFromDirectoryAsync<IEntityPlacer>(_pluginDirectory);
                pluginsLoaded += entityPlacers.Count();
                
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                    $"Loaded {pluginsLoaded} external plugins");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Failed to load external plugins");
            }
            
            return pluginsLoaded;
        }
        
        private async Task<T?> CreatePluginInstance<T>(Type pluginType, Assembly assembly) where T : class
        {
            try
            {
                // Try to create instance with dependency injection support
                var constructors = pluginType.GetConstructors()
                    .OrderByDescending(c => c.GetParameters().Length);
                
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    var args = new object[parameters.Length];
                    var canCreate = true;
                    
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramType = parameters[i].ParameterType;
                        
                        // Try to resolve common dependencies
                        if (paramType == typeof(IRandomGenerator))
                        {
                            args[i] = new RandomGenerator();
                        }
                        else if (paramType == typeof(ILoggerService))
                        {
                            args[i] = _logger;
                        }
                        else
                        {
                            // Can't resolve this parameter, try next constructor
                            canCreate = false;
                            break;
                        }
                    }
                    
                    if (canCreate)
                    {
                        var instance = Activator.CreateInstance(pluginType, args);
                        return instance as T;
                    }
                }
                
                // If no constructor with dependencies worked, try parameterless constructor
                var parameterlessInstance = Activator.CreateInstance(pluginType);
                return parameterlessInstance as T;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, $"Failed to create instance of plugin {pluginType.Name}");
                return null;
            }
        }
        
        private PluginMetadata CreatePluginMetadata(object plugin, string name)
        {
            var pluginType = plugin.GetType();
            var assembly = pluginType.Assembly;
            
            var metadata = new PluginMetadata
            {
                Name = name,
                Version = assembly.GetName().Version?.ToString() ?? "Unknown",
                Description = GetPluginDescription(plugin),
                InterfaceType = GetPluginInterfaceType(plugin),
                ImplementationType = pluginType,
                AssemblyPath = assembly.Location,
                LoadedAt = DateTime.UtcNow,
                Properties = GetPluginProperties(plugin)
            };
            
            return metadata;
        }
        
        private string GetPluginDescription(object plugin)
        {
            // Try to get description from plugin if it has a method for it
            if (plugin is ITerrainGenerator terrainGen)
            {
                return $"Terrain generator using {terrainGen.GetAlgorithmName()} algorithm";
            }
            
            if (plugin is IEntityPlacer)
            {
                var strategyName = plugin.GetType().Name.Replace("EntityPlacer", "").Replace("Placer", "");
                return $"Entity placement strategy: {strategyName}";
            }
            
            if (plugin is ProceduralMiniGameGenerator.Core.IEntityPlacer coreEntityPlacer)
            {
                return $"Entity placement strategy: {coreEntityPlacer.GetStrategyName()}";
            }
            
            return plugin.GetType().Name;
        }
        
        private Type GetPluginInterfaceType(object plugin)
        {
            var interfaces = plugin.GetType().GetInterfaces();
            
            // Return the most specific interface
            if (interfaces.Contains(typeof(ITerrainGenerator)))
                return typeof(ITerrainGenerator);
            
            if (interfaces.Contains(typeof(IEntityPlacer)))
                return typeof(IEntityPlacer);
            
            if (interfaces.Contains(typeof(ProceduralMiniGameGenerator.Core.IEntityPlacer)))
                return typeof(ProceduralMiniGameGenerator.Core.IEntityPlacer);
            
            return interfaces.FirstOrDefault() ?? typeof(object);
        }
        
        private Dictionary<string, object> GetPluginProperties(object plugin)
        {
            var properties = new Dictionary<string, object>();
            
            try
            {
                if (plugin is ITerrainGenerator terrainGen)
                {
                    properties["AlgorithmName"] = terrainGen.GetAlgorithmName();
                    properties["DefaultParameters"] = terrainGen.GetDefaultParameters();
                }
                
                properties["TypeName"] = plugin.GetType().Name;
                properties["AssemblyName"] = plugin.GetType().Assembly.GetName().Name ?? "Unknown";
            }
            catch (Exception ex)
            {
                properties["Error"] = $"Failed to get properties: {ex.Message}";
            }
            
            return properties;
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using ProceduralMiniGameGenerator.Configuration;
using ProceduralMiniGameGenerator.Generators;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Extension methods for registering logging-enhanced services
    /// </summary>
    public static class LoggingServiceExtensions
    {
        /// <summary>
        /// Registers all generation services with logging integration
        /// </summary>
        public static IServiceCollection AddLoggingIntegratedGenerationServices(this IServiceCollection services)
        {
            // Register base services first
            services.AddTransient<ConfigurationParser>();
            services.AddTransient<EntityPlacer>();
            services.AddTransient<LevelAssembler>();
            
            // Register logging-enhanced decorators
            services.AddTransient<IConfigurationParser>(provider =>
            {
                var baseParser = provider.GetRequiredService<ConfigurationParser>();
                var loggerService = provider.GetRequiredService<ILoggerService>();
                return new LoggingConfigurationParser(baseParser, loggerService);
            });
            
            services.AddTransient<IEntityPlacer>(provider =>
            {
                var basePlacer = provider.GetRequiredService<EntityPlacer>();
                var loggerService = provider.GetRequiredService<ILoggerService>();
                return (IEntityPlacer)new LoggingEntityPlacer(basePlacer, loggerService);
            });
            
            services.AddTransient<ILevelAssembler>(provider =>
            {
                var baseAssembler = provider.GetRequiredService<LevelAssembler>();
                var loggerService = provider.GetRequiredService<ILoggerService>();
                return new LoggingLevelAssembler(baseAssembler, loggerService);
            });
            
            return services;
        }
        
        /// <summary>
        /// Registers terrain generators with logging integration
        /// </summary>
        public static IServiceCollection AddLoggingIntegratedTerrainGenerators(this IServiceCollection services)
        {
            // Register base terrain generators
            services.AddTransient<PerlinNoiseGenerator>();
            services.AddTransient<CellularAutomataGenerator>();
            services.AddTransient<MazeGenerator>();
            
            // Register logging-enhanced terrain generator factory
            services.AddTransient<ITerrainGeneratorFactory>(provider =>
            {
                var loggerService = provider.GetRequiredService<ILoggerService>();
                return new LoggingTerrainGeneratorFactory(provider, loggerService);
            });
            
            return services;
        }
    }
    
    /// <summary>
    /// Factory for creating logging-enhanced terrain generators
    /// </summary>
    public interface ITerrainGeneratorFactory
    {
        ITerrainGenerator CreateGenerator(string algorithmName);
        IEnumerable<ITerrainGenerator> GetAllGenerators();
    }
    
    /// <summary>
    /// Implementation of terrain generator factory with logging integration
    /// </summary>
    public class LoggingTerrainGeneratorFactory : ITerrainGeneratorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerService _loggerService;
        private readonly Dictionary<string, Type> _generatorTypes;
        
        public LoggingTerrainGeneratorFactory(IServiceProvider serviceProvider, ILoggerService loggerService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
            
            _generatorTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "perlin", typeof(PerlinNoiseGenerator) },
                { "cellular", typeof(CellularAutomataGenerator) },
                { "maze", typeof(MazeGenerator) }
            };
        }
        
        public ITerrainGenerator CreateGenerator(string algorithmName)
        {
            if (string.IsNullOrWhiteSpace(algorithmName))
                throw new ArgumentException("Algorithm name cannot be null or empty", nameof(algorithmName));
            
            if (!_generatorTypes.TryGetValue(algorithmName, out var generatorType))
                throw new ArgumentException($"Unknown terrain generation algorithm: {algorithmName}");
            
            var baseGenerator = (ITerrainGenerator)_serviceProvider.GetRequiredService(generatorType);
            return new LoggingTerrainGeneratorDecorator(baseGenerator, _loggerService);
        }
        
        public IEnumerable<ITerrainGenerator> GetAllGenerators()
        {
            foreach (var kvp in _generatorTypes)
            {
                yield return CreateGenerator(kvp.Key);
            }
        }
    }
}
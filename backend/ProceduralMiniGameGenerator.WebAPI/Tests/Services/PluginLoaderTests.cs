using Microsoft.Extensions.Configuration;
using Moq;
using ProceduralMiniGameGenerator.WebAPI.Services;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Models;
using System.Reflection;
using Xunit;
using System.Collections.Generic;
using System.Numerics;
using IEntityPlacer = ProceduralMiniGameGenerator.Generators.IEntityPlacer;

namespace ProceduralMiniGameGenerator.WebAPI.Tests.Services
{
    /// <summary>
    /// Unit tests for PluginLoader functionality
    /// </summary>
    public class PluginLoaderTests
    {
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly PluginLoader _pluginLoader;
        
        public PluginLoaderTests()
        {
            _mockLogger = new Mock<ILoggerService>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup default configuration values using IConfigurationSection
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(x => x.Value).Returns("test-plugins");
            _mockConfiguration.Setup(x => x.GetSection("PluginSettings:Directory")).Returns(mockSection.Object);
            
            _pluginLoader = new PluginLoader(_mockLogger.Object, _mockConfiguration.Object);
        }
        
        [Fact]
        public async Task RegisterPluginAsync_WithValidPlugin_RegistersSuccessfully()
        {
            // Arrange
            var plugin = new TestTerrainGenerator();
            var pluginName = "TestGenerator";
            
            // Act
            await _pluginLoader.RegisterPluginAsync<ITerrainGenerator>(plugin, pluginName);
            
            // Assert
            var registeredPlugin = _pluginLoader.GetPlugin<ITerrainGenerator>(pluginName);
            Assert.NotNull(registeredPlugin);
            Assert.Same(plugin, registeredPlugin);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.LogAsync(
                    Microsoft.Extensions.Logging.LogLevel.Information,
                    It.Is<string>(s => s.Contains("Registered plugin") && s.Contains(pluginName)),
                    It.IsAny<object>()),
                Times.Once);
        }
        
        [Fact]
        public async Task RegisterPluginAsync_WithNullPlugin_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _pluginLoader.RegisterPluginAsync<ITerrainGenerator>(null!, "test"));
        }
        
        [Fact]
        public async Task RegisterPluginAsync_WithDuplicateName_LogsWarning()
        {
            // Arrange
            var plugin1 = new TestTerrainGenerator();
            var plugin2 = new TestTerrainGenerator();
            var pluginName = "DuplicateGenerator";
            
            // Act
            await _pluginLoader.RegisterPluginAsync<ITerrainGenerator>(plugin1, pluginName);
            await _pluginLoader.RegisterPluginAsync<ITerrainGenerator>(plugin2, pluginName);
            
            // Assert
            var registeredPlugin = _pluginLoader.GetPlugin<ITerrainGenerator>(pluginName);
            Assert.Same(plugin1, registeredPlugin); // First plugin should remain
            
            // Verify warning was logged
            _mockLogger.Verify(
                x => x.LogAsync(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    It.Is<string>(s => s.Contains("already registered")),
                    It.IsAny<object>()),
                Times.Once);
        }
        
        [Fact]
        public void GetPlugins_WithRegisteredPlugins_ReturnsAllPlugins()
        {
            // Arrange
            var plugin1 = new TestTerrainGenerator();
            var plugin2 = new TestTerrainGenerator();
            
            _pluginLoader.RegisterPluginAsync<ITerrainGenerator>(plugin1, "Generator1").Wait();
            _pluginLoader.RegisterPluginAsync<ITerrainGenerator>(plugin2, "Generator2").Wait();
            
            // Act
            var plugins = _pluginLoader.GetPlugins<ITerrainGenerator>();
            
            // Assert
            Assert.Equal(2, plugins.Count());
            Assert.Contains(plugin1, plugins);
            Assert.Contains(plugin2, plugins);
        }
        
        [Fact]
        public void GetPlugins_WithNoRegisteredPlugins_ReturnsEmpty()
        {
            // Act
            var plugins = _pluginLoader.GetPlugins<ITerrainGenerator>();
            
            // Assert
            Assert.Empty(plugins);
        }
        
        [Fact]
        public void GetPlugin_WithExistingPlugin_ReturnsPlugin()
        {
            // Arrange
            var plugin = new TestTerrainGenerator();
            var pluginName = "TestGenerator";
            
            _pluginLoader.RegisterPluginAsync<ITerrainGenerator>(plugin, pluginName).Wait();
            
            // Act
            var retrievedPlugin = _pluginLoader.GetPlugin<ITerrainGenerator>(pluginName);
            
            // Assert
            Assert.NotNull(retrievedPlugin);
            Assert.Same(plugin, retrievedPlugin);
        }
        
        [Fact]
        public void GetPlugin_WithNonExistentPlugin_ReturnsNull()
        {
            // Act
            var plugin = _pluginLoader.GetPlugin<ITerrainGenerator>("NonExistent");
            
            // Assert
            Assert.Null(plugin);
        }
        
        [Fact]
        public async Task LoadPluginsAsync_WithValidAssembly_LoadsPlugins()
        {
            // Arrange
            var assembly = Assembly.GetExecutingAssembly();
            
            // Act
            var plugins = await _pluginLoader.LoadPluginsAsync<ITestPlugin>(assembly);
            
            // Assert
            Assert.NotEmpty(plugins);
            
            // Verify performance logging
            _mockLogger.Verify(
                x => x.LogPerformanceAsync(
                    It.Is<string>(s => s.Contains("LoadPlugins")),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<object>()),
                Times.Once);
        }
        
        [Fact]
        public void GetPluginMetadata_WithRegisteredPlugins_ReturnsMetadata()
        {
            // Arrange
            var plugin = new TestTerrainGenerator();
            _pluginLoader.RegisterPluginAsync<ITerrainGenerator>(plugin, "TestGenerator").Wait();
            
            // Act
            var metadata = _pluginLoader.GetPluginMetadata();
            
            // Assert
            Assert.NotEmpty(metadata);
            var pluginMetadata = metadata.First();
            Assert.Equal("TestGenerator", pluginMetadata.Name);
            Assert.Equal(typeof(ITerrainGenerator), pluginMetadata.InterfaceType);
            Assert.Equal(typeof(TestTerrainGenerator), pluginMetadata.ImplementationType);
        }
        
        [Fact]
        public async Task DiscoverAndLoadPluginsAsync_WithValidConfiguration_LoadsPlugins()
        {
            // Arrange
            // Create a temporary directory structure for testing
            var tempDir = Path.Combine(Path.GetTempPath(), "test-plugins-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var mockTempSection = new Mock<IConfigurationSection>();
                mockTempSection.Setup(x => x.Value).Returns(tempDir);
                _mockConfiguration.Setup(x => x.GetSection("PluginSettings:Directory")).Returns(mockTempSection.Object);
                
                // Act
                var pluginCount = await _pluginLoader.DiscoverAndLoadPluginsAsync();
                
                // Assert
                Assert.True(pluginCount >= 0); // Should not fail even with empty directory
                
                // Verify performance logging
                _mockLogger.Verify(
                    x => x.LogPerformanceAsync(
                        "DiscoverAndLoadPlugins",
                        It.IsAny<TimeSpan>(),
                        It.IsAny<object>()),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        
        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PluginLoader(null!, _mockConfiguration.Object));
        }
        
        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PluginLoader(_mockLogger.Object, null!));
        }
    }
    
    // Test plugin interfaces and implementations for testing
    public interface ITestPlugin
    {
        string GetName();
    }
    
    public class TestPlugin : ITestPlugin
    {
        public string GetName() => "TestPlugin";
    }
    
    /// <summary>
    /// Test implementation of ITerrainGenerator for unit testing
    /// </summary>
    public class TestTerrainGenerator : ITerrainGenerator
    {
        public string GetAlgorithmName() => "test";
        
        public Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "testParam", "testValue" }
            };
        }
        
        public TileMap GenerateTerrain(GenerationConfig config, int seed)
        {
            var tileMap = new TileMap(10, 10);
            // Simple test implementation
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    tileMap.SetTile(x, y, TileType.Ground);
                }
            }
            return tileMap;
        }
        
        public bool SupportsParameters(Dictionary<string, object> parameters)
        {
            return true;
        }
        
        public List<string> ValidateParameters(Dictionary<string, object> parameters)
        {
            return new List<string>();
        }
    }
    
    /// <summary>
    /// Test implementation of IEntityPlacer for unit testing
    /// </summary>
    public class TestEntityPlacer : IEntityPlacer
    {
        public List<Entity> PlaceEntities(TileMap terrain, GenerationConfig config, int seed)
        {
            return new List<Entity>();
        }
        
        public bool IsValidPosition(Vector2 position, TileMap terrain, List<Entity> existingEntities)
        {
            return true;
        }
        
        public string GetStrategyName() => "test";
        
        public bool SupportsParameters(Dictionary<string, object> parameters)
        {
            return true;
        }
    }
}
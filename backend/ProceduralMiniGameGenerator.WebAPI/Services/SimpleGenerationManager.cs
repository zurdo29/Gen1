using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Configuration;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Simple implementation of IGenerationManager for web API
    /// </summary>
    public class SimpleGenerationManager : IGenerationManager
    {
        private readonly Dictionary<string, ITerrainGenerator> _terrainGenerators;
        private readonly Dictionary<string, IEntityPlacer> _entityPlacers;
        private readonly IConfigurationParser _configurationParser;
        private readonly ILoggerService _loggerService;
        private int _currentSeed;

        public SimpleGenerationManager(
            IConfigurationParser configurationParser,
            ILoggerService loggerService)
        {
            _configurationParser = configurationParser ?? throw new ArgumentNullException(nameof(configurationParser));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
            _terrainGenerators = new Dictionary<string, ITerrainGenerator>();
            _entityPlacers = new Dictionary<string, IEntityPlacer>();
            _currentSeed = Environment.TickCount;
            
            // Register default generators
            RegisterDefaultGenerators();
        }

        /// <summary>
        /// Generates a complete level based on configuration
        /// </summary>
        public Level GenerateLevel(GenerationConfig config)
        {
            try
            {
                // Set seed if provided
                if (config.Seed != 0)
                {
                    SetSeed(config.Seed);
                }

                // Get terrain generator
                if (!_terrainGenerators.TryGetValue(config.GenerationAlgorithm.ToLower(), out var terrainGenerator))
                {
                    throw new ArgumentException($"Unknown generation algorithm: {config.GenerationAlgorithm}");
                }

                // Generate terrain
                var terrain = terrainGenerator.GenerateTerrain(config, _currentSeed);

                // Place entities (use default placer for now)
                var entities = new List<Entity>();
                if (_entityPlacers.TryGetValue("default", out var entityPlacer))
                {
                    entities = entityPlacer.PlaceEntities(terrain, config, _currentSeed);
                }

                // Create level
                var level = new Level
                {
                    Name = $"Generated Level {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Terrain = terrain,
                    Entities = entities,
                    Metadata = new Dictionary<string, object>
                    {
                        ["generatedAt"] = DateTime.UtcNow,
                        ["algorithm"] = config.GenerationAlgorithm,
                        ["seed"] = _currentSeed,
                        ["size"] = $"{config.Width}x{config.Height}"
                    }
                };

                return level;
            }
            catch (Exception ex)
            {
                _loggerService.LogErrorAsync(ex, "Level generation failed", new { Config = config }).Wait();
                throw;
            }
        }

        /// <summary>
        /// Sets the random seed for reproducible generation
        /// </summary>
        public void SetSeed(int seed)
        {
            _currentSeed = seed;
        }

        /// <summary>
        /// Registers a terrain generation algorithm
        /// </summary>
        public void RegisterGenerationAlgorithm(string name, ITerrainGenerator generator)
        {
            _terrainGenerators[name.ToLower()] = generator;
        }

        /// <summary>
        /// Registers an entity placement algorithm
        /// </summary>
        public void RegisterEntityPlacer(string name, IEntityPlacer placer)
        {
            _entityPlacers[name.ToLower()] = placer;
        }

        /// <summary>
        /// Gets the list of available generation algorithms
        /// </summary>
        public List<string> GetAvailableAlgorithms()
        {
            return _terrainGenerators.Keys.ToList();
        }

        /// <summary>
        /// Gets the list of available entity placement strategies
        /// </summary>
        public List<string> GetAvailablePlacementStrategies()
        {
            return _entityPlacers.Keys.ToList();
        }

        /// <summary>
        /// Validates that a configuration can be used for generation
        /// </summary>
        public ValidationResult ValidateGenerationConfig(GenerationConfig config)
        {
            var isValid = _configurationParser.ValidateConfig(config, out var errors);
            
            if (isValid)
            {
                return ValidationResult.Success();
            }
            else
            {
                return ValidationResult.Failure(errors);
            }
        }

        /// <summary>
        /// Registers default generators
        /// </summary>
        private void RegisterDefaultGenerators()
        {
            // Register basic terrain generators
            RegisterGenerationAlgorithm("perlin", new SimplePerlinGenerator());
            RegisterGenerationAlgorithm("cellular", new SimpleCellularGenerator());
            RegisterGenerationAlgorithm("maze", new SimpleMazeGenerator());
            RegisterGenerationAlgorithm("rooms", new SimpleRoomGenerator());
            
            // Register basic entity placer
            RegisterEntityPlacer("default", new SimpleEntityPlacer());
        }
    }

    /// <summary>
    /// Simple Perlin noise terrain generator
    /// </summary>
    public class SimplePerlinGenerator : ITerrainGenerator
    {
        public TileMap GenerateTerrain(GenerationConfig config, int seed)
        {
            var random = new Random(seed);
            var tileMap = new TileMap(config.Width, config.Height);
            
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    // Simple noise-like generation
                    var noise = Math.Sin(x * 0.1) * Math.Cos(y * 0.1) + random.NextDouble() * 0.5;
                    
                    if (noise > 0.3)
                        tileMap.SetTile(x, y, TileType.Wall);
                    else if (noise < -0.2)
                        tileMap.SetTile(x, y, TileType.Water);
                    else
                        tileMap.SetTile(x, y, TileType.Ground);
                }
            }
            
            return tileMap;
        }
    }

    /// <summary>
    /// Simple cellular automata terrain generator
    /// </summary>
    public class SimpleCellularGenerator : ITerrainGenerator
    {
        public TileMap GenerateTerrain(GenerationConfig config, int seed)
        {
            var random = new Random(seed);
            var tileMap = new TileMap(config.Width, config.Height);
            
            // Initialize with random walls
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    tileMap.SetTile(x, y, random.NextDouble() > 0.45 ? TileType.Wall : TileType.Ground);
                }
            }
            
            // Apply cellular automata rules
            for (int iteration = 0; iteration < 5; iteration++)
            {
                var newTileMap = new TileMap(config.Width, config.Height);
                
                for (int x = 0; x < config.Width; x++)
                {
                    for (int y = 0; y < config.Height; y++)
                    {
                        var wallCount = CountNeighborWalls(tileMap, x, y);
                        newTileMap.SetTile(x, y, wallCount >= 4 ? TileType.Wall : TileType.Ground);
                    }
                }
                
                tileMap = newTileMap;
            }
            
            return tileMap;
        }
        
        private int CountNeighborWalls(TileMap tileMap, int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx < 0 || nx >= tileMap.Width || ny < 0 || ny >= tileMap.Height)
                    {
                        count++; // Treat out-of-bounds as walls
                    }
                    else if (tileMap.GetTile(nx, ny) == TileType.Wall)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Simple maze terrain generator
    /// </summary>
    public class SimpleMazeGenerator : ITerrainGenerator
    {
        public TileMap GenerateTerrain(GenerationConfig config, int seed)
        {
            var random = new Random(seed);
            var tileMap = new TileMap(config.Width, config.Height);
            
            // Fill with walls
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    tileMap.SetTile(x, y, TileType.Wall);
                }
            }
            
            // Create simple maze paths
            for (int x = 1; x < config.Width - 1; x += 2)
            {
                for (int y = 1; y < config.Height - 1; y += 2)
                {
                    tileMap.SetTile(x, y, TileType.Ground);
                    
                    // Randomly connect to neighbors
                    if (random.NextDouble() > 0.5 && x + 2 < config.Width - 1)
                        tileMap.SetTile(x + 1, y, TileType.Ground);
                    if (random.NextDouble() > 0.5 && y + 2 < config.Height - 1)
                        tileMap.SetTile(x, y + 1, TileType.Ground);
                }
            }
            
            return tileMap;
        }
    }

    /// <summary>
    /// Simple room-based terrain generator
    /// </summary>
    public class SimpleRoomGenerator : ITerrainGenerator
    {
        public TileMap GenerateTerrain(GenerationConfig config, int seed)
        {
            var random = new Random(seed);
            var tileMap = new TileMap(config.Width, config.Height);
            
            // Fill with walls
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    tileMap.SetTile(x, y, TileType.Wall);
                }
            }
            
            // Generate rooms
            int roomCount = Math.Max(3, Math.Min(8, (config.Width * config.Height) / 400));
            
            for (int i = 0; i < roomCount; i++)
            {
                int roomWidth = random.Next(5, Math.Min(15, config.Width / 3));
                int roomHeight = random.Next(5, Math.Min(15, config.Height / 3));
                int roomX = random.Next(1, config.Width - roomWidth - 1);
                int roomY = random.Next(1, config.Height - roomHeight - 1);
                
                // Create room
                for (int x = roomX; x < roomX + roomWidth; x++)
                {
                    for (int y = roomY; y < roomY + roomHeight; y++)
                    {
                        tileMap.SetTile(x, y, TileType.Ground);
                    }
                }
            }
            
            return tileMap;
        }
    }

    /// <summary>
    /// Simple entity placer
    /// </summary>
    public class SimpleEntityPlacer : IEntityPlacer
    {
        public List<Entity> PlaceEntities(TileMap terrain, GenerationConfig config, int seed)
        {
            var random = new Random(seed);
            var entities = new List<Entity>();
            
            // Find valid placement positions (ground tiles)
            var validPositions = new List<(int x, int y)>();
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    if (terrain.GetTile(x, y) == TileType.Ground)
                    {
                        validPositions.Add((x, y));
                    }
                }
            }
            
            // Place entities from configuration
            foreach (var entityConfig in config.Entities)
            {
                for (int i = 0; i < entityConfig.Count && validPositions.Count > 0; i++)
                {
                    var posIndex = random.Next(validPositions.Count);
                    var pos = validPositions[posIndex];
                    validPositions.RemoveAt(posIndex);
                    
                    entities.Add(new Entity
                    {
                        Type = entityConfig.Type,
                        Position = new Position { X = pos.x, Y = pos.y },
                        Properties = new Dictionary<string, object>(entityConfig.Properties)
                    });
                }
            }
            
            return entities;
        }
    }
}
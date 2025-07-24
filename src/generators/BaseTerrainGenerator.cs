using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Base class for terrain generators providing common functionality
    /// </summary>
    public abstract class BaseTerrainGenerator : ITerrainGenerator
    {
        protected IRandomGenerator _random;
        protected ISimpleLoggerService _logger;
        
        /// <summary>
        /// Creates a new base terrain generator
        /// </summary>
        /// <param name="randomGenerator">Random number generator</param>
        /// <param name="logger">Logger service for performance metrics</param>
        protected BaseTerrainGenerator(IRandomGenerator randomGenerator, ISimpleLoggerService logger = null)
        {
            _random = randomGenerator ?? throw new ArgumentNullException(nameof(randomGenerator));
            _logger = logger;
        }
        
        /// <summary>
        /// Generates terrain based on configuration and seed
        /// </summary>
        /// <param name="config">Generation configuration</param>
        /// <param name="seed">Random seed for reproducible generation</param>
        /// <returns>Generated tile map</returns>
        public virtual TileMap GenerateTerrain(GenerationConfig config, int seed)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var stopwatch = Stopwatch.StartNew();
            var algorithmName = GetAlgorithmName();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                _logger?.LogInfo($"Starting terrain generation with {algorithmName} algorithm", new {
                    OperationId = operationId,
                    Algorithm = algorithmName,
                    Width = config.Width,
                    Height = config.Height,
                    Seed = seed,
                    TotalTiles = config.Width * config.Height,
                    Parameters = config.AlgorithmParameters
                });
                
                _random.SetSeed(seed);
                
                var tileMap = new TileMap(config.Width, config.Height);
                
                // Initialize with empty tiles
                var initStopwatch = Stopwatch.StartNew();
                InitializeTileMap(tileMap);
                initStopwatch.Stop();
                
                _logger?.LogPerformance($"TerrainInitialization_{algorithmName}", initStopwatch.Elapsed, new {
                    TilesInitialized = config.Width * config.Height
                });
                
                // Generate the actual terrain
                var genStopwatch = Stopwatch.StartNew();
                GenerateTerrainInternal(tileMap, config);
                genStopwatch.Stop();
                
                _logger?.LogPerformance($"TerrainGeneration_{algorithmName}", genStopwatch.Elapsed, new {
                    TilesGenerated = config.Width * config.Height,
                    TilesPerSecond = (config.Width * config.Height) / genStopwatch.Elapsed.TotalSeconds
                });
                
                // Apply post-processing
                var postStopwatch = Stopwatch.StartNew();
                PostProcessTerrain(tileMap, config);
                postStopwatch.Stop();
                
                _logger?.LogPerformance($"TerrainPostProcessing_{algorithmName}", postStopwatch.Elapsed, new {
                    TilesProcessed = config.Width * config.Height
                });
                
                stopwatch.Stop();
                
                // Calculate terrain statistics
                var terrainStats = CalculateTerrainStatistics(tileMap);
                
                _logger?.LogGeneration($"TerrainGeneration_{algorithmName} (Config: {operationId})", stopwatch.Elapsed, new {
                    Algorithm = algorithmName,
                    Width = tileMap.Width,
                    Height = tileMap.Height,
                    Seed = seed,
                    TotalTiles = tileMap.Width * tileMap.Height,
                    TerrainStatistics = terrainStats,
                    Parameters = config.AlgorithmParameters
                });
                
                _logger?.LogInfo($"Terrain generation completed successfully with {algorithmName}", new {
                    OperationId = operationId,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                    TilesGenerated = tileMap.Width * tileMap.Height,
                    TerrainStats = terrainStats
                });
                
                return tileMap;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError($"Terrain generation failed with {algorithmName} algorithm", ex, new {
                    OperationId = operationId,
                    Algorithm = algorithmName,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Width = config.Width,
                    Height = config.Height,
                    Seed = seed,
                    Parameters = config.AlgorithmParameters
                });
                throw;
            }
        }
        
        /// <summary>
        /// Abstract method for actual terrain generation implementation
        /// </summary>
        /// <param name="tileMap">Tile map to populate</param>
        /// <param name="config">Generation configuration</param>
        protected abstract void GenerateTerrainInternal(TileMap tileMap, GenerationConfig config);
        
        /// <summary>
        /// Gets the name of this generation algorithm
        /// </summary>
        /// <returns>Algorithm name</returns>
        public abstract string GetAlgorithmName();
        
        /// <summary>
        /// Gets the default parameters for this algorithm
        /// </summary>
        /// <returns>Dictionary of default parameter values</returns>
        public abstract Dictionary<string, object> GetDefaultParameters();
        
        /// <summary>
        /// Validates algorithm-specific parameters
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>List of validation error messages</returns>
        public virtual List<string> ValidateParameters(Dictionary<string, object> parameters)
        {
            var errors = new List<string>();
            
            if (parameters == null)
            {
                errors.Add("Parameters cannot be null");
                return errors;
            }
            
            return errors;
        }
        
        /// <summary>
        /// Checks if this generator supports the given parameters
        /// </summary>
        /// <param name="parameters">Algorithm-specific parameters</param>
        /// <returns>True if parameters are supported</returns>
        public virtual bool SupportsParameters(Dictionary<string, object> parameters)
        {
            var errors = ValidateParameters(parameters);
            return errors.Count == 0;
        }
        
        /// <summary>
        /// Initializes the tile map with empty tiles
        /// </summary>
        /// <param name="tileMap">Tile map to initialize</param>
        protected virtual void InitializeTileMap(TileMap tileMap)
        {
            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    tileMap.SetTile(x, y, TileType.Empty);
                }
            }
        }
        
        /// <summary>
        /// Applies post-processing to the generated terrain
        /// </summary>
        /// <param name="tileMap">Generated tile map</param>
        /// <param name="config">Generation configuration</param>
        protected virtual void PostProcessTerrain(TileMap tileMap, GenerationConfig config)
        {
            // Add borders if needed
            AddBorders(tileMap);
            
            // Ensure connectivity
            EnsureConnectivity(tileMap);
        }
        
        /// <summary>
        /// Adds wall borders around the map
        /// </summary>
        /// <param name="tileMap">Tile map to add borders to</param>
        protected virtual void AddBorders(TileMap tileMap)
        {
            // Top and bottom borders
            for (int x = 0; x < tileMap.Width; x++)
            {
                tileMap.SetTile(x, 0, TileType.Wall);
                tileMap.SetTile(x, tileMap.Height - 1, TileType.Wall);
            }
            
            // Left and right borders
            for (int y = 0; y < tileMap.Height; y++)
            {
                tileMap.SetTile(0, y, TileType.Wall);
                tileMap.SetTile(tileMap.Width - 1, y, TileType.Wall);
            }
        }
        
        /// <summary>
        /// Ensures basic connectivity in the terrain
        /// </summary>
        /// <param name="tileMap">Tile map to process</param>
        protected virtual void EnsureConnectivity(TileMap tileMap)
        {
            // Basic implementation: ensure there's at least one walkable path
            // More sophisticated implementations can be added in derived classes
            
            // Find the first walkable tile
            int startX = -1, startY = -1;
            for (int x = 1; x < tileMap.Width - 1 && startX == -1; x++)
            {
                for (int y = 1; y < tileMap.Height - 1 && startY == -1; y++)
                {
                    if (tileMap.IsWalkable(x, y))
                    {
                        startX = x;
                        startY = y;
                    }
                }
            }
            
            // If no walkable tiles found, create a basic path
            if (startX == -1)
            {
                CreateBasicPath(tileMap);
            }
        }
        
        /// <summary>
        /// Creates a basic walkable path in the terrain
        /// </summary>
        /// <param name="tileMap">Tile map to modify</param>
        protected virtual void CreateBasicPath(TileMap tileMap)
        {
            // Create a simple horizontal path in the middle
            int midY = tileMap.Height / 2;
            for (int x = 1; x < tileMap.Width - 1; x++)
            {
                tileMap.SetTile(x, midY, TileType.Ground);
            }
        }
        
        /// <summary>
        /// Converts a string terrain type to TileType enum
        /// </summary>
        /// <param name="terrainType">String terrain type</param>
        /// <returns>Corresponding TileType</returns>
        protected TileType StringToTileType(string terrainType)
        {
            return terrainType?.ToLower() switch
            {
                "ground" => TileType.Ground,
                "wall" => TileType.Wall,
                "water" => TileType.Water,
                "grass" => TileType.Grass,
                "stone" => TileType.Stone,
                "sand" => TileType.Sand,
                "lava" => TileType.Lava,
                "ice" => TileType.Ice,
                _ => TileType.Ground
            };
        }
        
        /// <summary>
        /// Gets a parameter value with type conversion and default fallback
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="parameters">Parameters dictionary</param>
        /// <param name="key">Parameter key</param>
        /// <param name="defaultValue">Default value if not found or conversion fails</param>
        /// <returns>Parameter value or default</returns>
        protected T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue)
        {
            if (parameters == null || !parameters.ContainsKey(key))
                return defaultValue;
                
            try
            {
                return (T)Convert.ChangeType(parameters[key], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Calculates terrain statistics for logging
        /// </summary>
        /// <param name="terrain">Terrain to analyze</param>
        /// <returns>Dictionary of terrain statistics</returns>
        protected virtual Dictionary<string, object> CalculateTerrainStatistics(TileMap terrain)
        {
            var tileCounts = new Dictionary<TileType, int>();
            var totalTiles = terrain.Width * terrain.Height;
            var walkableTiles = 0;
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tileType = terrain.GetTile(x, y);
                    tileCounts[tileType] = tileCounts.GetValueOrDefault(tileType, 0) + 1;
                    
                    if (terrain.IsWalkable(x, y))
                        walkableTiles++;
                }
            }
            
            return new Dictionary<string, object>
            {
                ["TotalTiles"] = totalTiles,
                ["WalkableTiles"] = walkableTiles,
                ["WalkablePercentage"] = (walkableTiles * 100.0) / totalTiles,
                ["TileComposition"] = tileCounts.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => new { Count = kvp.Value, Percentage = (kvp.Value * 100.0) / totalTiles }
                )
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Terrain generator using cellular automata algorithm for cave-like structures
    /// </summary>
    public class CellularAutomataGenerator : BaseTerrainGenerator
    {
        /// <summary>
        /// Creates a new cellular automata terrain generator
        /// </summary>
        /// <param name="randomGenerator">Random number generator</param>
        /// <param name="logger">Logger service for performance metrics</param>
        public CellularAutomataGenerator(IRandomGenerator randomGenerator, ISimpleLoggerService logger = null) : base(randomGenerator, logger)
        {
        }
        
        /// <summary>
        /// Gets the name of this generation algorithm
        /// </summary>
        public override string GetAlgorithmName() => "cellular";
        
        /// <summary>
        /// Gets the default parameters for this algorithm
        /// </summary>
        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "initialFillProbability", 0.45f },
                { "iterations", 5 },
                { "birthLimit", 4 },
                { "deathLimit", 3 },
                { "wallType", "wall" },
                { "floorType", "ground" }
            };
        }
        
        /// <summary>
        /// Validates algorithm-specific parameters
        /// </summary>
        public override List<string> ValidateParameters(Dictionary<string, object> parameters)
        {
            var errors = base.ValidateParameters(parameters);
            
            if (parameters != null)
            {
                // Check for valid parameter names
                var validParams = new[] { "initialFillProbability", "iterations", "birthLimit", "deathLimit", "wallType", "floorType" };
                foreach (var param in parameters.Keys)
                {
                    if (!Array.Exists(validParams, p => p.Equals(param, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"Unknown parameter '{param}' for cellular automata generator");
                    }
                }
                
                // Validate specific parameter values
                if (parameters.ContainsKey("initialFillProbability"))
                {
                    var fillProb = GetParameter<float>(parameters, "initialFillProbability", 0.45f);
                    if (fillProb < 0.0f || fillProb > 1.0f)
                        errors.Add("Initial fill probability must be between 0.0 and 1.0");
                }
                
                if (parameters.ContainsKey("iterations"))
                {
                    var iterations = GetParameter<int>(parameters, "iterations", 5);
                    if (iterations < 1 || iterations > 20)
                        errors.Add("Iterations must be between 1 and 20");
                }
                
                if (parameters.ContainsKey("birthLimit"))
                {
                    var birthLimit = GetParameter<int>(parameters, "birthLimit", 4);
                    if (birthLimit < 0 || birthLimit > 8)
                        errors.Add("Birth limit must be between 0 and 8");
                }
                
                if (parameters.ContainsKey("deathLimit"))
                {
                    var deathLimit = GetParameter<int>(parameters, "deathLimit", 3);
                    if (deathLimit < 0 || deathLimit > 8)
                        errors.Add("Death limit must be between 0 and 8");
                }
            }
            
            return errors;
        }
        
        /// <summary>
        /// Generates terrain using cellular automata algorithm
        /// </summary>
        protected override void GenerateTerrainInternal(TileMap tileMap, GenerationConfig config)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            // Extract parameters with defaults
            var initialFillProbability = GetParameter<float>(config.AlgorithmParameters, "initialFillProbability", 0.45f);
            var iterations = GetParameter<int>(config.AlgorithmParameters, "iterations", 5);
            var birthLimit = GetParameter<int>(config.AlgorithmParameters, "birthLimit", 4);
            var deathLimit = GetParameter<int>(config.AlgorithmParameters, "deathLimit", 3);
            var wallType = GetParameter<string>(config.AlgorithmParameters, "wallType", "wall");
            var floorType = GetParameter<string>(config.AlgorithmParameters, "floorType", "ground");
            
            var wallTileType = StringToTileType(wallType);
            var floorTileType = StringToTileType(floorType);
            
            _logger?.LogInfo("Starting cellular automata terrain generation", new {
                OperationId = operationId,
                Algorithm = "CellularAutomata",
                InitialFillProbability = initialFillProbability,
                Iterations = iterations,
                BirthLimit = birthLimit,
                DeathLimit = deathLimit,
                WallType = wallType,
                FloorType = floorType,
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TotalTiles = tileMap.Width * tileMap.Height,
                Seed = _random.GetSeed(),
                Operation = "CellularAutomataTerrainGeneration"
            });
            
            // Log parameter validation
            var paramValidation = ValidateParameters(config.AlgorithmParameters);
            if (paramValidation.Any())
            {
                _logger?.LogWarning("Cellular automata parameter validation warnings", new {
                    OperationId = operationId,
                    ValidationWarnings = paramValidation,
                    WarningCount = paramValidation.Count
                });
            }
            
            // Initialize with random fill
            var initStopwatch = Stopwatch.StartNew();
            _logger?.LogInfo("Starting random fill initialization", new {
                OperationId = operationId,
                FillProbability = initialFillProbability,
                WallType = wallType,
                FloorType = floorType
            });
            
            InitializeRandomFill(tileMap, initialFillProbability, wallTileType, floorTileType);
            initStopwatch.Stop();
            
            // Calculate initial composition
            var initialComposition = CalculateTerrainComposition(tileMap);
            
            _logger?.LogPerformance("CellularAutomata_RandomFillInitialization", initStopwatch.Elapsed, new {
                OperationId = operationId,
                TilesInitialized = tileMap.Width * tileMap.Height,
                FillProbability = initialFillProbability,
                InitializationRate = (tileMap.Width * tileMap.Height) / initStopwatch.Elapsed.TotalSeconds,
                InitialComposition = initialComposition
            });
            
            // Apply cellular automata rules for specified iterations
            var iterationStats = new List<object>();
            for (int i = 0; i < iterations; i++)
            {
                var iterationStopwatch = Stopwatch.StartNew();
                
                _logger?.LogInfo($"Starting cellular automata iteration {i + 1}", new {
                    OperationId = operationId,
                    IterationNumber = i + 1,
                    TotalIterations = iterations,
                    BirthLimit = birthLimit,
                    DeathLimit = deathLimit
                });
                
                ApplyCellularAutomataRules(tileMap, birthLimit, deathLimit, wallTileType, floorTileType);
                iterationStopwatch.Stop();
                
                // Calculate composition after this iteration
                var iterationComposition = CalculateTerrainComposition(tileMap);
                var iterationStat = new {
                    IterationNumber = i + 1,
                    DurationMs = iterationStopwatch.ElapsedMilliseconds,
                    ProcessingRate = (tileMap.Width * tileMap.Height) / iterationStopwatch.Elapsed.TotalSeconds,
                    TerrainComposition = iterationComposition
                };
                iterationStats.Add(iterationStat);
                
                _logger?.LogPerformance($"CellularAutomata_Iteration_{i + 1}", iterationStopwatch.Elapsed, new {
                    OperationId = operationId,
                    IterationNumber = i + 1,
                    TotalIterations = iterations,
                    TilesProcessed = tileMap.Width * tileMap.Height,
                    ProcessingRate = iterationStat.ProcessingRate,
                    TerrainComposition = iterationComposition
                });
            }
            
            // Clean up small isolated areas
            var cleanupStopwatch = Stopwatch.StartNew();
            _logger?.LogInfo("Starting area cleanup", new {
                OperationId = operationId,
                MinAreaSize = 10 // From the hardcoded value in CleanupSmallAreas
            });
            
            var preCleanupComposition = CalculateTerrainComposition(tileMap);
            CleanupSmallAreas(tileMap, wallTileType, floorTileType);
            var postCleanupComposition = CalculateTerrainComposition(tileMap);
            cleanupStopwatch.Stop();
            
            _logger?.LogPerformance("CellularAutomata_AreaCleanup", cleanupStopwatch.Elapsed, new {
                OperationId = operationId,
                TilesProcessed = tileMap.Width * tileMap.Height,
                ProcessingRate = (tileMap.Width * tileMap.Height) / cleanupStopwatch.Elapsed.TotalSeconds,
                PreCleanupComposition = preCleanupComposition,
                PostCleanupComposition = postCleanupComposition
            });
            
            stopwatch.Stop();
            
            var finalComposition = CalculateTerrainComposition(tileMap);
            
            _logger?.LogGeneration(operationId, "CellularAutomataTerrainGeneration", stopwatch.Elapsed, new {
                Algorithm = "CellularAutomata",
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TotalTiles = tileMap.Width * tileMap.Height,
                Parameters = new { 
                    InitialFillProbability = initialFillProbability, 
                    Iterations = iterations, 
                    BirthLimit = birthLimit, 
                    DeathLimit = deathLimit, 
                    WallType = wallType, 
                    FloorType = floorType 
                },
                IterationStatistics = iterationStats,
                FinalComposition = finalComposition,
                Seed = _random.GetSeed()
            });
            
            _logger?.LogInfo("Cellular automata terrain generation completed successfully", new {
                OperationId = operationId,
                TotalDurationMs = stopwatch.ElapsedMilliseconds,
                Iterations = iterations,
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TilesGenerated = tileMap.Width * tileMap.Height,
                GenerationRate = (tileMap.Width * tileMap.Height) / stopwatch.Elapsed.TotalSeconds,
                FinalComposition = finalComposition,
                IterationCount = iterations
            });
        }
        
        /// <summary>
        /// Calculates terrain composition statistics
        /// </summary>
        private Dictionary<string, object> CalculateTerrainComposition(TileMap tileMap)
        {
            var tileCounts = new Dictionary<TileType, int>();
            var totalTiles = tileMap.Width * tileMap.Height;

            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    var tileType = tileMap.GetTile(x, y);
                    tileCounts[tileType] = tileCounts.GetValueOrDefault(tileType, 0) + 1;
                }
            }

            return tileCounts.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => (object)new { 
                    Count = kvp.Value, 
                    Percentage = (kvp.Value * 100.0) / totalTiles,
                    Density = (double)kvp.Value / totalTiles
                }
            );
        }
        
        /// <summary>
        /// Initializes the map with random fill based on probability
        /// </summary>
        private void InitializeRandomFill(TileMap tileMap, float fillProbability, TileType wallType, TileType floorType)
        {
            for (int x = 1; x < tileMap.Width - 1; x++)
            {
                for (int y = 1; y < tileMap.Height - 1; y++)
                {
                    if (_random.NextFloat() < fillProbability)
                    {
                        tileMap.SetTile(x, y, wallType);
                    }
                    else
                    {
                        tileMap.SetTile(x, y, floorType);
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies cellular automata rules to evolve the terrain
        /// </summary>
        private void ApplyCellularAutomataRules(TileMap tileMap, int birthLimit, int deathLimit, TileType wallType, TileType floorType)
        {
            var newMap = new TileType[tileMap.Width, tileMap.Height];
            
            // Copy borders
            for (int x = 0; x < tileMap.Width; x++)
            {
                newMap[x, 0] = tileMap.GetTile(x, 0);
                newMap[x, tileMap.Height - 1] = tileMap.GetTile(x, tileMap.Height - 1);
            }
            for (int y = 0; y < tileMap.Height; y++)
            {
                newMap[0, y] = tileMap.GetTile(0, y);
                newMap[tileMap.Width - 1, y] = tileMap.GetTile(tileMap.Width - 1, y);
            }
            
            // Apply rules to interior cells
            for (int x = 1; x < tileMap.Width - 1; x++)
            {
                for (int y = 1; y < tileMap.Height - 1; y++)
                {
                    int neighborWalls = CountNeighborWalls(tileMap, x, y, wallType);
                    var currentTile = tileMap.GetTile(x, y);
                    
                    if (currentTile == wallType)
                    {
                        // Wall cell: dies if it has fewer neighbors than death limit
                        newMap[x, y] = neighborWalls >= deathLimit ? wallType : floorType;
                    }
                    else
                    {
                        // Floor cell: becomes wall if it has more neighbors than birth limit
                        newMap[x, y] = neighborWalls > birthLimit ? wallType : floorType;
                    }
                }
            }
            
            // Copy the new map back
            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    tileMap.SetTile(x, y, newMap[x, y]);
                }
            }
        }
        
        /// <summary>
        /// Counts the number of wall neighbors around a cell
        /// </summary>
        private int CountNeighborWalls(TileMap tileMap, int x, int y, TileType wallType)
        {
            int count = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip the center cell
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    // Treat out-of-bounds as walls
                    if (nx < 0 || nx >= tileMap.Width || ny < 0 || ny >= tileMap.Height)
                    {
                        count++;
                    }
                    else if (tileMap.GetTile(nx, ny) == wallType)
                    {
                        count++;
                    }
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Cleans up small isolated areas to improve connectivity
        /// </summary>
        private void CleanupSmallAreas(TileMap tileMap, TileType wallType, TileType floorType)
        {
            var visited = new bool[tileMap.Width, tileMap.Height];
            
            for (int x = 1; x < tileMap.Width - 1; x++)
            {
                for (int y = 1; y < tileMap.Height - 1; y++)
                {
                    if (!visited[x, y] && tileMap.GetTile(x, y) == floorType)
                    {
                        var area = FloodFill(tileMap, x, y, floorType, visited);
                        
                        // If the area is too small, fill it with walls
                        if (area.Count < 10) // Minimum area size
                        {
                            foreach (var point in area)
                            {
                                tileMap.SetTile(point.X, point.Y, wallType);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Performs flood fill to find connected areas
        /// </summary>
        private List<Point> FloodFill(TileMap tileMap, int startX, int startY, TileType targetType, bool[,] visited)
        {
            var area = new List<Point>();
            var stack = new Stack<Point>();
            stack.Push(new Point(startX, startY));
            
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                
                if (current.X < 0 || current.X >= tileMap.Width || 
                    current.Y < 0 || current.Y >= tileMap.Height ||
                    visited[current.X, current.Y] ||
                    tileMap.GetTile(current.X, current.Y) != targetType)
                {
                    continue;
                }
                
                visited[current.X, current.Y] = true;
                area.Add(current);
                
                // Add neighbors to stack
                stack.Push(new Point(current.X + 1, current.Y));
                stack.Push(new Point(current.X - 1, current.Y));
                stack.Push(new Point(current.X, current.Y + 1));
                stack.Push(new Point(current.X, current.Y - 1));
            }
            
            return area;
        }
        
        /// <summary>
        /// Simple point structure for flood fill
        /// </summary>
        private struct Point
        {
            public int X { get; }
            public int Y { get; }
            
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
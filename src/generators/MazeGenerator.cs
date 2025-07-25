using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Terrain generator using maze generation algorithms for maze-like structures
    /// </summary>
    public class MazeGenerator : BaseTerrainGenerator
    {
        /// <summary>
        /// Creates a new maze terrain generator
        /// </summary>
        /// <param name="randomGenerator">Random number generator</param>
        /// <param name="logger">Logger service for performance metrics</param>
        public MazeGenerator(IRandomGenerator randomGenerator, ISimpleLoggerService logger = null) : base(randomGenerator, logger)
        {
        }
        
        /// <summary>
        /// Gets the name of this generation algorithm
        /// </summary>
        public override string GetAlgorithmName() => "maze";
        
        /// <summary>
        /// Gets the default parameters for this algorithm
        /// </summary>
        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "algorithm", "recursive_backtracking" },
                { "wallType", "wall" },
                { "pathType", "ground" },
                { "complexity", 0.5f },
                { "density", 0.5f },
                { "braidingFactor", 0.0f }
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
                var validParams = new[] { "algorithm", "wallType", "pathType", "complexity", "density", "braidingFactor" };
                foreach (var param in parameters.Keys)
                {
                    if (!Array.Exists(validParams, p => p.Equals(param, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"Unknown parameter '{param}' for maze generator");
                    }
                }
                
                // Validate specific parameter values
                if (parameters.ContainsKey("algorithm"))
                {
                    var algorithm = GetParameter<string>(parameters, "algorithm", "recursive_backtracking");
                    var validAlgorithms = new[] { "recursive_backtracking", "kruskal", "prim", "simple" };
                    if (!Array.Exists(validAlgorithms, a => a.Equals(algorithm, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"Invalid maze algorithm '{algorithm}'. Valid algorithms are: {string.Join(", ", validAlgorithms)}");
                    }
                }
                
                if (parameters.ContainsKey("complexity"))
                {
                    var complexity = GetParameter<float>(parameters, "complexity", 0.5f);
                    if (complexity < 0.0f || complexity > 1.0f)
                        errors.Add("Complexity must be between 0.0 and 1.0");
                }
                
                if (parameters.ContainsKey("density"))
                {
                    var density = GetParameter<float>(parameters, "density", 0.5f);
                    if (density < 0.0f || density > 1.0f)
                        errors.Add("Density must be between 0.0 and 1.0");
                }
                
                if (parameters.ContainsKey("braidingFactor"))
                {
                    var braidingFactor = GetParameter<float>(parameters, "braidingFactor", 0.0f);
                    if (braidingFactor < 0.0f || braidingFactor > 1.0f)
                        errors.Add("Braiding factor must be between 0.0 and 1.0");
                }
            }
            
            return errors;
        }
        
        /// <summary>
        /// Generates terrain using maze generation algorithm
        /// </summary>
        protected override void GenerateTerrainInternal(TileMap tileMap, GenerationConfig config)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            // Extract parameters with defaults
            var algorithm = GetParameter<string>(config.AlgorithmParameters, "algorithm", "recursive_backtracking");
            var wallType = GetParameter<string>(config.AlgorithmParameters, "wallType", "wall");
            var pathType = GetParameter<string>(config.AlgorithmParameters, "pathType", "ground");
            var complexity = GetParameter<float>(config.AlgorithmParameters, "complexity", 0.5f);
            var density = GetParameter<float>(config.AlgorithmParameters, "density", 0.5f);
            var braidingFactor = GetParameter<float>(config.AlgorithmParameters, "braidingFactor", 0.0f);
            
            var wallTileType = StringToTileType(wallType);
            var pathTileType = StringToTileType(pathType);
            
            _logger?.LogInfo("Starting maze terrain generation", new {
                OperationId = operationId,
                Algorithm = algorithm,
                WallType = wallType,
                PathType = pathType,
                Complexity = complexity,
                Density = density,
                BraidingFactor = braidingFactor,
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TotalTiles = tileMap.Width * tileMap.Height,
                Seed = _random.GetSeed(),
                Operation = "MazeTerrainGeneration"
            });
            
            // Log parameter validation
            var paramValidation = ValidateParameters(config.AlgorithmParameters);
            if (paramValidation.Any())
            {
                _logger?.LogWarning("Maze parameter validation warnings", new {
                    OperationId = operationId,
                    ValidationWarnings = paramValidation,
                    WarningCount = paramValidation.Count
                });
            }
            
            // Initialize maze with walls
            var initStopwatch = Stopwatch.StartNew();
            _logger?.LogInfo("Starting maze wall initialization", new {
                OperationId = operationId,
                WallType = wallType
            });
            InitializeMazeWithWalls(tileMap, wallTileType);
            initStopwatch.Stop();
            
            _logger?.LogPerformance("Maze_WallInitialization", initStopwatch.Elapsed, new {
                OperationId = operationId,
                TilesInitialized = tileMap.Width * tileMap.Height,
                InitializationRate = (tileMap.Width * tileMap.Height) / initStopwatch.Elapsed.TotalSeconds
            });
            
            // Generate maze based on selected algorithm
            var genStopwatch = Stopwatch.StartNew();
            _logger?.LogInfo($"Starting maze generation with algorithm: {algorithm}", new {
                OperationId = operationId,
                Algorithm = algorithm,
                Complexity = complexity,
                Density = density
            });
            
            var preGenComposition = CalculateTerrainComposition(tileMap);
            switch (algorithm.ToLower())
            {
                case "recursive_backtracking":
                    GenerateRecursiveBacktrackingMaze(tileMap, wallTileType, pathTileType);
                    break;
                case "simple":
                    GenerateSimpleMaze(tileMap, wallTileType, pathTileType, complexity, density);
                    break;
                case "kruskal":
                case "prim":
                    // For now, fall back to recursive backtracking for these algorithms
                    _logger?.LogWarning($"Algorithm '{algorithm}' not fully implemented, falling back to recursive_backtracking", new {
                        OperationId = operationId,
                        RequestedAlgorithm = algorithm,
                        FallbackAlgorithm = "recursive_backtracking"
                    });
                    GenerateRecursiveBacktrackingMaze(tileMap, wallTileType, pathTileType);
                    break;
                default:
                    _logger?.LogWarning($"Unknown algorithm '{algorithm}', falling back to recursive_backtracking", new {
                        OperationId = operationId,
                        RequestedAlgorithm = algorithm,
                        FallbackAlgorithm = "recursive_backtracking"
                    });
                    GenerateRecursiveBacktrackingMaze(tileMap, wallTileType, pathTileType);
                    break;
            }
            var postGenComposition = CalculateTerrainComposition(tileMap);
            genStopwatch.Stop();
            
            _logger?.LogPerformance($"Maze_{algorithm}_Generation", genStopwatch.Elapsed, new {
                OperationId = operationId,
                Algorithm = algorithm,
                TilesProcessed = tileMap.Width * tileMap.Height,
                ProcessingRate = (tileMap.Width * tileMap.Height) / genStopwatch.Elapsed.TotalSeconds,
                PreGenComposition = preGenComposition,
                PostGenComposition = postGenComposition
            });
            
            // Apply braiding if requested
            if (braidingFactor > 0.0f)
            {
                var braidStopwatch = Stopwatch.StartNew();
                _logger?.LogInfo("Starting maze braiding", new {
                    OperationId = operationId,
                    BraidingFactor = braidingFactor
                });
                
                var preBraidComposition = CalculateTerrainComposition(tileMap);
                ApplyBraiding(tileMap, wallTileType, pathTileType, braidingFactor);
                var postBraidComposition = CalculateTerrainComposition(tileMap);
                braidStopwatch.Stop();
                
                _logger?.LogPerformance("Maze_Braiding", braidStopwatch.Elapsed, new {
                    OperationId = operationId,
                    BraidingFactor = braidingFactor,
                    TilesProcessed = tileMap.Width * tileMap.Height,
                    ProcessingRate = (tileMap.Width * tileMap.Height) / braidStopwatch.Elapsed.TotalSeconds,
                    PreBraidComposition = preBraidComposition,
                    PostBraidComposition = postBraidComposition
                });
            }
            
            stopwatch.Stop();
            var finalComposition = CalculateTerrainComposition(tileMap);
            
            _logger?.LogGeneration(operationId, "MazeTerrainGeneration", stopwatch.Elapsed, new {
                Algorithm = algorithm,
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TotalTiles = tileMap.Width * tileMap.Height,
                Parameters = new { 
                    Algorithm = algorithm,
                    WallType = wallType, 
                    PathType = pathType, 
                    Complexity = complexity, 
                    Density = density,
                    BraidingFactor = braidingFactor
                },
                FinalComposition = finalComposition,
                Seed = _random.GetSeed(),
                BraidingApplied = braidingFactor > 0.0f
            });
            
            _logger?.LogInfo("Maze terrain generation completed successfully", new {
                OperationId = operationId,
                TotalDurationMs = stopwatch.ElapsedMilliseconds,
                Algorithm = algorithm,
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TilesGenerated = tileMap.Width * tileMap.Height,
                GenerationRate = (tileMap.Width * tileMap.Height) / stopwatch.Elapsed.TotalSeconds,
                FinalComposition = finalComposition,
                BraidingApplied = braidingFactor > 0.0f
            });
        }
        
        /// <summary>
        /// Initializes the maze with walls
        /// </summary>
        private void InitializeMazeWithWalls(TileMap tileMap, TileType wallType)
        {
            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    tileMap.SetTile(x, y, wallType);
                }
            }
        }
        
        /// <summary>
        /// Generates a maze using recursive backtracking algorithm
        /// </summary>
        private void GenerateRecursiveBacktrackingMaze(TileMap tileMap, TileType wallType, TileType pathType)
        {
            var visited = new bool[tileMap.Width, tileMap.Height];
            var stack = new Stack<MazeCell>();
            
            // Start from a random odd position (maze cells are at odd coordinates)
            int startX = 1 + (_random.Next((tileMap.Width - 1) / 2) * 2);
            int startY = 1 + (_random.Next((tileMap.Height - 1) / 2) * 2);
            
            var current = new MazeCell(startX, startY);
            tileMap.SetTile(current.X, current.Y, pathType);
            visited[current.X, current.Y] = true;
            stack.Push(current);
            
            while (stack.Count > 0)
            {
                current = stack.Peek();
                var neighbors = GetUnvisitedNeighbors(current, tileMap, visited);
                
                if (neighbors.Count > 0)
                {
                    var next = neighbors[_random.Next(neighbors.Count)];
                    
                    // Remove wall between current and next
                    int wallX = (current.X + next.X) / 2;
                    int wallY = (current.Y + next.Y) / 2;
                    tileMap.SetTile(wallX, wallY, pathType);
                    tileMap.SetTile(next.X, next.Y, pathType);
                    
                    visited[next.X, next.Y] = true;
                    stack.Push(next);
                }
                else
                {
                    stack.Pop();
                }
            }
        }
        
        /// <summary>
        /// Generates a simple maze using random wall removal
        /// </summary>
        private void GenerateSimpleMaze(TileMap tileMap, TileType wallType, TileType pathType, float complexity, float density)
        {
            // Create a grid pattern first
            for (int x = 1; x < tileMap.Width - 1; x += 2)
            {
                for (int y = 1; y < tileMap.Height - 1; y += 2)
                {
                    tileMap.SetTile(x, y, pathType);
                }
            }
            
            // Add random walls based on complexity and density
            int wallsToAdd = (int)((tileMap.Width * tileMap.Height * complexity * density) / 4);
            
            for (int i = 0; i < wallsToAdd; i++)
            {
                int x = _random.Next(1, tileMap.Width - 1);
                int y = _random.Next(1, tileMap.Height - 1);
                
                // Only place walls on even coordinates to maintain maze structure
                if ((x % 2 == 0) || (y % 2 == 0))
                {
                    if (_random.NextFloat() < density)
                    {
                        tileMap.SetTile(x, y, wallType);
                    }
                    else
                    {
                        tileMap.SetTile(x, y, pathType);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets unvisited neighbors for maze generation
        /// </summary>
        private List<MazeCell> GetUnvisitedNeighbors(MazeCell cell, TileMap tileMap, bool[,] visited)
        {
            var neighbors = new List<MazeCell>();
            var directions = new[]
            {
                new MazeCell(0, -2), // North
                new MazeCell(2, 0),  // East
                new MazeCell(0, 2),  // South
                new MazeCell(-2, 0)  // West
            };
            
            foreach (var dir in directions)
            {
                int newX = cell.X + dir.X;
                int newY = cell.Y + dir.Y;
                
                if (newX > 0 && newX < tileMap.Width - 1 && 
                    newY > 0 && newY < tileMap.Height - 1 && 
                    !visited[newX, newY])
                {
                    neighbors.Add(new MazeCell(newX, newY));
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Applies braiding to the maze by removing some dead ends
        /// </summary>
        private void ApplyBraiding(TileMap tileMap, TileType wallType, TileType pathType, float braidingFactor)
        {
            var deadEnds = FindDeadEnds(tileMap, pathType);
            int braidCount = (int)(deadEnds.Count * braidingFactor);
            
            for (int i = 0; i < braidCount && deadEnds.Count > 0; i++)
            {
                var deadEnd = deadEnds[_random.Next(deadEnds.Count)];
                deadEnds.Remove(deadEnd);
                
                // Find a wall to remove to connect this dead end
                var wallsToRemove = GetAdjacentWalls(deadEnd, tileMap, wallType);
                if (wallsToRemove.Count > 0)
                {
                    var wallToRemove = wallsToRemove[_random.Next(wallsToRemove.Count)];
                    tileMap.SetTile(wallToRemove.X, wallToRemove.Y, pathType);
                }
            }
        }
        
        /// <summary>
        /// Finds all dead ends in the maze
        /// </summary>
        private List<MazeCell> FindDeadEnds(TileMap tileMap, TileType pathType)
        {
            var deadEnds = new List<MazeCell>();
            
            for (int x = 1; x < tileMap.Width - 1; x++)
            {
                for (int y = 1; y < tileMap.Height - 1; y++)
                {
                    if (tileMap.GetTile(x, y) == pathType)
                    {
                        int pathNeighbors = CountPathNeighbors(tileMap, x, y, pathType);
                        if (pathNeighbors == 1) // Dead end has only one path neighbor
                        {
                            deadEnds.Add(new MazeCell(x, y));
                        }
                    }
                }
            }
            
            return deadEnds;
        }
        
        /// <summary>
        /// Gets adjacent walls to a cell
        /// </summary>
        private List<MazeCell> GetAdjacentWalls(MazeCell cell, TileMap tileMap, TileType wallType)
        {
            var walls = new List<MazeCell>();
            var directions = new[]
            {
                new MazeCell(0, -1), // North
                new MazeCell(1, 0),  // East
                new MazeCell(0, 1),  // South
                new MazeCell(-1, 0)  // West
            };
            
            foreach (var dir in directions)
            {
                int newX = cell.X + dir.X;
                int newY = cell.Y + dir.Y;
                
                if (newX > 0 && newX < tileMap.Width - 1 && 
                    newY > 0 && newY < tileMap.Height - 1 && 
                    tileMap.GetTile(newX, newY) == wallType)
                {
                    walls.Add(new MazeCell(newX, newY));
                }
            }
            
            return walls;
        }
        
        /// <summary>
        /// Counts path neighbors around a cell
        /// </summary>
        private int CountPathNeighbors(TileMap tileMap, int x, int y, TileType pathType)
        {
            int count = 0;
            var directions = new[]
            {
                new MazeCell(0, -1), // North
                new MazeCell(1, 0),  // East
                new MazeCell(0, 1),  // South
                new MazeCell(-1, 0)  // West
            };
            
            foreach (var dir in directions)
            {
                int newX = x + dir.X;
                int newY = y + dir.Y;
                
                if (newX >= 0 && newX < tileMap.Width && 
                    newY >= 0 && newY < tileMap.Height && 
                    tileMap.GetTile(newX, newY) == pathType)
                {
                    count++;
                }
            }
            
            return count;
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
        /// Simple structure to represent a maze cell
        /// </summary>
        private struct MazeCell
        {
            public int X { get; }
            public int Y { get; }
            
            public MazeCell(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
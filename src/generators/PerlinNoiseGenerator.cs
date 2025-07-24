using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Terrain generator using Perlin noise algorithm for natural-looking terrain
    /// </summary>
    public class PerlinNoiseGenerator : BaseTerrainGenerator
    {
        private readonly PerlinNoise _perlinNoise;
        
        /// <summary>
        /// Creates a new Perlin noise terrain generator
        /// </summary>
        /// <param name="randomGenerator">Random number generator</param>
        /// <param name="logger">Logger service for performance metrics</param>
        public PerlinNoiseGenerator(IRandomGenerator randomGenerator, ISimpleLoggerService logger = null) : base(randomGenerator, logger)
        {
            _perlinNoise = new PerlinNoise();
        }
        
        /// <summary>
        /// Gets the name of this generation algorithm
        /// </summary>
        public override string GetAlgorithmName() => "perlin";
        
        /// <summary>
        /// Gets the default parameters for this algorithm
        /// </summary>
        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "scale", 0.1f },
                { "octaves", 4 },
                { "persistence", 0.5f },
                { "lacunarity", 2.0f },
                { "waterLevel", 0.3f },
                { "mountainLevel", 0.7f }
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
                var validParams = new[] { "scale", "octaves", "persistence", "lacunarity", "waterLevel", "mountainLevel" };
                foreach (var param in parameters.Keys)
                {
                    if (!Array.Exists(validParams, p => p.Equals(param, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"Unknown parameter '{param}' for Perlin noise generator");
                    }
                }
                
                // Validate specific parameter values
                if (parameters.ContainsKey("scale"))
                {
                    var scale = GetParameter<float>(parameters, "scale", 0.1f);
                    if (scale <= 0 || scale > 1.0f)
                        errors.Add("Scale must be between 0 and 1.0");
                }
                
                if (parameters.ContainsKey("octaves"))
                {
                    var octaves = GetParameter<int>(parameters, "octaves", 4);
                    if (octaves < 1 || octaves > 10)
                        errors.Add("Octaves must be between 1 and 10");
                }
                
                if (parameters.ContainsKey("persistence"))
                {
                    var persistence = GetParameter<float>(parameters, "persistence", 0.5f);
                    if (persistence < 0 || persistence > 1.0f)
                        errors.Add("Persistence must be between 0 and 1.0");
                }
                
                if (parameters.ContainsKey("lacunarity"))
                {
                    var lacunarity = GetParameter<float>(parameters, "lacunarity", 2.0f);
                    if (lacunarity < 1.0f || lacunarity > 4.0f)
                        errors.Add("Lacunarity must be between 1.0 and 4.0");
                }
                
                if (parameters.ContainsKey("waterLevel"))
                {
                    var waterLevel = GetParameter<float>(parameters, "waterLevel", 0.3f);
                    if (waterLevel < 0 || waterLevel > 1.0f)
                        errors.Add("Water level must be between 0 and 1.0");
                }
                
                if (parameters.ContainsKey("mountainLevel"))
                {
                    var mountainLevel = GetParameter<float>(parameters, "mountainLevel", 0.7f);
                    if (mountainLevel < 0 || mountainLevel > 1.0f)
                        errors.Add("Mountain level must be between 0 and 1.0");
                }
                
                // Check that waterLevel < mountainLevel
                if (parameters.ContainsKey("waterLevel") && parameters.ContainsKey("mountainLevel"))
                {
                    var waterLevel = GetParameter<float>(parameters, "waterLevel", 0.3f);
                    var mountainLevel = GetParameter<float>(parameters, "mountainLevel", 0.7f);
                    if (waterLevel >= mountainLevel)
                        errors.Add("Water level must be less than mountain level");
                }
            }
            
            return errors;
        }
        
        /// <summary>
        /// Generates terrain using Perlin noise algorithm
        /// </summary>
        protected override void GenerateTerrainInternal(TileMap tileMap, GenerationConfig config)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            _perlinNoise.SetSeed(_random.GetSeed());
            
            // Extract parameters with defaults
            var scale = GetParameter<float>(config.AlgorithmParameters, "scale", 0.1f);
            var octaves = GetParameter<int>(config.AlgorithmParameters, "octaves", 4);
            var persistence = GetParameter<float>(config.AlgorithmParameters, "persistence", 0.5f);
            var lacunarity = GetParameter<float>(config.AlgorithmParameters, "lacunarity", 2.0f);
            var waterLevel = GetParameter<float>(config.AlgorithmParameters, "waterLevel", 0.3f);
            var mountainLevel = GetParameter<float>(config.AlgorithmParameters, "mountainLevel", 0.7f);
            
            _logger?.LogInfo("Starting Perlin noise terrain generation", new {
                OperationId = operationId,
                Algorithm = "PerlinNoise",
                Scale = scale,
                Octaves = octaves,
                Persistence = persistence,
                Lacunarity = lacunarity,
                WaterLevel = waterLevel,
                MountainLevel = mountainLevel,
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TotalTiles = tileMap.Width * tileMap.Height,
                Seed = _random.GetSeed(),
                Operation = "PerlinNoiseTerrainGeneration"
            });
            
            // Log parameter validation
            var paramValidation = ValidateParameters(config.AlgorithmParameters);
            if (paramValidation.Any())
            {
                _logger?.LogWarning("Perlin noise parameter validation warnings", new {
                    OperationId = operationId,
                    ValidationWarnings = paramValidation,
                    WarningCount = paramValidation.Count
                });
            }
            
            // Initialize noise generation
            var noiseStopwatch = Stopwatch.StartNew();
            var terrainTypeStats = new Dictionary<TileType, int>();
            var noiseValueStats = new List<float>();
            
            // Log noise generation start
            _logger?.LogInfo("Starting noise value generation", new {
                OperationId = operationId,
                NoiseParameters = new { Scale = scale, Octaves = octaves, Persistence = persistence, Lacunarity = lacunarity },
                TilesToProcess = tileMap.Width * tileMap.Height
            });
            
            // Generate noise map with progress tracking
            int processedTiles = 0;
            int progressInterval = Math.Max(1, (tileMap.Width * tileMap.Height) / 10); // Log every 10%
            
            for (int x = 0; x < tileMap.Width; x++)
            {
                for (int y = 0; y < tileMap.Height; y++)
                {
                    float noiseValue = GenerateOctaveNoise(x, y, scale, octaves, persistence, lacunarity);
                    TileType tileType = DetermineTerrainType(noiseValue, waterLevel, mountainLevel, config.TerrainTypes);
                    tileMap.SetTile(x, y, tileType);
                    
                    // Track terrain type statistics
                    terrainTypeStats[tileType] = terrainTypeStats.GetValueOrDefault(tileType, 0) + 1;
                    noiseValueStats.Add(noiseValue);
                    
                    processedTiles++;
                    
                    // Log progress periodically
                    if (processedTiles % progressInterval == 0)
                    {
                        var progress = (double)processedTiles / (tileMap.Width * tileMap.Height);
                        _logger?.LogInfo($"Perlin noise generation progress: {progress:P1}", new {
                            OperationId = operationId,
                            ProcessedTiles = processedTiles,
                            TotalTiles = tileMap.Width * tileMap.Height,
                            Progress = progress,
                            ElapsedMs = noiseStopwatch.ElapsedMilliseconds
                        });
                    }
                }
            }
            
            noiseStopwatch.Stop();
            
            // Calculate noise statistics
            var noiseStats = new {
                MinValue = noiseValueStats.Min(),
                MaxValue = noiseValueStats.Max(),
                AverageValue = noiseValueStats.Average(),
                StandardDeviation = CalculateStandardDeviation(noiseValueStats)
            };
            
            _logger?.LogPerformance("PerlinNoise_NoiseGeneration", noiseStopwatch.Elapsed, new {
                OperationId = operationId,
                TilesGenerated = tileMap.Width * tileMap.Height,
                TilesPerSecond = (tileMap.Width * tileMap.Height) / noiseStopwatch.Elapsed.TotalSeconds,
                NoiseParameters = new { Scale = scale, Octaves = octaves, Persistence = persistence, Lacunarity = lacunarity },
                NoiseStatistics = noiseStats
            });
            
            // Log terrain type classification
            var classificationStopwatch = Stopwatch.StartNew();
            var terrainComposition = terrainTypeStats.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => new { 
                    Count = kvp.Value, 
                    Percentage = (kvp.Value * 100.0) / (tileMap.Width * tileMap.Height),
                    Density = (double)kvp.Value / (tileMap.Width * tileMap.Height)
                }
            );
            classificationStopwatch.Stop();
            
            _logger?.LogPerformance("PerlinNoise_TerrainClassification", classificationStopwatch.Elapsed, new {
                OperationId = operationId,
                TerrainTypes = terrainComposition.Keys.Count,
                ClassificationSpeed = (tileMap.Width * tileMap.Height) / classificationStopwatch.Elapsed.TotalSeconds
            });
            
            stopwatch.Stop();
            
            _logger?.LogGeneration(operationId, "PerlinNoiseTerrainGeneration", stopwatch.Elapsed, new {
                Algorithm = "PerlinNoise",
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TotalTiles = tileMap.Width * tileMap.Height,
                Parameters = new { Scale = scale, Octaves = octaves, Persistence = persistence, Lacunarity = lacunarity, WaterLevel = waterLevel, MountainLevel = mountainLevel },
                TerrainComposition = terrainComposition,
                NoiseStatistics = noiseStats,
                Seed = _random.GetSeed()
            });
            
            _logger?.LogInfo("Perlin noise terrain generation completed successfully", new {
                OperationId = operationId,
                TotalDurationMs = stopwatch.ElapsedMilliseconds,
                TerrainSize = $"{tileMap.Width}x{tileMap.Height}",
                TilesGenerated = tileMap.Width * tileMap.Height,
                GenerationRate = (tileMap.Width * tileMap.Height) / stopwatch.Elapsed.TotalSeconds,
                TerrainComposition = terrainComposition,
                NoiseStatistics = noiseStats
            });
        }
        
        /// <summary>
        /// Calculates standard deviation of noise values
        /// </summary>
        private double CalculateStandardDeviation(List<float> values)
        {
            if (values.Count == 0) return 0.0;
            
            var average = values.Average();
            var sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
            return Math.Sqrt(sumOfSquares / values.Count);
        }
        
        /// <summary>
        /// Generates octave noise for more natural terrain
        /// </summary>
        private float GenerateOctaveNoise(int x, int y, float scale, int octaves, float persistence, float lacunarity)
        {
            float value = 0;
            float amplitude = 1;
            float frequency = scale;
            float maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                value += _perlinNoise.Noise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return value / maxValue;
        }
        
        /// <summary>
        /// Determines terrain type based on noise value and thresholds
        /// </summary>
        private TileType DetermineTerrainType(float noiseValue, float waterLevel, float mountainLevel, List<string> terrainTypes)
        {
            // Normalize noise value to 0-1 range
            float normalizedValue = (noiseValue + 1) / 2;
            
            if (normalizedValue < waterLevel && terrainTypes.Contains("water"))
                return TileType.Water;
            else if (normalizedValue > mountainLevel && terrainTypes.Contains("stone"))
                return TileType.Stone;
            else if (normalizedValue > 0.6f && terrainTypes.Contains("grass"))
                return TileType.Grass;
            else if (terrainTypes.Contains("ground"))
                return TileType.Ground;
            else
                return TileType.Ground; // Default fallback
        }
        

    }
}
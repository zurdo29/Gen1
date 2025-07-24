using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Generators
{
    /// <summary>
    /// Simple test implementation of terrain generator for validation
    /// </summary>
    public class TestTerrainGenerator : BaseTerrainGenerator
    {
        /// <summary>
        /// Creates a new test terrain generator
        /// </summary>
        /// <param name="randomGenerator">Random number generator</param>
        public TestTerrainGenerator(IRandomGenerator randomGenerator) : base(randomGenerator)
        {
        }
        
        /// <summary>
        /// Gets the name of this generation algorithm
        /// </summary>
        /// <returns>Algorithm name</returns>
        public override string GetAlgorithmName()
        {
            return "test";
        }
        
        /// <summary>
        /// Gets the default parameters for this algorithm
        /// </summary>
        /// <returns>Dictionary of default parameter values</returns>
        public override Dictionary<string, object> GetDefaultParameters()
        {
            return new Dictionary<string, object>
            {
                { "fillPercentage", 0.4 },
                { "groundType", "ground" }
            };
        }
        
        /// <summary>
        /// Validates algorithm-specific parameters
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>List of validation error messages</returns>
        public override List<string> ValidateParameters(Dictionary<string, object> parameters)
        {
            var errors = base.ValidateParameters(parameters);
            
            if (parameters != null)
            {
                // Validate fill percentage
                if (parameters.ContainsKey("fillPercentage"))
                {
                    var fillPercentage = GetParameter<double>(parameters, "fillPercentage", 0.4);
                    if (fillPercentage < 0.0 || fillPercentage > 1.0)
                    {
                        errors.Add("fillPercentage must be between 0.0 and 1.0");
                    }
                }
            }
            
            return errors;
        }
        
        /// <summary>
        /// Generates simple test terrain
        /// </summary>
        /// <param name="tileMap">Tile map to populate</param>
        /// <param name="config">Generation configuration</param>
        protected override void GenerateTerrainInternal(TileMap tileMap, GenerationConfig config)
        {
            var fillPercentage = GetParameter<double>(config.AlgorithmParameters, "fillPercentage", 0.4);
            var groundType = GetParameter<string>(config.AlgorithmParameters, "groundType", "ground");
            var tileType = StringToTileType(groundType);
            
            // Fill randomly based on percentage
            for (int x = 1; x < tileMap.Width - 1; x++)
            {
                for (int y = 1; y < tileMap.Height - 1; y++)
                {
                    if (_random.NextDouble() < fillPercentage)
                    {
                        tileMap.SetTile(x, y, tileType);
                    }
                    else
                    {
                        tileMap.SetTile(x, y, TileType.Wall);
                    }
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Tests
{
    /// <summary>
    /// Mock generation manager for testing purposes
    /// </summary>
    public class MockGenerationManager : ProceduralMiniGameGenerator.Core.IGenerationManager
    {
        private readonly Dictionary<string, ITerrainGenerator> _terrainGenerators;
        private readonly Dictionary<string, ProceduralMiniGameGenerator.Core.IEntityPlacer> _entityPlacers;
        private int _currentSeed;

        public MockGenerationManager()
        {
            _terrainGenerators = new Dictionary<string, ITerrainGenerator>();
            _entityPlacers = new Dictionary<string, ProceduralMiniGameGenerator.Core.IEntityPlacer>();
            _currentSeed = Environment.TickCount;
        }

        public Level GenerateLevel(GenerationConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // Use a simple terrain generator for testing
            var randomGenerator = new RandomGenerator();
            var terrainGenerator = new PerlinNoiseGenerator(randomGenerator);
            var terrain = terrainGenerator.GenerateTerrain(config, config.Seed);

            // Use a simple entity placer for testing
            var entityPlacer = new EntityPlacer(randomGenerator);
            var entities = entityPlacer.PlaceEntities(terrain, config, config.Seed);

            // Assemble the level
            var levelAssembler = new LevelAssembler();
            var level = levelAssembler.AssembleLevel(terrain, entities, config);

            return level;
        }

        public void SetSeed(int seed)
        {
            _currentSeed = seed;
        }

        public void RegisterGenerationAlgorithm(string name, ITerrainGenerator generator)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Algorithm name cannot be null or empty", nameof(name));
            
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));

            _terrainGenerators[name.ToLower()] = generator;
        }

        public void RegisterEntityPlacer(string name, ProceduralMiniGameGenerator.Core.IEntityPlacer placer)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Placer name cannot be null or empty", nameof(name));
            
            if (placer == null)
                throw new ArgumentNullException(nameof(placer));

            _entityPlacers[name.ToLower()] = placer;
        }

        public List<string> GetAvailableAlgorithms()
        {
            return new List<string>(_terrainGenerators.Keys);
        }

        public List<string> GetAvailablePlacementStrategies()
        {
            return new List<string>(_entityPlacers.Keys);
        }

        public ValidationResult ValidateGenerationConfig(GenerationConfig config)
        {
            var result = new ValidationResult();
            
            if (config == null)
            {
                result.Errors.Add("Configuration cannot be null");
                return result;
            }

            if (config.Width <= 0 || config.Height <= 0)
            {
                result.Errors.Add("Level dimensions must be positive");
            }

            if (string.IsNullOrEmpty(config.GenerationAlgorithm))
            {
                result.Errors.Add("Generation algorithm must be specified");
            }

            return result;
        }
    }
}
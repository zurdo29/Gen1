using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Tests.Generators
{
    /// <summary>
    /// Test runner for entity placement comprehensive tests
    /// Validates the implementation against requirements 3.3 and 3.4
    /// </summary>
    public class EntityPlacementTestRunner
    {
        private readonly IRandomGenerator _random;
        private readonly EntityPlacer _entityPlacer;
        
        public EntityPlacementTestRunner()
        {
            _random = new RandomGenerator();
            _entityPlacer = new EntityPlacer(_random);
        }
        
        /// <summary>
        /// Runs all comprehensive entity placement tests
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("=== Entity Placement Comprehensive Tests ===");
            Console.WriteLine("Testing Requirements 3.3 and 3.4");
            Console.WriteLine();
            
            try
            {
                // Test placement in various terrain types (Requirement 3.3)
                TestTerrainTypePlacement();
                
                // Test valid position verification (Requirement 3.4)
                TestValidPositionVerification();
                
                // Test impossible placement scenarios (Requirement 3.4)
                TestImpossiblePlacementScenarios();
                
                Console.WriteLine("✅ All entity placement tests passed successfully!");
                Console.WriteLine("Requirements 3.3 and 3.4 are fully satisfied.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                throw;
            }
        }
        
        private void TestTerrainTypePlacement()
        {
            Console.WriteLine("🧪 Testing placement in various terrain types...");
            
            // Test all walkable terrain types
            var walkableTerrainTypes = new[] { TileType.Ground, TileType.Grass, TileType.Sand };
            
            foreach (var terrainType in walkableTerrainTypes)
            {
                var terrain = CreateUniformTerrain(6, 6, terrainType);
                var config = CreateBasicEntityConfig(EntityType.Enemy, 3);
                
                var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
                var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
                
                if (enemyEntities.Count == 0)
                    throw new Exception($"Failed to place enemies on {terrainType} terrain");
                
                foreach (var enemy in enemyEntities)
                {
                    if (terrain.GetTile((int)enemy.Position.X, (int)enemy.Position.Y) != terrainType)
                        throw new Exception($"Enemy not placed on correct {terrainType} terrain");
                }
                
                Console.WriteLine($"  ✅ Successfully placed entities on {terrainType} terrain");
            }
            
            // Test non-walkable terrain types
            var nonWalkableTerrainTypes = new[] { TileType.Wall, TileType.Water };
            
            foreach (var terrainType in nonWalkableTerrainTypes)
            {
                var terrain = CreateUniformTerrain(5, 5, terrainType);
                var config = CreateBasicEntityConfig(EntityType.Item, 4);
                
                var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
                var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
                
                if (itemEntities.Count != 0)
                    throw new Exception($"Incorrectly placed items on non-walkable {terrainType} terrain");
                
                Console.WriteLine($"  ✅ Correctly avoided placing entities on {terrainType} terrain");
            }
            
            Console.WriteLine("✅ Terrain type placement tests completed");
            Console.WriteLine();
        }
        
        private void TestValidPositionVerification()
        {
            Console.WriteLine("🧪 Testing valid position verification...");
            
            var terrain = CreateBorderedTerrain(10, 10);
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 6, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = 4, PlacementStrategy = "random" }
                }
            };
            
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Verify all positions are valid
            foreach (var entity in entities)
            {
                var otherEntities = entities.Where(e => e != entity).ToList();
                bool isValid = _entityPlacer.IsValidPosition(entity.Position, terrain, otherEntities);
                
                if (!isValid)
                    throw new Exception($"Entity at {entity.Position} is in an invalid position");
                
                // Check bounds
                if (entity.Position.X < 0 || entity.Position.X >= terrain.Width ||
                    entity.Position.Y < 0 || entity.Position.Y >= terrain.Height)
                    throw new Exception($"Entity at {entity.Position} is out of bounds");
                
                // Check walkable terrain
                if (!terrain.IsWalkable((int)entity.Position.X, (int)entity.Position.Y))
                    throw new Exception($"Entity at {entity.Position} is on non-walkable terrain");
            }
            
            // Test minimum distance between entities
            var allEntities = entities.ToList();
            for (int i = 0; i < allEntities.Count - 1; i++)
            {
                for (int j = i + 1; j < allEntities.Count; j++)
                {
                    var distance = Vector2.Distance(allEntities[i].Position, allEntities[j].Position);
                    if (distance < 1.0f)
                        throw new Exception($"Entities too close: {distance} < 1.0");
                }
            }
            
            Console.WriteLine("  ✅ All entities placed in valid positions");
            Console.WriteLine("  ✅ Minimum distance constraints respected");
            Console.WriteLine("✅ Valid position verification tests completed");
            Console.WriteLine();
        }
        
        private void TestImpossiblePlacementScenarios()
        {
            Console.WriteLine("🧪 Testing impossible placement scenarios...");
            
            // Test completely blocked terrain
            var blockedTerrain = CreateUniformTerrain(4, 4, TileType.Wall);
            var blockedConfig = CreateBasicEntityConfig(EntityType.Enemy, 5);
            
            var blockedEntities = _entityPlacer.PlaceEntities(blockedTerrain, blockedConfig, 12345);
            if (blockedEntities.Count != 0)
                throw new Exception("Should place no entities on completely blocked terrain");
            
            Console.WriteLine("  ✅ Handled completely blocked terrain correctly");
            
            // Test single walkable tile
            var singleTileTerrain = CreateUniformTerrain(5, 5, TileType.Wall);
            singleTileTerrain.SetTile(2, 2, TileType.Ground);
            
            var singleTileConfig = new GenerationConfig
            {
                Width = 5,
                Height = 5,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 3, PlacementStrategy = "random" }
                }
            };
            
            var singleTileEntities = _entityPlacer.PlaceEntities(singleTileTerrain, singleTileConfig, 12345);
            var playerEntities = singleTileEntities.Where(e => e.Type == EntityType.Player).ToList();
            var enemyEntities = singleTileEntities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            if (playerEntities.Count != 1)
                throw new Exception("Should place exactly one player on single tile");
            if (enemyEntities.Count != 0)
                throw new Exception("Should place no enemies when only one tile available");
            if (playerEntities[0].Position != new Vector2(2, 2))
                throw new Exception("Player should be on the only walkable tile");
            
            Console.WriteLine("  ✅ Handled single walkable tile correctly");
            
            // Test impossible distance constraints
            var constrainedTerrain = CreateUniformTerrain(6, 6, TileType.Ground);
            var constrainedConfig = new GenerationConfig
            {
                Width = 6,
                Height = 6,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Enemy, 
                        Count = 20,
                        PlacementStrategy = "random",
                        MinDistance = 5.0f // Impossible in 6x6 terrain
                    }
                }
            };
            
            var constrainedEntities = _entityPlacer.PlaceEntities(constrainedTerrain, constrainedConfig, 12345);
            var constrainedEnemies = constrainedEntities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            if (constrainedEnemies.Count >= 20)
                throw new Exception("Should place fewer entities with impossible constraints");
            
            // Verify constraints are still respected
            for (int i = 0; i < constrainedEnemies.Count - 1; i++)
            {
                for (int j = i + 1; j < constrainedEnemies.Count; j++)
                {
                    var distance = Vector2.Distance(constrainedEnemies[i].Position, constrainedEnemies[j].Position);
                    if (distance < 5.0f)
                        throw new Exception($"Distance constraint violated: {distance} < 5.0");
                }
            }
            
            Console.WriteLine("  ✅ Handled impossible distance constraints correctly");
            
            // Test excessive entity requests
            var smallTerrain = CreateUniformTerrain(4, 4, TileType.Ground);
            var excessiveConfig = CreateBasicEntityConfig(EntityType.Enemy, 50);
            
            var excessiveEntities = _entityPlacer.PlaceEntities(smallTerrain, excessiveConfig, 12345);
            var excessiveEnemies = excessiveEntities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            if (excessiveEnemies.Count >= 50)
                throw new Exception("Should place fewer entities than terrain capacity");
            if (excessiveEnemies.Count > 16)
                throw new Exception("Should not exceed terrain capacity");
            
            Console.WriteLine("  ✅ Handled excessive entity requests correctly");
            
            // Test zero and negative entity counts
            var zeroConfig = new GenerationConfig
            {
                Width = 6,
                Height = 6,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 0, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = -5, PlacementStrategy = "random" }
                }
            };
            
            var zeroEntities = _entityPlacer.PlaceEntities(constrainedTerrain, zeroConfig, 12345);
            var zeroPlayers = zeroEntities.Where(e => e.Type == EntityType.Player).ToList();
            var zeroEnemies = zeroEntities.Where(e => e.Type == EntityType.Enemy).ToList();
            var zeroItems = zeroEntities.Where(e => e.Type == EntityType.Item).ToList();
            
            if (zeroPlayers.Count != 1)
                throw new Exception("Should place exactly one player with zero/negative counts");
            if (zeroEnemies.Count != 0)
                throw new Exception("Should place no enemies when count is 0");
            if (zeroItems.Count != 0)
                throw new Exception("Should place no items when count is negative");
            
            Console.WriteLine("  ✅ Handled zero and negative entity counts correctly");
            
            Console.WriteLine("✅ Impossible placement scenario tests completed");
            Console.WriteLine();
        }
        
        #region Helper Methods
        
        private TileMap CreateUniformTerrain(int width, int height, TileType tileType)
        {
            var terrain = new TileMap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    terrain.SetTile(x, y, tileType);
                }
            }
            return terrain;
        }
        
        private TileMap CreateBorderedTerrain(int width, int height)
        {
            var terrain = new TileMap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                        terrain.SetTile(x, y, TileType.Wall);
                    else
                        terrain.SetTile(x, y, TileType.Ground);
                }
            }
            return terrain;
        }
        
        private GenerationConfig CreateBasicEntityConfig(EntityType entityType, int count)
        {
            return new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = entityType, 
                        Count = count, 
                        PlacementStrategy = "random" 
                    }
                }
            };
        }
        
        #endregion
    }
}
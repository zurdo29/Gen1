using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Tests.Generators
{
    /// <summary>
    /// Comprehensive unit tests for entity placement covering all requirements:
    /// - Test placement in various terrain types
    /// - Verify entities are placed in valid positions
    /// - Test handling of impossible placement scenarios
    /// Requirements: 3.3, 3.4
    /// </summary>
    [TestClass]
    public class EntityPlacementComprehensiveTests
    {
        private IRandomGenerator _random = null!;
        private EntityPlacer _entityPlacer = null!;
        
        [TestInitialize]
        public void Setup()
        {
            _random = new RandomGenerator();
            _entityPlacer = new EntityPlacer(_random);
        }
        
        #region Terrain Type Placement Tests (Requirement 3.3)
        
        [TestMethod]
        public void PlaceEntities_OnAllWalkableTerrainTypes_PlacesEntitiesCorrectly()
        {
            // Test all walkable terrain types: Ground, Grass, Sand
            var walkableTerrainTypes = new[] { TileType.Ground, TileType.Grass, TileType.Sand };
            
            foreach (var terrainType in walkableTerrainTypes)
            {
                // Arrange
                var terrain = CreateUniformTerrain(6, 6, terrainType);
                var config = CreateBasicEntityConfig(EntityType.Enemy, 3);
                
                // Act
                var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
                
                // Assert
                var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
                Assert.IsTrue(enemyEntities.Count > 0, $"Should place enemies on {terrainType} terrain");
                
                foreach (var enemy in enemyEntities)
                {
                    Assert.AreEqual(terrainType, terrain.GetTile((int)enemy.Position.X, (int)enemy.Position.Y),
                        $"Enemy should be placed on {terrainType} tile");
                    Assert.IsTrue(terrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                        $"Enemy should be on walkable {terrainType} terrain");
                }
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnAllNonWalkableTerrainTypes_PlacesNoEntities()
        {
            // Test all non-walkable terrain types: Wall, Water
            var nonWalkableTerrainTypes = new[] { TileType.Wall, TileType.Water };
            
            foreach (var terrainType in nonWalkableTerrainTypes)
            {
                // Arrange
                var terrain = CreateUniformTerrain(5, 5, terrainType);
                var config = CreateBasicEntityConfig(EntityType.Item, 4);
                
                // Act
                var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
                
                // Assert
                var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
                Assert.AreEqual(0, itemEntities.Count, 
                    $"Should place no items on non-walkable {terrainType} terrain");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnMixedTerrainWithWalkableAndNonWalkable_PlacesOnlyOnWalkable()
        {
            // Arrange - Create checkerboard pattern of walkable and non-walkable
            var terrain = new TileMap(8, 8);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    // Checkerboard: walkable on even sum, non-walkable on odd sum
                    terrain.SetTile(x, y, (x + y) % 2 == 0 ? TileType.Ground : TileType.Wall);
                }
            }
            
            var config = CreateBasicEntityConfig(EntityType.PowerUp, 6);
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var powerUpEntities = entities.Where(e => e.Type == EntityType.PowerUp).ToList();
            
            foreach (var powerUp in powerUpEntities)
            {
                Assert.IsTrue(terrain.IsWalkable((int)powerUp.Position.X, (int)powerUp.Position.Y),
                    "PowerUp should only be placed on walkable tiles in mixed terrain");
                Assert.AreEqual(TileType.Ground, terrain.GetTile((int)powerUp.Position.X, (int)powerUp.Position.Y),
                    "PowerUp should be on Ground tiles (walkable) in checkerboard pattern");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnComplexMixedTerrain_RespectsTerrainConstraints()
        {
            // Arrange - Create complex terrain with multiple types
            var terrain = new TileMap(10, 10);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (x == 0 || x == 9 || y == 0 || y == 9)
                        terrain.SetTile(x, y, TileType.Wall); // Border walls
                    else if (x == 5 && y >= 2 && y <= 7)
                        terrain.SetTile(x, y, TileType.Water); // Water barrier
                    else if (x < 5)
                        terrain.SetTile(x, y, TileType.Ground); // Left side ground
                    else
                        terrain.SetTile(x, y, TileType.Grass); // Right side grass
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 5, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = 3, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            foreach (var entity in entities)
            {
                var tileType = terrain.GetTile((int)entity.Position.X, (int)entity.Position.Y);
                Assert.IsTrue(terrain.IsWalkable((int)entity.Position.X, (int)entity.Position.Y),
                    $"Entity should be on walkable terrain, found on {tileType}");
                Assert.IsTrue(tileType == TileType.Ground || tileType == TileType.Grass,
                    $"Entity should be on Ground or Grass, found on {tileType}");
            }
        }
        
        #endregion
        
        #region Valid Position Verification Tests (Requirement 3.4)
        
        [TestMethod]
        public void PlaceEntities_VerifiesAllPositionsAreValid_UsingIsValidPosition()
        {
            // Arrange
            var terrain = CreateBorderedTerrain(12, 12);
            var config = new GenerationConfig
            {
                Width = 12,
                Height = 12,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 8, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = 5, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.PowerUp, Count = 3, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            foreach (var entity in entities)
            {
                // Create list of other entities for validation
                var otherEntities = entities.Where(e => e != entity).ToList();
                
                // Verify position is valid according to IsValidPosition method
                bool isValid = _entityPlacer.IsValidPosition(entity.Position, terrain, otherEntities);
                Assert.IsTrue(isValid, 
                    $"Entity of type {entity.Type} at position {entity.Position} should be in a valid position");
                
                // Additional specific checks
                Assert.IsTrue(entity.Position.X >= 0 && entity.Position.X < terrain.Width,
                    $"Entity X position {entity.Position.X} should be within terrain bounds [0, {terrain.Width})");
                Assert.IsTrue(entity.Position.Y >= 0 && entity.Position.Y < terrain.Height,
                    $"Entity Y position {entity.Position.Y} should be within terrain bounds [0, {terrain.Height})");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_MaintainsMinimumDistanceBetweenEntities()
        {
            // Arrange
            var terrain = CreateUniformTerrain(8, 8, TileType.Ground);
            var config = CreateBasicEntityConfig(EntityType.Enemy, 6);
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var allEntities = entities.ToList();
            
            // Check minimum distance between all entity pairs
            for (int i = 0; i < allEntities.Count - 1; i++)
            {
                for (int j = i + 1; j < allEntities.Count; j++)
                {
                    var distance = Vector2.Distance(allEntities[i].Position, allEntities[j].Position);
                    Assert.IsTrue(distance >= 1.0f, 
                        $"Entities should maintain minimum distance of 1.0, found {distance} between " +
                        $"{allEntities[i].Type} at {allEntities[i].Position} and " +
                        $"{allEntities[j].Type} at {allEntities[j].Position}");
                }
            }
        }
        
        [TestMethod]
        public void PlaceEntities_RespectsEntitySpecificPlacementRules()
        {
            // Arrange
            var terrain = CreateBorderedTerrain(10, 10);
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 4, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            Assert.IsNotNull(playerEntity, "Should have a player entity");
            
            // Verify enemies respect their specific placement rules (minimum distance from player)
            foreach (var enemy in enemyEntities)
            {
                var distanceFromPlayer = Vector2.Distance(enemy.Position, playerEntity.Position);
                Assert.IsTrue(distanceFromPlayer >= 3.0f, 
                    $"Enemy should maintain minimum distance of 3.0 from player, found {distanceFromPlayer}");
            }
        }
        
        [TestMethod]
        public void IsValidPosition_WithVariousScenarios_ReturnsCorrectResults()
        {
            // Arrange
            var terrain = CreateBorderedTerrain(6, 6);
            var existingEntity = new EnemyEntity { Position = new Vector2(3, 3) };
            var existingEntities = new List<Entity> { existingEntity };
            
            var testCases = new[]
            {
                // Valid positions
                new { Position = new Vector2(1, 1), Expected = true, Description = "Valid walkable position" },
                new { Position = new Vector2(5, 1), Expected = true, Description = "Valid position far from existing entity" },
                
                // Invalid positions - out of bounds
                new { Position = new Vector2(-1, 3), Expected = false, Description = "Out of bounds - negative X" },
                new { Position = new Vector2(6, 3), Expected = false, Description = "Out of bounds - X too large" },
                new { Position = new Vector2(3, -1), Expected = false, Description = "Out of bounds - negative Y" },
                new { Position = new Vector2(3, 6), Expected = false, Description = "Out of bounds - Y too large" },
                
                // Invalid positions - non-walkable terrain
                new { Position = new Vector2(0, 0), Expected = false, Description = "Wall tile (border)" },
                new { Position = new Vector2(0, 3), Expected = false, Description = "Wall tile (left border)" },
                new { Position = new Vector2(5, 0), Expected = false, Description = "Wall tile (top border)" },
                
                // Invalid positions - too close to existing entity
                new { Position = new Vector2(3, 3), Expected = false, Description = "Same position as existing entity" },
                new { Position = new Vector2(3.5f, 3), Expected = false, Description = "Too close to existing entity" },
                new { Position = new Vector2(3, 3.9f), Expected = false, Description = "Just under minimum distance" }
            };
            
            // Act & Assert
            foreach (var testCase in testCases)
            {
                var result = _entityPlacer.IsValidPosition(testCase.Position, terrain, existingEntities);
                Assert.AreEqual(testCase.Expected, result, 
                    $"IsValidPosition failed for: {testCase.Description} at position {testCase.Position}");
            }
        }
        
        #endregion
        
        #region Impossible Placement Scenario Tests (Requirement 3.4)
        
        [TestMethod]
        public void PlaceEntities_WithCompletelyBlockedTerrain_HandlesGracefully()
        {
            // Arrange - All tiles are walls
            var terrain = CreateUniformTerrain(4, 4, TileType.Wall);
            var config = CreateBasicEntityConfig(EntityType.Enemy, 5);
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            Assert.IsNotNull(entities, "Should return a list even with completely blocked terrain");
            Assert.AreEqual(0, entities.Count, "Should place no entities on completely blocked terrain");
        }
        
        [TestMethod]
        public void PlaceEntities_WithSingleAvailableSpace_PlacesOnlyPlayer()
        {
            // Arrange - Only one walkable tile
            var terrain = CreateUniformTerrain(5, 5, TileType.Wall);
            terrain.SetTile(2, 2, TileType.Ground); // Single walkable tile
            
            var config = new GenerationConfig
            {
                Width = 5,
                Height = 5,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 3, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = 2, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var playerEntities = entities.Where(e => e.Type == EntityType.Player).ToList();
            var otherEntities = entities.Where(e => e.Type != EntityType.Player).ToList();
            
            Assert.AreEqual(1, playerEntities.Count, "Should place exactly one player");
            Assert.AreEqual(0, otherEntities.Count, "Should place no other entities when only one space available");
            Assert.AreEqual(new Vector2(2, 2), playerEntities[0].Position, "Player should be on the only walkable tile");
        }
        
        [TestMethod]
        public void PlaceEntities_WithImpossibleDistanceConstraints_PlacesFewerEntities()
        {
            // Arrange - Constraints that cannot be satisfied
            var terrain = CreateUniformTerrain(6, 6, TileType.Ground);
            var config = new GenerationConfig
            {
                Width = 6,
                Height = 6,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Enemy, 
                        Count = 20, // Request many
                        PlacementStrategy = "random",
                        MinDistance = 5.0f // But require impossible distance in 6x6 terrain
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count < 20, "Should place fewer enemies than requested");
            Assert.IsTrue(enemyEntities.Count <= 2, "Should place very few enemies with impossible constraints");
            
            // Verify constraints are still respected for placed entities
            for (int i = 0; i < enemyEntities.Count - 1; i++)
            {
                for (int j = i + 1; j < enemyEntities.Count; j++)
                {
                    var distance = Vector2.Distance(enemyEntities[i].Position, enemyEntities[j].Position);
                    Assert.IsTrue(distance >= 5.0f, 
                        $"Placed enemies should still respect minimum distance constraint, found {distance}");
                }
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithConflictingPlayerDistanceConstraints_HandlesGracefully()
        {
            // Arrange - Conflicting constraints: must be close to player but far from each other
            var terrain = CreateUniformTerrain(8, 8, TileType.Ground);
            var config = new GenerationConfig
            {
                Width = 8,
                Height = 8,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Item, 
                        Count = 10,
                        PlacementStrategy = "random",
                        MaxDistanceFromPlayer = 2.0f, // Must be close to player
                        MinDistance = 4.0f // But far from each other - impossible!
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            Assert.IsNotNull(entities, "Should handle conflicting constraints without crashing");
            
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            
            Assert.IsNotNull(playerEntity, "Should still place a player");
            Assert.IsTrue(itemEntities.Count < 10, "Should place fewer items due to conflicting constraints");
            Assert.IsTrue(itemEntities.Count >= 0, "Should not crash with conflicting constraints");
            
            // Verify any placed items still respect the constraints that can be satisfied
            foreach (var item in itemEntities)
            {
                var distanceFromPlayer = Vector2.Distance(playerEntity.Position, item.Position);
                Assert.IsTrue(distanceFromPlayer <= 2.0f, 
                    $"Item should respect max distance from player constraint, found {distanceFromPlayer}");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithExcessiveEntityRequests_PlacesMaximumPossible()
        {
            // Arrange - Request more entities than terrain can hold
            var terrain = CreateUniformTerrain(4, 4, TileType.Ground); // Only 16 tiles
            var config = CreateBasicEntityConfig(EntityType.Enemy, 50); // Request 50 entities
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count < 50, "Should place fewer enemies than requested");
            Assert.IsTrue(enemyEntities.Count <= 16, "Should not exceed terrain capacity");
            Assert.IsTrue(enemyEntities.Count >= 0, "Should handle excessive requests gracefully");
            
            // Verify all placed entities are valid
            foreach (var enemy in enemyEntities)
            {
                Assert.IsTrue(terrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                    "All placed enemies should be on walkable terrain");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithDisconnectedTerrainAreas_PlacesInAccessibleAreas()
        {
            // Arrange - Create terrain with separate disconnected areas
            var terrain = CreateUniformTerrain(11, 11, TileType.Wall);
            
            // Create three separate walkable areas
            // Area 1: Top-left corner
            for (int x = 1; x <= 3; x++)
                for (int y = 1; y <= 3; y++)
                    terrain.SetTile(x, y, TileType.Ground);
            
            // Area 2: Top-right corner  
            for (int x = 7; x <= 9; x++)
                for (int y = 1; y <= 3; y++)
                    terrain.SetTile(x, y, TileType.Grass);
            
            // Area 3: Bottom-center
            for (int x = 4; x <= 6; x++)
                for (int y = 7; y <= 9; y++)
                    terrain.SetTile(x, y, TileType.Sand);
            
            var config = CreateBasicEntityConfig(EntityType.Enemy, 8);
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            Assert.IsNotNull(playerEntity, "Should place a player in one of the disconnected areas");
            
            // All entities should be on walkable terrain
            foreach (var entity in entities)
            {
                Assert.IsTrue(terrain.IsWalkable((int)entity.Position.X, (int)entity.Position.Y),
                    $"Entity at {entity.Position} should be on walkable terrain");
                
                var tileType = terrain.GetTile((int)entity.Position.X, (int)entity.Position.Y);
                Assert.IsTrue(tileType == TileType.Ground || tileType == TileType.Grass || tileType == TileType.Sand,
                    $"Entity should be on one of the walkable areas, found on {tileType}");
            }
            
            // Should handle disconnected areas gracefully
            Assert.IsTrue(enemyEntities.Count >= 0, "Should handle disconnected terrain without errors");
        }
        
        [TestMethod]
        public void PlaceEntities_WithZeroAndNegativeEntityCounts_HandlesGracefully()
        {
            // Arrange
            var terrain = CreateUniformTerrain(6, 6, TileType.Ground);
            var config = new GenerationConfig
            {
                Width = 6,
                Height = 6,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 0, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = -5, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.PowerUp, Count = -1, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var playerEntities = entities.Where(e => e.Type == EntityType.Player).ToList();
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            var powerUpEntities = entities.Where(e => e.Type == EntityType.PowerUp).ToList();
            
            Assert.AreEqual(1, playerEntities.Count, "Should place exactly one player");
            Assert.AreEqual(0, enemyEntities.Count, "Should place no enemies when count is 0");
            Assert.AreEqual(0, itemEntities.Count, "Should place no items when count is negative");
            Assert.AreEqual(0, powerUpEntities.Count, "Should place no power-ups when count is negative");
        }
        
        #endregion
        
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
                    // Create walls on borders, ground in interior
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
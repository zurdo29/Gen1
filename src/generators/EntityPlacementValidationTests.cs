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
    /// Comprehensive unit tests for entity placement validation
    /// Tests placement in various terrain types, valid positions, and impossible scenarios
    /// </summary>
    [TestClass]
    public class EntityPlacementValidationTests
    {
        private IRandomGenerator _random = null!;
        private EntityPlacer _entityPlacer = null!;
        
        [TestInitialize]
        public void Setup()
        {
            _random = new RandomGenerator();
            _entityPlacer = new EntityPlacer(_random);
        }
        
        #region Terrain Type Placement Tests
        
        [TestMethod]
        public void PlaceEntities_OnGroundTerrain_PlacesAllEntitiesOnWalkableTiles()
        {
            // Arrange - Create terrain with only ground tiles
            var terrain = CreateUniformTerrain(8, 8, TileType.Ground);
            var config = CreateEntityConfig(EntityType.Enemy, 5, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count > 0, "Should place enemies on ground terrain");
            
            foreach (var enemy in enemyEntities)
            {
                Assert.IsTrue(terrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                    "All enemies should be placed on walkable ground tiles");
                Assert.AreEqual(TileType.Ground, terrain.GetTile((int)enemy.Position.X, (int)enemy.Position.Y));
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnGrassTerrain_PlacesAllEntitiesOnWalkableTiles()
        {
            // Arrange - Create terrain with only grass tiles
            var terrain = CreateUniformTerrain(6, 6, TileType.Grass);
            var config = CreateEntityConfig(EntityType.Item, 3, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            Assert.IsTrue(itemEntities.Count > 0, "Should place items on grass terrain");
            
            foreach (var item in itemEntities)
            {
                Assert.IsTrue(terrain.IsWalkable((int)item.Position.X, (int)item.Position.Y),
                    "All items should be placed on walkable grass tiles");
                Assert.AreEqual(TileType.Grass, terrain.GetTile((int)item.Position.X, (int)item.Position.Y));
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnSandTerrain_PlacesAllEntitiesOnWalkableTiles()
        {
            // Arrange - Create terrain with only sand tiles
            var terrain = CreateUniformTerrain(7, 7, TileType.Sand);
            var config = CreateEntityConfig(EntityType.PowerUp, 4, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var powerUpEntities = entities.Where(e => e.Type == EntityType.PowerUp).ToList();
            Assert.IsTrue(powerUpEntities.Count > 0, "Should place power-ups on sand terrain");
            
            foreach (var powerUp in powerUpEntities)
            {
                Assert.IsTrue(terrain.IsWalkable((int)powerUp.Position.X, (int)powerUp.Position.Y),
                    "All power-ups should be placed on walkable sand tiles");
                Assert.AreEqual(TileType.Sand, terrain.GetTile((int)powerUp.Position.X, (int)powerUp.Position.Y));
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnMixedWalkableTerrain_PlacesOnlyOnWalkableTiles()
        {
            // Arrange - Create mixed terrain with different walkable types
            var terrain = new TileMap(8, 8);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    // Create pattern: Ground, Grass, Sand, repeat
                    var pattern = (x + y) % 3;
                    var tileType = pattern switch
                    {
                        0 => TileType.Ground,
                        1 => TileType.Grass,
                        _ => TileType.Sand
                    };
                    terrain.SetTile(x, y, tileType);
                }
            }
            
            var config = CreateEntityConfig(EntityType.NPC, 6, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var npcEntities = entities.Where(e => e.Type == EntityType.NPC).ToList();
            
            foreach (var npc in npcEntities)
            {
                var tileType = terrain.GetTile((int)npc.Position.X, (int)npc.Position.Y);
                Assert.IsTrue(terrain.IsWalkable((int)npc.Position.X, (int)npc.Position.Y),
                    $"NPC should be placed on walkable terrain, found on {tileType}");
                Assert.IsTrue(tileType == TileType.Ground || tileType == TileType.Grass || tileType == TileType.Sand,
                    "NPC should be on one of the walkable tile types");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnNonWalkableTerrain_PlacesNoEntities()
        {
            // Arrange - Create terrain with only non-walkable tiles
            var terrain = CreateUniformTerrain(5, 5, TileType.Wall);
            var config = CreateEntityConfig(EntityType.Enemy, 3, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            Assert.IsNotNull(entities, "Should return a list even with no walkable terrain");
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.AreEqual(0, enemyEntities.Count, "Should place no enemies on non-walkable terrain");
        }
        
        [TestMethod]
        public void PlaceEntities_OnWaterTerrain_PlacesNoEntities()
        {
            // Arrange - Create terrain with only water (non-walkable)
            var terrain = CreateUniformTerrain(6, 6, TileType.Water);
            var config = CreateEntityConfig(EntityType.Item, 4, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            Assert.AreEqual(0, itemEntities.Count, "Should place no items on water terrain");
        }
        
        #endregion
        
        #region Valid Position Verification Tests
        
        [TestMethod]
        public void IsValidPosition_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var terrain = CreateUniformTerrain(5, 5, TileType.Ground);
            var position = new Vector2(2, 2);
            var existingEntities = new List<Entity>();
            
            // Act
            var isValid = _entityPlacer.IsValidPosition(position, terrain, existingEntities);
            
            // Assert
            Assert.IsTrue(isValid, "Position on walkable terrain should be valid");
        }
        
        [TestMethod]
        public void IsValidPosition_OnNonWalkableTerrain_ReturnsFalse()
        {
            // Arrange
            var terrain = CreateUniformTerrain(5, 5, TileType.Wall);
            var position = new Vector2(2, 2);
            var existingEntities = new List<Entity>();
            
            // Act
            var isValid = _entityPlacer.IsValidPosition(position, terrain, existingEntities);
            
            // Assert
            Assert.IsFalse(isValid, "Position on non-walkable terrain should be invalid");
        }
        
        [TestMethod]
        public void IsValidPosition_OutOfBounds_ReturnsFalse()
        {
            // Arrange
            var terrain = CreateUniformTerrain(5, 5, TileType.Ground);
            var existingEntities = new List<Entity>();
            
            var outOfBoundsPositions = new[]
            {
                new Vector2(-1, 2),  // Left boundary
                new Vector2(5, 2),   // Right boundary
                new Vector2(2, -1),  // Top boundary
                new Vector2(2, 5),   // Bottom boundary
                new Vector2(-1, -1), // Corner
                new Vector2(10, 10)  // Far out of bounds
            };
            
            // Act & Assert
            foreach (var position in outOfBoundsPositions)
            {
                var isValid = _entityPlacer.IsValidPosition(position, terrain, existingEntities);
                Assert.IsFalse(isValid, $"Out of bounds position {position} should be invalid");
            }
        }
        
        [TestMethod]
        public void IsValidPosition_NearExistingEntity_ReturnsFalse()
        {
            // Arrange
            var terrain = CreateUniformTerrain(5, 5, TileType.Ground);
            var existingEntity = new EnemyEntity { Position = new Vector2(2, 2) };
            var existingEntities = new List<Entity> { existingEntity };
            
            var tooClosePositions = new[]
            {
                new Vector2(2, 2),     // Same position
                new Vector2(2.5f, 2),  // Very close
                new Vector2(2, 2.9f)   // Just under 1.0 distance
            };
            
            // Act & Assert
            foreach (var position in tooClosePositions)
            {
                var isValid = _entityPlacer.IsValidPosition(position, terrain, existingEntities);
                Assert.IsFalse(isValid, $"Position {position} too close to existing entity should be invalid");
            }
        }
        
        [TestMethod]
        public void IsValidPosition_FarFromExistingEntity_ReturnsTrue()
        {
            // Arrange
            var terrain = CreateUniformTerrain(5, 5, TileType.Ground);
            var existingEntity = new EnemyEntity { Position = new Vector2(1, 1) };
            var existingEntities = new List<Entity> { existingEntity };
            
            var farPositions = new[]
            {
                new Vector2(3, 3),   // Diagonal distance > 1.0
                new Vector2(4, 1),   // Horizontal distance > 1.0
                new Vector2(1, 4)    // Vertical distance > 1.0
            };
            
            // Act & Assert
            foreach (var position in farPositions)
            {
                var isValid = _entityPlacer.IsValidPosition(position, terrain, existingEntities);
                Assert.IsTrue(isValid, $"Position {position} far from existing entity should be valid");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_VerifiesAllPlacedEntitiesAreInValidPositions()
        {
            // Arrange
            var terrain = CreateMixedTerrain(10, 10);
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 5, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = 3, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.PowerUp, Count = 2, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            foreach (var entity in entities)
            {
                // Verify position is within bounds
                Assert.IsTrue(entity.Position.X >= 0 && entity.Position.X < terrain.Width,
                    $"Entity X position {entity.Position.X} should be within terrain bounds");
                Assert.IsTrue(entity.Position.Y >= 0 && entity.Position.Y < terrain.Height,
                    $"Entity Y position {entity.Position.Y} should be within terrain bounds");
                
                // Verify position is on walkable terrain
                Assert.IsTrue(terrain.IsWalkable((int)entity.Position.X, (int)entity.Position.Y),
                    $"Entity at {entity.Position} should be on walkable terrain");
                
                // Verify position is valid according to IsValidPosition
                var otherEntities = entities.Where(e => e != entity).ToList();
                Assert.IsTrue(_entityPlacer.IsValidPosition(entity.Position, terrain, otherEntities),
                    $"Entity at {entity.Position} should be in a valid position");
            }
        }
        
        #endregion
        
        #region Impossible Placement Scenario Tests
        
        [TestMethod]
        public void PlaceEntities_WithNoWalkableSpace_PlacesNoEntities()
        {
            // Arrange - Terrain with no walkable tiles
            var terrain = CreateUniformTerrain(4, 4, TileType.Wall);
            var config = CreateEntityConfig(EntityType.Enemy, 5, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            Assert.IsNotNull(entities, "Should return a list even with impossible placement");
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.AreEqual(0, enemyEntities.Count, "Should place no entities when no walkable space exists");
        }
        
        [TestMethod]
        public void PlaceEntities_WithSingleWalkableTile_PlacesOnlyOneEntity()
        {
            // Arrange - Terrain with only one walkable tile
            var terrain = CreateUniformTerrain(3, 3, TileType.Wall);
            terrain.SetTile(1, 1, TileType.Ground); // Only center tile is walkable
            
            var config = CreateEntityConfig(EntityType.Enemy, 5, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var playerEntities = entities.Where(e => e.Type == EntityType.Player).ToList();
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            Assert.AreEqual(1, playerEntities.Count, "Should place exactly one player");
            Assert.AreEqual(0, enemyEntities.Count, "Should place no enemies when only one tile available");
            Assert.AreEqual(new Vector2(1, 1), playerEntities[0].Position, "Player should be on the only walkable tile");
        }
        
        [TestMethod]
        public void PlaceEntities_WithTightDistanceConstraints_PlacesFewerEntities()
        {
            // Arrange - Small terrain with very restrictive distance constraints
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
                        Count = 15, // Request many entities
                        PlacementStrategy = "random",
                        MinDistance = 4.0f // But require large minimum distance
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count < 15, "Should place fewer enemies than requested due to distance constraints");
            Assert.IsTrue(enemyEntities.Count <= 4, "Should place very few enemies with tight distance constraints");
            
            // Verify all placed enemies respect minimum distance
            for (int i = 0; i < enemyEntities.Count - 1; i++)
            {
                for (int j = i + 1; j < enemyEntities.Count; j++)
                {
                    var distance = Vector2.Distance(enemyEntities[i].Position, enemyEntities[j].Position);
                    Assert.IsTrue(distance >= 4.0f, $"Enemies should maintain minimum distance of 4.0, found {distance}");
                }
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithMaxDistanceFromPlayerConstraint_PlacesOnlyNearbyEntities()
        {
            // Arrange - Large terrain but restrict entities to be close to player
            var terrain = CreateUniformTerrain(12, 12, TileType.Ground);
            var config = new GenerationConfig
            {
                Width = 12,
                Height = 12,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Item, 
                        Count = 20,
                        PlacementStrategy = "random",
                        MaxDistanceFromPlayer = 3.0f // Very restrictive distance
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            
            Assert.IsNotNull(playerEntity, "Should have a player entity");
            Assert.IsTrue(itemEntities.Count < 20, "Should place fewer items than requested due to distance constraint");
            
            foreach (var item in itemEntities)
            {
                var distance = Vector2.Distance(playerEntity.Position, item.Position);
                Assert.IsTrue(distance <= 3.0f, $"Item should be within 3.0 units of player, was {distance}");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithConflictingConstraints_HandlesGracefully()
        {
            // Arrange - Create constraints that are mathematically impossible to satisfy
            var terrain = CreateUniformTerrain(8, 8, TileType.Ground);
            var config = new GenerationConfig
            {
                Width = 8,
                Height = 8,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Enemy, 
                        Count = 10,
                        PlacementStrategy = "far_from_player",
                        MaxDistanceFromPlayer = 2.0f, // Must be close to player
                        MinDistance = 5.0f // But also far from each other - impossible!
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            Assert.IsNotNull(entities, "Should handle conflicting constraints gracefully");
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count < 10, "Should place fewer entities when constraints conflict");
            Assert.IsTrue(enemyEntities.Count >= 0, "Should not crash with conflicting constraints");
        }
        
        [TestMethod]
        public void PlaceEntities_WithExcessiveEntityCount_PlacesMaximumPossible()
        {
            // Arrange - Request more entities than can possibly fit
            var terrain = CreateUniformTerrain(5, 5, TileType.Ground);
            var config = CreateEntityConfig(EntityType.Enemy, 100, "random"); // Way more than can fit
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count < 100, "Should place fewer enemies than requested");
            Assert.IsTrue(enemyEntities.Count <= 25, "Should not exceed terrain capacity"); // 5x5 = 25 tiles max
            
            // Verify all placed enemies are on valid positions
            foreach (var enemy in enemyEntities)
            {
                Assert.IsTrue(terrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                    "All placed enemies should be on walkable terrain");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnDisconnectedTerrain_PlacesOnlyInReachableAreas()
        {
            // Arrange - Create terrain with disconnected walkable areas
            var terrain = CreateUniformTerrain(9, 9, TileType.Wall);
            
            // Create two separate walkable areas
            // Area 1: Top-left
            terrain.SetTile(1, 1, TileType.Ground);
            terrain.SetTile(1, 2, TileType.Ground);
            terrain.SetTile(2, 1, TileType.Ground);
            
            // Area 2: Bottom-right (disconnected)
            terrain.SetTile(6, 6, TileType.Ground);
            terrain.SetTile(6, 7, TileType.Ground);
            terrain.SetTile(7, 6, TileType.Ground);
            
            var config = CreateEntityConfig(EntityType.Enemy, 4, "random");
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            Assert.IsNotNull(playerEntity, "Should place a player in one of the areas");
            
            // All entities should be on walkable terrain
            foreach (var entity in entities)
            {
                Assert.IsTrue(terrain.IsWalkable((int)entity.Position.X, (int)entity.Position.Y),
                    $"Entity at {entity.Position} should be on walkable terrain");
            }
            
            // Should place some entities, but not necessarily all requested due to disconnected areas
            Assert.IsTrue(enemyEntities.Count >= 0, "Should handle disconnected terrain gracefully");
        }
        
        [TestMethod]
        public void PlaceEntities_WithZeroOrNegativeEntityCount_PlacesOnlyPlayer()
        {
            // Arrange
            var terrain = CreateUniformTerrain(5, 5, TileType.Ground);
            var config = new GenerationConfig
            {
                Width = 5,
                Height = 5,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 0, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = -3, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(terrain, config, 12345);
            
            // Assert
            var playerEntities = entities.Where(e => e.Type == EntityType.Player).ToList();
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            
            Assert.AreEqual(1, playerEntities.Count, "Should place exactly one player");
            Assert.AreEqual(0, enemyEntities.Count, "Should place no enemies when count is 0");
            Assert.AreEqual(0, itemEntities.Count, "Should place no items when count is negative");
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
        
        private TileMap CreateMixedTerrain(int width, int height)
        {
            var terrain = new TileMap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Create borders of walls, interior mixed walkable
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        terrain.SetTile(x, y, TileType.Wall);
                    }
                    else
                    {
                        // Mix of walkable terrain types
                        var pattern = (x + y) % 3;
                        var tileType = pattern switch
                        {
                            0 => TileType.Ground,
                            1 => TileType.Grass,
                            _ => TileType.Sand
                        };
                        terrain.SetTile(x, y, tileType);
                    }
                }
            }
            return terrain;
        }
        
        private GenerationConfig CreateEntityConfig(EntityType entityType, int count, string strategy)
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
                        PlacementStrategy = strategy 
                    }
                }
            };
        }
        
        #endregion
    }
}
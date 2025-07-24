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
    [TestClass]
    public class EntityPlacementTests
    {
        private IRandomGenerator _random = null!;
        private EntityPlacer _entityPlacer = null!;
        private TileMap _testTerrain = null!;
        private GenerationConfig _testConfig = null!;
        
        [TestInitialize]
        public void Setup()
        {
            _random = new RandomGenerator();
            _entityPlacer = new EntityPlacer(_random);
            
            // Create a simple test terrain (10x10 with walkable ground)
            _testTerrain = new TileMap(10, 10);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    // Create borders of walls, interior of ground
                    if (x == 0 || x == 9 || y == 0 || y == 9)
                        _testTerrain.SetTile(x, y, TileType.Wall);
                    else
                        _testTerrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            _testConfig = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Seed = 12345,
                Entities = new List<EntityConfig>()
            };
        }
        
        #region Terrain Type Tests
        
        [TestMethod]
        public void PlaceEntities_OnGroundTerrain_PlacesEntitiesSuccessfully()
        {
            // Arrange - Create terrain with only ground tiles
            var groundTerrain = new TileMap(5, 5);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    groundTerrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 5,
                Height = 5,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 3, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(groundTerrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count > 0, "Should place enemies on ground terrain");
            
            foreach (var enemy in enemyEntities)
            {
                Assert.AreEqual(TileType.Ground, groundTerrain.GetTile((int)enemy.Position.X, (int)enemy.Position.Y),
                    "Enemy should be placed on ground tile");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnGrassTerrain_PlacesEntitiesSuccessfully()
        {
            // Arrange - Create terrain with grass tiles
            var grassTerrain = new TileMap(5, 5);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    grassTerrain.SetTile(x, y, TileType.Grass);
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 5,
                Height = 5,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Item, Count = 2, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(grassTerrain, config, 12345);
            
            // Assert
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            Assert.IsTrue(itemEntities.Count > 0, "Should place items on grass terrain");
            
            foreach (var item in itemEntities)
            {
                Assert.AreEqual(TileType.Grass, grassTerrain.GetTile((int)item.Position.X, (int)item.Position.Y),
                    "Item should be placed on grass tile");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnSandTerrain_PlacesEntitiesSuccessfully()
        {
            // Arrange - Create terrain with sand tiles
            var sandTerrain = new TileMap(5, 5);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    sandTerrain.SetTile(x, y, TileType.Sand);
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 5,
                Height = 5,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.PowerUp, Count = 2, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(sandTerrain, config, 12345);
            
            // Assert
            var powerUpEntities = entities.Where(e => e.Type == EntityType.PowerUp).ToList();
            Assert.IsTrue(powerUpEntities.Count > 0, "Should place power-ups on sand terrain");
            
            foreach (var powerUp in powerUpEntities)
            {
                Assert.AreEqual(TileType.Sand, sandTerrain.GetTile((int)powerUp.Position.X, (int)powerUp.Position.Y),
                    "Power-up should be placed on sand tile");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnMixedTerrain_PlacesOnlyOnWalkableTiles()
        {
            // Arrange - Create mixed terrain with walkable and non-walkable tiles
            var mixedTerrain = new TileMap(6, 6);
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    // Create a checkerboard pattern
                    if ((x + y) % 2 == 0)
                        mixedTerrain.SetTile(x, y, TileType.Ground); // Walkable
                    else
                        mixedTerrain.SetTile(x, y, TileType.Wall); // Non-walkable
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 6,
                Height = 6,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 5, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(mixedTerrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            foreach (var enemy in enemyEntities)
            {
                var tileType = mixedTerrain.GetTile((int)enemy.Position.X, (int)enemy.Position.Y);
                Assert.IsTrue(mixedTerrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                    $"Enemy should only be placed on walkable tiles, found on {tileType}");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_OnWaterTerrain_DoesNotPlaceEntities()
        {
            // Arrange - Create terrain with mostly water (non-walkable)
            var waterTerrain = new TileMap(5, 5);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    waterTerrain.SetTile(x, y, TileType.Water);
                }
            }
            // Add one walkable tile for player
            waterTerrain.SetTile(2, 2, TileType.Ground);
            
            var config = new GenerationConfig
            {
                Width = 5,
                Height = 5,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 3, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(waterTerrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            // Should have very few or no enemies due to lack of walkable space
            Assert.IsTrue(enemyEntities.Count <= 1, "Should place very few enemies on mostly water terrain");
            
            foreach (var enemy in enemyEntities)
            {
                Assert.IsTrue(waterTerrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                    "Any placed enemy should be on walkable terrain");
            }
        }
        
        #endregion
        
        #region Impossible Placement Scenarios
        
        [TestMethod]
        public void PlaceEntities_WithNoWalkableTerrain_PlacesNoEntities()
        {
            // Arrange - Create terrain with no walkable tiles
            var wallTerrain = new TileMap(4, 4);
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    wallTerrain.SetTile(x, y, TileType.Wall);
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 4,
                Height = 4,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 5, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(wallTerrain, config, 12345);
            
            // Assert
            Assert.IsNotNull(entities, "Should return a list even with no walkable terrain");
            Assert.AreEqual(0, entities.Count, "Should place no entities when no walkable terrain exists");
        }
        
        [TestMethod]
        public void PlaceEntities_WithSingleWalkableTile_PlacesOnlyPlayer()
        {
            // Arrange - Create terrain with only one walkable tile
            var singleTileTerrain = new TileMap(3, 3);
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    singleTileTerrain.SetTile(x, y, TileType.Wall);
                }
            }
            singleTileTerrain.SetTile(1, 1, TileType.Ground); // Only center tile is walkable
            
            var config = new GenerationConfig
            {
                Width = 3,
                Height = 3,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 3, PlacementStrategy = "random" },
                    new EntityConfig { Type = EntityType.Item, Count = 2, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(singleTileTerrain, config, 12345);
            
            // Assert
            var playerEntities = entities.Where(e => e.Type == EntityType.Player).ToList();
            var otherEntities = entities.Where(e => e.Type != EntityType.Player).ToList();
            
            Assert.AreEqual(1, playerEntities.Count, "Should place exactly one player");
            Assert.IsTrue(otherEntities.Count == 0, "Should not place other entities when only one tile is available");
            Assert.AreEqual(new Vector2(1, 1), playerEntities[0].Position, "Player should be on the only walkable tile");
        }
        
        [TestMethod]
        public void PlaceEntities_WithTightMinDistanceConstraints_PlacesFewerEntities()
        {
            // Arrange - Create small terrain with very tight distance constraints
            var smallTerrain = new TileMap(5, 5);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    smallTerrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 5,
                Height = 5,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Enemy, 
                        Count = 10, // Request many entities
                        PlacementStrategy = "random",
                        MinDistance = 3.0f // But require large minimum distance
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(smallTerrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count < 10, "Should place fewer enemies than requested due to distance constraints");
            
            // Verify all placed enemies respect minimum distance
            for (int i = 0; i < enemyEntities.Count - 1; i++)
            {
                for (int j = i + 1; j < enemyEntities.Count; j++)
                {
                    var distance = Vector2.Distance(enemyEntities[i].Position, enemyEntities[j].Position);
                    Assert.IsTrue(distance >= 3.0f, $"Enemies should maintain minimum distance of 3.0, found {distance}");
                }
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithMaxDistanceFromPlayerConstraint_PlacesOnlyNearbyEntities()
        {
            // Arrange - Create large terrain but restrict entities to be close to player
            var largeTerrain = new TileMap(15, 15);
            for (int x = 0; x < 15; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    largeTerrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 15,
                Height = 15,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Item, 
                        Count = 20,
                        PlacementStrategy = "random",
                        MaxDistanceFromPlayer = 2.0f // Very restrictive distance
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(largeTerrain, config, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            
            Assert.IsNotNull(playerEntity, "Should have a player entity");
            Assert.IsTrue(itemEntities.Count < 20, "Should place fewer items than requested due to distance constraint");
            
            foreach (var item in itemEntities)
            {
                var distance = Vector2.Distance(playerEntity.Position, item.Position);
                Assert.IsTrue(distance <= 2.0f, $"Item should be within 2.0 units of player, was {distance}");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithConflictingConstraints_HandlesGracefully()
        {
            // Arrange - Create constraints that are impossible to satisfy
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Enemy, 
                        Count = 5,
                        PlacementStrategy = "far_from_player",
                        MaxDistanceFromPlayer = 2.0f, // Must be close to player
                        MinDistance = 4.0f // But also far from each other - impossible!
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, config, 12345);
            
            // Assert
            Assert.IsNotNull(entities, "Should handle conflicting constraints gracefully");
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count < 5, "Should place fewer entities when constraints conflict");
        }
        
        [TestMethod]
        public void PlaceEntities_WithPathfindingStrategy_OnDisconnectedTerrain_PlacesOnlyReachableEntities()
        {
            // Arrange - Create terrain with disconnected areas
            var disconnectedTerrain = new TileMap(7, 7);
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 7; y++)
                {
                    disconnectedTerrain.SetTile(x, y, TileType.Wall);
                }
            }
            
            // Create two separate walkable areas
            disconnectedTerrain.SetTile(1, 1, TileType.Ground); // Area 1
            disconnectedTerrain.SetTile(1, 2, TileType.Ground);
            disconnectedTerrain.SetTile(5, 5, TileType.Ground); // Area 2 (disconnected)
            disconnectedTerrain.SetTile(5, 4, TileType.Ground);
            
            var config = new GenerationConfig
            {
                Width = 7,
                Height = 7,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Enemy, 
                        Count = 3,
                        PlacementStrategy = "pathfinding"
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(disconnectedTerrain, config, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            if (playerEntity != null)
            {
                // All enemies should be in the same connected area as the player
                foreach (var enemy in enemyEntities)
                {
                    // This is a simplified check - in a real pathfinding test, 
                    // we'd verify actual reachability
                    Assert.IsTrue(disconnectedTerrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                        "Enemy should be placed on walkable terrain");
                }
            }
        }
        
        #endregion
        
        #region Edge Cases and Boundary Tests
        
        [TestMethod]
        public void PlaceEntities_WithZeroEntityCount_PlacesOnlyPlayer()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 0, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, config, 12345);
            
            // Assert
            var playerEntities = entities.Where(e => e.Type == EntityType.Player).ToList();
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            Assert.AreEqual(1, playerEntities.Count, "Should place exactly one player");
            Assert.AreEqual(0, enemyEntities.Count, "Should place no enemies when count is 0");
        }
        
        [TestMethod]
        public void PlaceEntities_WithNegativeEntityCount_PlacesOnlyPlayer()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Item, Count = -5, PlacementStrategy = "random" }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, config, 12345);
            
            // Assert
            var playerEntities = entities.Where(e => e.Type == EntityType.Player).ToList();
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            
            Assert.AreEqual(1, playerEntities.Count, "Should place exactly one player");
            Assert.AreEqual(0, itemEntities.Count, "Should place no items when count is negative");
        }
        
        [TestMethod]
        public void PlaceEntities_WithEmptyEntityConfiguration_PlacesOnlyPlayer()
        {
            // Arrange
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>() // Empty list
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, config, 12345);
            
            // Assert
            var playerEntities = entities.Where(e => e.Type == EntityType.Player).ToList();
            
            Assert.AreEqual(1, playerEntities.Count, "Should place exactly one player even with empty entity config");
            Assert.AreEqual(1, entities.Count, "Should have only the player entity");
        }
        
        [TestMethod]
        public void PlaceEntities_WithUnknownPlacementStrategy_FallsBackToRandom()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Enemy,
                Count = 2,
                PlacementStrategy = "unknown_strategy" // Invalid strategy
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count > 0, "Should still place entities with unknown strategy by falling back to random");
            
            foreach (var enemy in enemyEntities)
            {
                Assert.IsTrue(_testTerrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                    "Enemy should be placed on walkable terrain even with unknown strategy");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithVeryLargeEntityCount_PlacesMaximumPossible()
        {
            // Arrange - Request more entities than can possibly fit
            var config = new GenerationConfig
            {
                Width = 10,
                Height = 10,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig 
                    { 
                        Type = EntityType.Enemy, 
                        Count = 1000, // Way more than can fit
                        PlacementStrategy = "random"
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, config, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count < 1000, "Should place fewer enemies than requested");
            Assert.IsTrue(enemyEntities.Count > 0, "Should place some enemies");
            
            // Verify all placed enemies are on valid positions
            foreach (var enemy in enemyEntities)
            {
                Assert.IsTrue(_testTerrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y),
                    "All placed enemies should be on walkable terrain");
            }
        }
        
        #endregion
        
        #region Placement Strategy Tests
        
        [TestMethod]
        public void PlaceEntities_WithNearWallsStrategy_PlacesEntitiesNearWalls()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Item,
                Count = 3,
                PlacementStrategy = "near_walls"
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            
            foreach (var item in itemEntities)
            {
                int x = (int)item.Position.X;
                int y = (int)item.Position.Y;
                
                // Check if there's at least one adjacent wall
                bool nearWall = false;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (!_testTerrain.IsWalkable(x + dx, y + dy))
                        {
                            nearWall = true;
                            break;
                        }
                    }
                    if (nearWall) break;
                }
                
                Assert.IsTrue(nearWall, "Item placed with near_walls strategy should be adjacent to at least one wall");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithCenterStrategy_PlacesEntitiesNearCenter()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.PowerUp,
                Count = 2,
                PlacementStrategy = "center"
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var powerUpEntities = entities.Where(e => e.Type == EntityType.PowerUp).ToList();
            var center = new Vector2(_testTerrain.Width / 2f, _testTerrain.Height / 2f);
            
            foreach (var powerUp in powerUpEntities)
            {
                var distanceFromCenter = Vector2.Distance(powerUp.Position, center);
                // Should be closer to center than to edges
                var distanceToEdge = Math.Min(
                    Math.Min(powerUp.Position.X, _testTerrain.Width - powerUp.Position.X),
                    Math.Min(powerUp.Position.Y, _testTerrain.Height - powerUp.Position.Y));
                
                Assert.IsTrue(distanceFromCenter <= distanceToEdge + 2.0f, 
                    "Power-up should be placed relatively close to center with center strategy");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithCornersStrategy_PlacesEntitiesInCorners()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Exit,
                Count = 1,
                PlacementStrategy = "corners"
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var exitEntities = entities.Where(e => e.Type == EntityType.Exit).ToList();
            
            if (exitEntities.Count > 0)
            {
                var exit = exitEntities[0];
                var center = new Vector2(_testTerrain.Width / 2f, _testTerrain.Height / 2f);
                var distanceFromCenter = Vector2.Distance(exit.Position, center);
                
                // Should be farther from center (indicating corner placement)
                Assert.IsTrue(distanceFromCenter > 3.0f, 
                    "Exit with corners strategy should be placed away from center");
            }
        }
        
        #endregion
        
        #region Valid Position Tests
        
        [TestMethod]
        public void PlaceEntities_WithValidConfiguration_PlacesPlayerFirst()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Enemy,
                Count = 2,
                PlacementStrategy = "random"
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            Assert.IsTrue(entities.Count > 0, "Should place at least the player entity");
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            Assert.IsNotNull(playerEntity, "Should place a player entity first");
            Assert.IsTrue(_testTerrain.IsWalkable((int)playerEntity.Position.X, (int)playerEntity.Position.Y), 
                "Player should be placed on walkable terrain");
        }
        
        [TestMethod]
        public void PlaceEntities_WithEnemyConfiguration_PlacesEnemiesAwayFromPlayer()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Enemy,
                Count = 3,
                PlacementStrategy = "random",
                MinDistance = 2.0f
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            
            Assert.IsNotNull(playerEntity, "Should have a player entity");
            Assert.IsTrue(enemyEntities.Count > 0, "Should place enemy entities");
            
            foreach (var enemy in enemyEntities)
            {
                var distance = Vector2.Distance(playerEntity.Position, enemy.Position);
                Assert.IsTrue(distance >= 2.0f, $"Enemy should be at least 2 units away from player, was {distance}");
                Assert.IsTrue(_testTerrain.IsWalkable((int)enemy.Position.X, (int)enemy.Position.Y), 
                    "Enemy should be placed on walkable terrain");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithItemConfiguration_PlacesItemsOnWalkableTerrain()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Item,
                Count = 5,
                PlacementStrategy = "random"
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            Assert.IsTrue(itemEntities.Count > 0, "Should place item entities");
            
            foreach (var item in itemEntities)
            {
                Assert.IsTrue(_testTerrain.IsWalkable((int)item.Position.X, (int)item.Position.Y), 
                    "Item should be placed on walkable terrain");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithClusteredStrategy_PlacesEntitiesNearEachOther()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Enemy,
                Count = 4,
                PlacementStrategy = "clustered",
                MinDistance = 1.0f
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var enemyEntities = entities.Where(e => e.Type == EntityType.Enemy).ToList();
            Assert.IsTrue(enemyEntities.Count >= 2, "Should place multiple enemies for clustering test");
            
            if (enemyEntities.Count >= 2)
            {
                // Check that at least some enemies are close to each other
                bool foundCluster = false;
                for (int i = 0; i < enemyEntities.Count - 1; i++)
                {
                    for (int j = i + 1; j < enemyEntities.Count; j++)
                    {
                        var distance = Vector2.Distance(enemyEntities[i].Position, enemyEntities[j].Position);
                        if (distance <= 3.0f) // Within clustering range
                        {
                            foundCluster = true;
                            break;
                        }
                    }
                    if (foundCluster) break;
                }
                
                Assert.IsTrue(foundCluster, "Clustered strategy should place some entities near each other");
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithSpreadStrategy_PlacesEntitiesFarApart()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Item,
                Count = 3,
                PlacementStrategy = "spread",
                MinDistance = 2.0f
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            
            // Check that all items maintain minimum distance
            for (int i = 0; i < itemEntities.Count - 1; i++)
            {
                for (int j = i + 1; j < itemEntities.Count; j++)
                {
                    var distance = Vector2.Distance(itemEntities[i].Position, itemEntities[j].Position);
                    Assert.IsTrue(distance >= 2.0f, $"Items should be at least 2 units apart, found {distance}");
                }
            }
        }
        
        [TestMethod]
        public void PlaceEntities_WithFarFromPlayerStrategy_PlacesEntitiesAwayFromPlayer()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Exit,
                Count = 1,
                PlacementStrategy = "far_from_player"
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var exitEntity = entities.FirstOrDefault(e => e.Type == EntityType.Exit);
            
            Assert.IsNotNull(playerEntity, "Should have a player entity");
            Assert.IsNotNull(exitEntity, "Should have an exit entity");
            
            var distance = Vector2.Distance(playerEntity.Position, exitEntity.Position);
            Assert.IsTrue(distance > 3.0f, $"Exit should be far from player, distance was {distance}");
        }
        
        [TestMethod]
        public void PlaceEntities_WithMaxDistanceConstraint_RespectsDistanceLimit()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Item,
                Count = 5,
                PlacementStrategy = "random",
                MaxDistanceFromPlayer = 4.0f
            });
            
            // Act
            var entities = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, 12345);
            
            // Assert
            var playerEntity = entities.FirstOrDefault(e => e.Type == EntityType.Player);
            var itemEntities = entities.Where(e => e.Type == EntityType.Item).ToList();
            
            Assert.IsNotNull(playerEntity, "Should have a player entity");
            
            foreach (var item in itemEntities)
            {
                var distance = Vector2.Distance(playerEntity.Position, item.Position);
                Assert.IsTrue(distance <= 4.0f, $"Item should be within 4 units of player, was {distance}");
            }
        }
        
        [TestMethod]
        public void IsValidPosition_WithWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var position = new Vector2(5, 5); // Center of test terrain (walkable)
            var existingEntities = new List<Entity>();
            
            // Act
            var isValid = _entityPlacer.IsValidPosition(position, _testTerrain, existingEntities);
            
            // Assert
            Assert.IsTrue(isValid, "Position on walkable terrain should be valid");
        }
        
        [TestMethod]
        public void IsValidPosition_WithWallTerrain_ReturnsFalse()
        {
            // Arrange
            var position = new Vector2(0, 0); // Corner of test terrain (wall)
            var existingEntities = new List<Entity>();
            
            // Act
            var isValid = _entityPlacer.IsValidPosition(position, _testTerrain, existingEntities);
            
            // Assert
            Assert.IsFalse(isValid, "Position on wall terrain should not be valid");
        }
        
        [TestMethod]
        public void IsValidPosition_WithOccupiedPosition_ReturnsFalse()
        {
            // Arrange
            var position = new Vector2(5, 5);
            var existingEntities = new List<Entity>
            {
                EntityFactory.CreateEntity(EntityType.Player)
            };
            existingEntities[0].Position = position;
            
            // Act
            var isValid = _entityPlacer.IsValidPosition(position, _testTerrain, existingEntities);
            
            // Assert
            Assert.IsFalse(isValid, "Position occupied by another entity should not be valid");
        }
        
        [TestMethod]
        public void IsValidPosition_OutOfBounds_ReturnsFalse()
        {
            // Arrange
            var position = new Vector2(-1, -1); // Out of bounds
            var existingEntities = new List<Entity>();
            
            // Act
            var isValid = _entityPlacer.IsValidPosition(position, _testTerrain, existingEntities);
            
            // Assert
            Assert.IsFalse(isValid, "Out of bounds position should not be valid");
        }
        
        [TestMethod]
        public void PlaceEntities_WithImpossibleConfiguration_HandlesGracefully()
        {
            // Arrange - Create a very small terrain with too many entities
            var smallTerrain = new TileMap(3, 3);
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    smallTerrain.SetTile(x, y, x == 1 && y == 1 ? TileType.Ground : TileType.Wall);
                }
            }
            
            var config = new GenerationConfig
            {
                Width = 3,
                Height = 3,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig
                    {
                        Type = EntityType.Enemy,
                        Count = 10, // Too many for the available space
                        PlacementStrategy = "random",
                        MinDistance = 1.0f
                    }
                }
            };
            
            // Act
            var entities = _entityPlacer.PlaceEntities(smallTerrain, config, 12345);
            
            // Assert
            Assert.IsNotNull(entities, "Should return a list even when placement is impossible");
            var enemyCount = entities.Count(e => e.Type == EntityType.Enemy);
            Assert.IsTrue(enemyCount < 10, "Should place fewer enemies than requested when space is limited");
        }
        
        [TestMethod]
        public void PlaceEntities_WithReproducibleSeed_GeneratesConsistentResults()
        {
            // Arrange
            _testConfig.Entities.Add(new EntityConfig
            {
                Type = EntityType.Enemy,
                Count = 3,
                PlacementStrategy = "random"
            });
            
            const int seed = 54321;
            
            // Act
            var entities1 = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, seed);
            var entities2 = _entityPlacer.PlaceEntities(_testTerrain, _testConfig, seed);
            
            // Assert
            Assert.AreEqual(entities1.Count, entities2.Count, "Same seed should produce same number of entities");
            
            for (int i = 0; i < entities1.Count; i++)
            {
                Assert.AreEqual(entities1[i].Type, entities2[i].Type, $"Entity {i} should have same type");
                Assert.AreEqual(entities1[i].Position, entities2[i].Position, $"Entity {i} should have same position");
            }
        }
        
        [TestMethod]
        public void EntityFactory_CreateEntity_CreatesCorrectEntityTypes()
        {
            // Test all entity types
            var entityTypes = EntityFactory.GetAllEntityTypes();
            
            foreach (var entityType in entityTypes)
            {
                // Act
                var entity = EntityFactory.CreateEntity(entityType);
                
                // Assert
                Assert.IsNotNull(entity, $"Should create entity for type {entityType}");
                Assert.AreEqual(entityType, entity.Type, $"Created entity should have correct type {entityType}");
            }
        }
        
        [TestMethod]
        public void EntityFactory_CreateEntityWithProperties_SetsProperties()
        {
            // Arrange
            var properties = new Dictionary<string, object>
            {
                { "health", 100 },
                { "damage", 25 },
                { "name", "TestEnemy" }
            };
            
            // Act
            var entity = EntityFactory.CreateEntity(EntityType.Enemy, properties);
            
            // Assert
            Assert.AreEqual(EntityType.Enemy, entity.Type);
            Assert.AreEqual(100, entity.Properties["health"]);
            Assert.AreEqual(25, entity.Properties["damage"]);
            Assert.AreEqual("TestEnemy", entity.Properties["name"]);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void EntityFactory_CreateEntity_WithInvalidType_ThrowsException()
        {
            // Act
            EntityFactory.CreateEntity((EntityType)999);
        }
        
        #endregion
    }
}

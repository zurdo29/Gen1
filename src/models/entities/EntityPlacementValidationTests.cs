using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Tests.Models.Entities
{
    /// <summary>
    /// Unit tests for entity-specific placement validation rules
    /// Tests each entity type's CanPlaceAt method with various terrain and entity configurations
    /// </summary>
    [TestClass]
    public class EntityPlacementValidationTests
    {
        private TileMap _walkableTerrain = null!;
        private TileMap _nonWalkableTerrain = null!;
        private TileMap _mixedTerrain = null!;
        private List<Entity> _emptyEntityList = null!;
        
        [TestInitialize]
        public void Setup()
        {
            // Create walkable terrain (all ground)
            _walkableTerrain = new TileMap(6, 6);
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    _walkableTerrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            // Create non-walkable terrain (all walls)
            _nonWalkableTerrain = new TileMap(4, 4);
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    _nonWalkableTerrain.SetTile(x, y, TileType.Wall);
                }
            }
            
            // Create mixed terrain
            _mixedTerrain = new TileMap(8, 8);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    // Checkerboard pattern of walkable and non-walkable
                    if ((x + y) % 2 == 0)
                        _mixedTerrain.SetTile(x, y, TileType.Ground);
                    else
                        _mixedTerrain.SetTile(x, y, TileType.Wall);
                }
            }
            
            _emptyEntityList = new List<Entity>();
        }
        
        #region Player Entity Tests
        
        [TestMethod]
        public void PlayerEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var player = new PlayerEntity();
            var position = new Vector2(3, 3);
            
            // Act
            var canPlace = player.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "Player should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Player, player.Type);
        }
        
        [TestMethod]
        public void PlayerEntity_CanPlaceAt_OnNonWalkableTerrain_ReturnsFalse()
        {
            // Arrange
            var player = new PlayerEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = player.CanPlaceAt(position, _nonWalkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsFalse(canPlace, "Player should not be placeable on non-walkable terrain");
        }
        
        [TestMethod]
        public void PlayerEntity_CanPlaceAt_OnDifferentWalkableTerrainTypes_ReturnsTrue()
        {
            // Arrange
            var player = new PlayerEntity();
            var terrainTypes = new[] { TileType.Ground, TileType.Grass, TileType.Sand };
            
            foreach (var terrainType in terrainTypes)
            {
                var terrain = new TileMap(3, 3);
                terrain.SetTile(1, 1, terrainType);
                var position = new Vector2(1, 1);
                
                // Act
                var canPlace = player.CanPlaceAt(position, terrain, _emptyEntityList);
                
                // Assert
                Assert.IsTrue(canPlace, $"Player should be placeable on {terrainType} terrain");
            }
        }
        
        #endregion
        
        #region Enemy Entity Tests
        
        [TestMethod]
        public void EnemyEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var enemy = new EnemyEntity();
            var position = new Vector2(3, 3);
            
            // Act
            var canPlace = enemy.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "Enemy should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Enemy, enemy.Type);
        }
        
        [TestMethod]
        public void EnemyEntity_CanPlaceAt_TooCloseToPlayer_ReturnsFalse()
        {
            // Arrange
            var enemy = new EnemyEntity();
            var player = new PlayerEntity { Position = new Vector2(3, 3) };
            var entitiesWithPlayer = new List<Entity> { player };
            
            var tooClosePositions = new[]
            {
                new Vector2(3, 3),     // Same position
                new Vector2(3, 4),     // Distance = 1
                new Vector2(4, 3),     // Distance = 1
                new Vector2(4, 4),     // Distance = √2 ≈ 1.41
                new Vector2(3, 5)      // Distance = 2
            };
            
            // Act & Assert
            foreach (var position in tooClosePositions)
            {
                var canPlace = enemy.CanPlaceAt(position, _walkableTerrain, entitiesWithPlayer);
                Assert.IsFalse(canPlace, $"Enemy should not be placeable at {position} (too close to player)");
            }
        }
        
        [TestMethod]
        public void EnemyEntity_CanPlaceAt_FarFromPlayer_ReturnsTrue()
        {
            // Arrange
            var enemy = new EnemyEntity();
            var player = new PlayerEntity { Position = new Vector2(1, 1) };
            var entitiesWithPlayer = new List<Entity> { player };
            
            var farPositions = new[]
            {
                new Vector2(4, 4),     // Distance = √18 ≈ 4.24
                new Vector2(5, 1),     // Distance = 4
                new Vector2(1, 5)      // Distance = 4
            };
            
            // Act & Assert
            foreach (var position in farPositions)
            {
                var canPlace = enemy.CanPlaceAt(position, _walkableTerrain, entitiesWithPlayer);
                Assert.IsTrue(canPlace, $"Enemy should be placeable at {position} (far from player)");
            }
        }
        
        [TestMethod]
        public void EnemyEntity_CanPlaceAt_TooCloseToOtherEnemy_ReturnsFalse()
        {
            // Arrange
            var enemy = new EnemyEntity();
            var existingEnemy = new EnemyEntity { Position = new Vector2(3, 3) };
            var entitiesWithEnemy = new List<Entity> { existingEnemy };
            
            var tooClosePosition = new Vector2(3, 4); // Distance = 1
            
            // Act
            var canPlace = enemy.CanPlaceAt(tooClosePosition, _walkableTerrain, entitiesWithEnemy);
            
            // Assert
            Assert.IsFalse(canPlace, "Enemy should not be placeable too close to another enemy");
        }
        
        #endregion
        
        #region Item Entity Tests
        
        [TestMethod]
        public void ItemEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var item = new ItemEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = item.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "Item should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Item, item.Type);
        }
        
        [TestMethod]
        public void ItemEntity_CanPlaceAt_OnNonWalkableTerrain_ReturnsFalse()
        {
            // Arrange
            var item = new ItemEntity();
            var position = new Vector2(1, 1);
            
            // Act
            var canPlace = item.CanPlaceAt(position, _nonWalkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsFalse(canPlace, "Item should not be placeable on non-walkable terrain");
        }
        
        [TestMethod]
        public void ItemEntity_CanPlaceAt_OccupiedPosition_ReturnsFalse()
        {
            // Arrange
            var item = new ItemEntity();
            var existingItem = new ItemEntity { Position = new Vector2(2, 2) };
            var entitiesWithItem = new List<Entity> { existingItem };
            var position = new Vector2(2, 2); // Same position
            
            // Act
            var canPlace = item.CanPlaceAt(position, _walkableTerrain, entitiesWithItem);
            
            // Assert
            Assert.IsFalse(canPlace, "Item should not be placeable on occupied position");
        }
        
        [TestMethod]
        public void ItemEntity_CanPlaceAt_NearOtherEntities_ReturnsTrue()
        {
            // Arrange
            var item = new ItemEntity();
            var player = new PlayerEntity { Position = new Vector2(2, 2) };
            var enemy = new EnemyEntity { Position = new Vector2(3, 3) };
            var entitiesWithOthers = new List<Entity> { player, enemy };
            var position = new Vector2(4, 4); // Close but not overlapping
            
            // Act
            var canPlace = item.CanPlaceAt(position, _walkableTerrain, entitiesWithOthers);
            
            // Assert
            Assert.IsTrue(canPlace, "Item should be placeable near other entities if not overlapping");
        }
        
        #endregion
        
        #region PowerUp Entity Tests
        
        [TestMethod]
        public void PowerUpEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var powerUp = new PowerUpEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = powerUp.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "PowerUp should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.PowerUp, powerUp.Type);
        }
        
        [TestMethod]
        public void PowerUpEntity_CanPlaceAt_TooCloseToOtherEntity_ReturnsFalse()
        {
            // Arrange
            var powerUp = new PowerUpEntity();
            var existingEntity = new ItemEntity { Position = new Vector2(2, 2) };
            var entitiesWithItem = new List<Entity> { existingEntity };
            var position = new Vector2(2.5f, 2); // Distance < 1.0
            
            // Act
            var canPlace = powerUp.CanPlaceAt(position, _walkableTerrain, entitiesWithItem);
            
            // Assert
            Assert.IsFalse(canPlace, "PowerUp should not be placeable too close to other entities");
        }
        
        [TestMethod]
        public void PowerUpEntity_CanPlaceAt_FarFromOtherEntities_ReturnsTrue()
        {
            // Arrange
            var powerUp = new PowerUpEntity();
            var existingEntity = new ItemEntity { Position = new Vector2(1, 1) };
            var entitiesWithItem = new List<Entity> { existingEntity };
            var position = new Vector2(3, 3); // Distance > 1.0
            
            // Act
            var canPlace = powerUp.CanPlaceAt(position, _walkableTerrain, entitiesWithItem);
            
            // Assert
            Assert.IsTrue(canPlace, "PowerUp should be placeable far from other entities");
        }
        
        #endregion
        
        #region NPC Entity Tests
        
        [TestMethod]
        public void NPCEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var npc = new NPCEntity();
            var position = new Vector2(3, 3);
            
            // Act
            var canPlace = npc.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "NPC should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.NPC, npc.Type);
        }
        
        [TestMethod]
        public void NPCEntity_CanPlaceAt_TooCloseToOtherEntity_ReturnsFalse()
        {
            // Arrange
            var npc = new NPCEntity();
            var existingEntity = new PlayerEntity { Position = new Vector2(2, 2) };
            var entitiesWithPlayer = new List<Entity> { existingEntity };
            var position = new Vector2(2, 3); // Distance = 1 < 1.5
            
            // Act
            var canPlace = npc.CanPlaceAt(position, _walkableTerrain, entitiesWithPlayer);
            
            // Assert
            Assert.IsFalse(canPlace, "NPC should not be placeable too close to other entities (< 1.5 units)");
        }
        
        [TestMethod]
        public void NPCEntity_CanPlaceAt_FarEnoughFromOtherEntity_ReturnsTrue()
        {
            // Arrange
            var npc = new NPCEntity();
            var existingEntity = new PlayerEntity { Position = new Vector2(1, 1) };
            var entitiesWithPlayer = new List<Entity> { existingEntity };
            var position = new Vector2(3, 3); // Distance = √8 ≈ 2.83 > 1.5
            
            // Act
            var canPlace = npc.CanPlaceAt(position, _walkableTerrain, entitiesWithPlayer);
            
            // Assert
            Assert.IsTrue(canPlace, "NPC should be placeable far enough from other entities (> 1.5 units)");
        }
        
        #endregion
        
        #region Exit Entity Tests
        
        [TestMethod]
        public void ExitEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var exit = new ExitEntity();
            var position = new Vector2(4, 4);
            
            // Act
            var canPlace = exit.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "Exit should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Exit, exit.Type);
        }
        
        [TestMethod]
        public void ExitEntity_CanPlaceAt_TooCloseToPlayer_ReturnsFalse()
        {
            // Arrange
            var exit = new ExitEntity();
            var player = new PlayerEntity { Position = new Vector2(3, 3) };
            var entitiesWithPlayer = new List<Entity> { player };
            
            var tooClosePositions = new[]
            {
                new Vector2(3, 3),     // Same position
                new Vector2(3, 4),     // Distance = 1
                new Vector2(4, 4),     // Distance = √2 ≈ 1.41
                new Vector2(3, 6)      // Distance = 3
            };
            
            // Act & Assert
            foreach (var position in tooClosePositions)
            {
                var canPlace = exit.CanPlaceAt(position, _walkableTerrain, entitiesWithPlayer);
                Assert.IsFalse(canPlace, $"Exit should not be placeable at {position} (too close to player, distance < 5)");
            }
        }
        
        [TestMethod]
        public void ExitEntity_CanPlaceAt_FarFromPlayer_ReturnsTrue()
        {
            // Arrange
            var exit = new ExitEntity();
            var player = new PlayerEntity { Position = new Vector2(0, 0) };
            var entitiesWithPlayer = new List<Entity> { player };
            var position = new Vector2(5, 0); // Distance = 5 (exactly at threshold)
            
            // Act
            var canPlace = exit.CanPlaceAt(position, _walkableTerrain, entitiesWithPlayer);
            
            // Assert
            Assert.IsTrue(canPlace, "Exit should be placeable far from player (distance >= 5)");
        }
        
        #endregion
        
        #region Checkpoint Entity Tests
        
        [TestMethod]
        public void CheckpointEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var checkpoint = new CheckpointEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = checkpoint.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "Checkpoint should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Checkpoint, checkpoint.Type);
        }
        
        [TestMethod]
        public void CheckpointEntity_CanPlaceAt_NearOtherEntities_ReturnsTrue()
        {
            // Arrange
            var checkpoint = new CheckpointEntity();
            var player = new PlayerEntity { Position = new Vector2(2, 2) };
            var enemy = new EnemyEntity { Position = new Vector2(3, 2) };
            var entitiesWithOthers = new List<Entity> { player, enemy };
            var position = new Vector2(2, 3); // Close to both
            
            // Act
            var canPlace = checkpoint.CanPlaceAt(position, _walkableTerrain, entitiesWithOthers);
            
            // Assert
            Assert.IsTrue(canPlace, "Checkpoint should be placeable near other entities");
        }
        
        #endregion
        
        #region Obstacle Entity Tests
        
        [TestMethod]
        public void ObstacleEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var obstacle = new ObstacleEntity();
            var position = new Vector2(3, 3);
            
            // Act
            var canPlace = obstacle.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "Obstacle should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Obstacle, obstacle.Type);
        }
        
        [TestMethod]
        public void ObstacleEntity_CanPlaceAt_OnNonWalkableTerrain_ReturnsFalse()
        {
            // Arrange
            var obstacle = new ObstacleEntity();
            var position = new Vector2(1, 1);
            
            // Act
            var canPlace = obstacle.CanPlaceAt(position, _nonWalkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsFalse(canPlace, "Obstacle should not be placeable on non-walkable terrain");
        }
        
        #endregion
        
        #region Trigger Entity Tests
        
        [TestMethod]
        public void TriggerEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var trigger = new TriggerEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = trigger.CanPlaceAt(position, _walkableTerrain, _emptyEntityList);
            
            // Assert
            Assert.IsTrue(canPlace, "Trigger should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Trigger, trigger.Type);
        }
        
        [TestMethod]
        public void TriggerEntity_CanPlaceAt_OverlapWithNonTriggerEntity_ReturnsTrue()
        {
            // Arrange
            var trigger = new TriggerEntity();
            var item = new ItemEntity { Position = new Vector2(2, 2) };
            var entitiesWithItem = new List<Entity> { item };
            var position = new Vector2(2, 2); // Same position as item
            
            // Act
            var canPlace = trigger.CanPlaceAt(position, _walkableTerrain, entitiesWithItem);
            
            // Assert
            Assert.IsTrue(canPlace, "Trigger should be able to overlap with non-trigger entities");
        }
        
        [TestMethod]
        public void TriggerEntity_CanPlaceAt_OverlapWithAnotherTrigger_ReturnsFalse()
        {
            // Arrange
            var trigger = new TriggerEntity();
            var existingTrigger = new TriggerEntity { Position = new Vector2(2, 2) };
            var entitiesWithTrigger = new List<Entity> { existingTrigger };
            var position = new Vector2(2, 2); // Same position as existing trigger
            
            // Act
            var canPlace = trigger.CanPlaceAt(position, _walkableTerrain, entitiesWithTrigger);
            
            // Assert
            Assert.IsFalse(canPlace, "Trigger should not overlap with another trigger at exact same position");
        }
        
        #endregion
        
        #region Mixed Terrain Tests
        
        [TestMethod]
        public void AllEntityTypes_CanPlaceAt_OnMixedTerrain_OnlyOnWalkableTiles()
        {
            // Arrange
            var entityTypes = new Entity[]
            {
                new PlayerEntity(),
                new EnemyEntity(),
                new ItemEntity(),
                new PowerUpEntity(),
                new NPCEntity(),
                new ExitEntity(),
                new CheckpointEntity(),
                new ObstacleEntity(),
                new TriggerEntity()
            };
            
            // Test positions on walkable tiles (even coordinates in checkerboard)
            var walkablePositions = new[] { new Vector2(0, 0), new Vector2(2, 2), new Vector2(4, 4) };
            
            // Test positions on non-walkable tiles (odd coordinates in checkerboard)
            var nonWalkablePositions = new[] { new Vector2(1, 1), new Vector2(3, 3), new Vector2(5, 5) };
            
            // Act & Assert
            foreach (var entity in entityTypes)
            {
                // Should be placeable on walkable tiles
                foreach (var walkablePos in walkablePositions)
                {
                    var canPlaceOnWalkable = entity.CanPlaceAt(walkablePos, _mixedTerrain, _emptyEntityList);
                    Assert.IsTrue(canPlaceOnWalkable, 
                        $"{entity.GetType().Name} should be placeable on walkable tile at {walkablePos}");
                }
                
                // Should not be placeable on non-walkable tiles
                foreach (var nonWalkablePos in nonWalkablePositions)
                {
                    var canPlaceOnNonWalkable = entity.CanPlaceAt(nonWalkablePos, _mixedTerrain, _emptyEntityList);
                    Assert.IsFalse(canPlaceOnNonWalkable, 
                        $"{entity.GetType().Name} should not be placeable on non-walkable tile at {nonWalkablePos}");
                }
            }
        }
        
        #endregion
    }
}
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Tests.Models.Entities
{
    [TestClass]
    public class EntityTests
    {
        private TileMap _testTerrain = null!;
        private List<Entity> _existingEntities = null!;
        
        [TestInitialize]
        public void Setup()
        {
            // Create a simple 5x5 test terrain
            _testTerrain = new TileMap(5, 5);
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    // Borders are walls, interior is ground
                    if (x == 0 || x == 4 || y == 0 || y == 4)
                        _testTerrain.SetTile(x, y, TileType.Wall);
                    else
                        _testTerrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            _existingEntities = new List<Entity>();
        }
        
        [TestMethod]
        public void PlayerEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var player = new PlayerEntity();
            var position = new Vector2(2, 2); // Center, walkable
            
            // Act
            var canPlace = player.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "Player should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Player, player.Type);
        }
        
        [TestMethod]
        public void PlayerEntity_CanPlaceAt_OnWall_ReturnsFalse()
        {
            // Arrange
            var player = new PlayerEntity();
            var position = new Vector2(0, 0); // Wall position
            
            // Act
            var canPlace = player.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsFalse(canPlace, "Player should not be placeable on walls");
        }
        
        [TestMethod]
        public void EnemyEntity_CanPlaceAt_NearPlayer_ReturnsFalse()
        {
            // Arrange
            var enemy = new EnemyEntity();
            var player = new PlayerEntity { Position = new Vector2(2, 2) };
            _existingEntities.Add(player);
            
            var position = new Vector2(2, 3); // Too close to player (distance < 3)
            
            // Act
            var canPlace = enemy.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsFalse(canPlace, "Enemy should not be placeable too close to player");
            Assert.AreEqual(EntityType.Enemy, enemy.Type);
        }
        
        [TestMethod]
        public void EnemyEntity_CanPlaceAt_FarFromPlayer_ReturnsTrue()
        {
            // Arrange
            var enemy = new EnemyEntity();
            var player = new PlayerEntity { Position = new Vector2(1, 1) };
            _existingEntities.Add(player);
            
            var position = new Vector2(3, 3); // Far enough from player
            
            // Act
            var canPlace = enemy.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "Enemy should be placeable far from player");
        }
        
        [TestMethod]
        public void ItemEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var item = new ItemEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = item.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "Item should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Item, item.Type);
        }
        
        [TestMethod]
        public void ItemEntity_CanPlaceAt_OccupiedPosition_ReturnsFalse()
        {
            // Arrange
            var item = new ItemEntity();
            var existingItem = new ItemEntity { Position = new Vector2(2, 2) };
            _existingEntities.Add(existingItem);
            
            var position = new Vector2(2, 2); // Same position as existing item
            
            // Act
            var canPlace = item.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsFalse(canPlace, "Item should not be placeable on occupied position");
        }
        
        [TestMethod]
        public void PowerUpEntity_CanPlaceAt_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var powerUp = new PowerUpEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = powerUp.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "PowerUp should be placeable on valid position");
            Assert.AreEqual(EntityType.PowerUp, powerUp.Type);
        }
        
        [TestMethod]
        public void NPCEntity_CanPlaceAt_WithMinimumDistance_ReturnsTrue()
        {
            // Arrange
            var npc = new NPCEntity();
            var existingEntity = new ItemEntity { Position = new Vector2(1, 1) };
            _existingEntities.Add(existingEntity);
            
            var position = new Vector2(3, 3); // Far enough (distance > 1.5)
            
            // Act
            var canPlace = npc.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "NPC should be placeable with sufficient distance");
            Assert.AreEqual(EntityType.NPC, npc.Type);
        }
        
        [TestMethod]
        public void ExitEntity_CanPlaceAt_FarFromPlayer_ReturnsTrue()
        {
            // Arrange
            var exit = new ExitEntity();
            var player = new PlayerEntity { Position = new Vector2(1, 1) };
            _existingEntities.Add(player);
            
            var position = new Vector2(3, 3); // Distance > 5 not possible in 5x5, but > 2 is ok
            
            // Act
            var canPlace = exit.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "Exit should be placeable far from player");
            Assert.AreEqual(EntityType.Exit, exit.Type);
        }
        
        [TestMethod]
        public void ExitEntity_CanPlaceAt_TooCloseToPlayer_ReturnsFalse()
        {
            // Arrange
            var exit = new ExitEntity();
            var player = new PlayerEntity { Position = new Vector2(2, 2) };
            _existingEntities.Add(player);
            
            var position = new Vector2(2, 3); // Too close to player (distance < 5)
            
            // Act
            var canPlace = exit.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsFalse(canPlace, "Exit should not be placeable too close to player");
        }
        
        [TestMethod]
        public void CheckpointEntity_CanPlaceAt_ValidPosition_ReturnsTrue()
        {
            // Arrange
            var checkpoint = new CheckpointEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = checkpoint.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "Checkpoint should be placeable on valid position");
            Assert.AreEqual(EntityType.Checkpoint, checkpoint.Type);
        }
        
        [TestMethod]
        public void ObstacleEntity_CanPlaceAt_OnWalkableTerrain_ReturnsTrue()
        {
            // Arrange
            var obstacle = new ObstacleEntity();
            var position = new Vector2(2, 2);
            
            // Act
            var canPlace = obstacle.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "Obstacle should be placeable on walkable terrain");
            Assert.AreEqual(EntityType.Obstacle, obstacle.Type);
        }
        
        [TestMethod]
        public void TriggerEntity_CanPlaceAt_AllowsOverlap_ReturnsTrue()
        {
            // Arrange
            var trigger = new TriggerEntity();
            var existingItem = new ItemEntity { Position = new Vector2(2, 2) };
            _existingEntities.Add(existingItem);
            
            var position = new Vector2(2, 2); // Same position as item (triggers can overlap)
            
            // Act
            var canPlace = trigger.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsTrue(canPlace, "Trigger should allow overlap with other entities");
            Assert.AreEqual(EntityType.Trigger, trigger.Type);
        }
        
        [TestMethod]
        public void TriggerEntity_CanPlaceAt_SamePositionAsTrigger_ReturnsFalse()
        {
            // Arrange
            var trigger = new TriggerEntity();
            var existingTrigger = new TriggerEntity { Position = new Vector2(2, 2) };
            _existingEntities.Add(existingTrigger);
            
            var position = new Vector2(2, 2); // Same position as existing trigger
            
            // Act
            var canPlace = trigger.CanPlaceAt(position, _testTerrain, _existingEntities);
            
            // Assert
            Assert.IsFalse(canPlace, "Trigger should not overlap with another trigger at exact same position");
        }
        
        [TestMethod]
        public void AllEntityTypes_HaveCorrectType_AfterCreation()
        {
            // Test that all entity types are correctly set
            var entities = new Entity[]
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
            
            var expectedTypes = new EntityType[]
            {
                EntityType.Player,
                EntityType.Enemy,
                EntityType.Item,
                EntityType.PowerUp,
                EntityType.NPC,
                EntityType.Exit,
                EntityType.Checkpoint,
                EntityType.Obstacle,
                EntityType.Trigger
            };
            
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.AreEqual(expectedTypes[i], entities[i].Type, 
                    $"Entity {entities[i].GetType().Name} should have type {expectedTypes[i]}");
            }
        }
    }
}
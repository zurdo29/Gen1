using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Generators.PlacementStrategies;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Tests.Generators.PlacementStrategies
{
    public class PlacementStrategyTests
    {
        private readonly IRandomGenerator _random;
        private readonly PathfindingPlacementStrategy _pathfindingStrategy;
        private readonly TileMap _testTerrain;
        private readonly EntityConfig _testConfig;
        
        public PlacementStrategyTests()
        {
            _random = new RandomGenerator();
            _pathfindingStrategy = new PathfindingPlacementStrategy();
            
            // Create a simple test terrain (8x8 with walkable ground)
            _testTerrain = new TileMap(8, 8);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    // Create borders of walls, interior of ground
                    if (x == 0 || x == 7 || y == 0 || y == 7)
                        _testTerrain.SetTile(x, y, TileType.Wall);
                    else
                        _testTerrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            _testConfig = new EntityConfig
            {
                Type = EntityType.Enemy,
                Count = 1,
                PlacementStrategy = "pathfinding",
                MinDistance = 1.0f,
                MaxDistanceFromPlayer = float.MaxValue
            };
        }
        
        #region Terrain Type Tests
        
        [Fact]
        public void PathfindingStrategy_OnGroundTerrain_FindsValidPosition()
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
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(2, 2);
            existingEntities.Add(playerEntity);
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(groundTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            Assert.NotNull(position);
            Assert.True(groundTerrain.IsWalkable((int)position.Value.X, (int)position.Value.Y));
        }
        
        [Fact]
        public void PathfindingStrategy_OnGrassTerrain_FindsValidPosition()
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
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(1, 1);
            existingEntities.Add(playerEntity);
            
            var itemEntity = EntityFactory.CreateEntity(EntityType.Item);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(grassTerrain, _testConfig, existingEntities, itemEntity, _random);
            
            // Assert
            Assert.NotNull(position);
            Assert.Equal(TileType.Grass, grassTerrain.GetTile((int)position.Value.X, (int)position.Value.Y));
        }
        
        [Fact]
        public void PathfindingStrategy_OnSandTerrain_FindsValidPosition()
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
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(3, 3);
            existingEntities.Add(playerEntity);
            
            var powerUpEntity = EntityFactory.CreateEntity(EntityType.PowerUp);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(sandTerrain, _testConfig, existingEntities, powerUpEntity, _random);
            
            // Assert
            Assert.NotNull(position);
            Assert.Equal(TileType.Sand, sandTerrain.GetTile((int)position.Value.X, (int)position.Value.Y));
        }
        
        [Fact]
        public void PathfindingStrategy_OnMixedTerrain_FindsOnlyWalkablePositions()
        {
            // Arrange - Create mixed terrain with walkable and non-walkable tiles
            var mixedTerrain = new TileMap(6, 6);
            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    // Create a pattern with some walkable and some non-walkable tiles
                    if (x < 3 && y < 3)
                        mixedTerrain.SetTile(x, y, TileType.Ground); // Walkable area
                    else if (x >= 3 && y >= 3)
                        mixedTerrain.SetTile(x, y, TileType.Grass); // Another walkable area
                    else
                        mixedTerrain.SetTile(x, y, TileType.Wall); // Non-walkable barrier
                }
            }
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(1, 1); // In the ground area
            existingEntities.Add(playerEntity);
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(mixedTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            if (position.HasValue)
            {
                Assert.True(mixedTerrain.IsWalkable((int)position.Value.X, (int)position.Value.Y));
                
                // Should be in the same connected area as the player (ground area)
                Assert.True(position.Value.X < 3 && position.Value.Y < 3);
            }
        }
        
        [Fact]
        public void PathfindingStrategy_OnWaterTerrain_ReturnsNull()
        {
            // Arrange - Create terrain with mostly water (non-walkable)
            var waterTerrain = new TileMap(4, 4);
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    waterTerrain.SetTile(x, y, TileType.Water);
                }
            }
            // Add one walkable tile for player
            waterTerrain.SetTile(1, 1, TileType.Ground);
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(1, 1);
            existingEntities.Add(playerEntity);
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(waterTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            Assert.Null(position);
        }
        
        #endregion
        
        #region Valid Position Tests
        
        [Fact]
        public void PathfindingStrategy_WithValidTerrain_PlacesEntityInReachablePosition()
        {
            // Arrange
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(3, 3);
            existingEntities.Add(playerEntity);
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(_testTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            Assert.NotNull(position);
            Assert.True(_testTerrain.IsWalkable((int)position.Value.X, (int)position.Value.Y));
            
            // Verify the position is reachable from player position
            Assert.True(IsReachable(_testTerrain, playerEntity.Position, position.Value));
        }
        
        [Fact]
        public void PathfindingStrategy_WithMinDistanceConstraint_RespectsDistance()
        {
            // Arrange
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(3, 3);
            existingEntities.Add(playerEntity);
            
            var config = new EntityConfig
            {
                Type = EntityType.Enemy,
                PlacementStrategy = "pathfinding",
                MinDistance = 2.0f,
                MaxDistanceFromPlayer = float.MaxValue
            };
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(_testTerrain, config, existingEntities, enemyEntity, _random);
            
            // Assert
            if (position.HasValue)
            {
                var distance = Vector2.Distance(playerEntity.Position, position.Value);
                Assert.True(distance >= 2.0f);
            }
        }
        
        [Fact]
        public void PathfindingStrategy_WithMaxDistanceConstraint_RespectsDistance()
        {
            // Arrange
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(3, 3);
            existingEntities.Add(playerEntity);
            
            var config = new EntityConfig
            {
                Type = EntityType.Item,
                PlacementStrategy = "pathfinding",
                MinDistance = 0.0f,
                MaxDistanceFromPlayer = 2.0f
            };
            
            var itemEntity = EntityFactory.CreateEntity(EntityType.Item);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(_testTerrain, config, existingEntities, itemEntity, _random);
            
            // Assert
            if (position.HasValue)
            {
                var distance = Vector2.Distance(playerEntity.Position, position.Value);
                Assert.True(distance <= 2.0f);
            }
        }
        
        [Fact]
        public void PathfindingStrategy_WithNoPlayer_UsesRandomPlacement()
        {
            // Arrange - No player entity in existing entities
            var existingEntities = new List<Entity>();
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(_testTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            Assert.NotNull(position);
            Assert.True(_testTerrain.IsWalkable((int)position.Value.X, (int)position.Value.Y));
        }
        
        [Fact]
        public void PathfindingStrategy_WithMultipleExistingEntities_AvoidsCollisions()
        {
            // Arrange
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(3, 3);
            existingEntities.Add(playerEntity);
            
            var existingEnemy = EntityFactory.CreateEntity(EntityType.Enemy);
            existingEnemy.Position = new Vector2(4, 3);
            existingEntities.Add(existingEnemy);
            
            var newEnemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(_testTerrain, _testConfig, existingEntities, newEnemyEntity, _random);
            
            // Assert
            if (position.HasValue)
            {
                foreach (var entity in existingEntities)
                {
                    var distance = Vector2.Distance(entity.Position, position.Value);
                    Assert.True(distance >= _testConfig.MinDistance);
                }
            }
        }
        
        #endregion
        
        #region Impossible Placement Scenarios
        
        [Fact]
        public void PathfindingStrategy_WithNoWalkableTerrain_ReturnsNull()
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
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(1, 1); // Invalid position, but testing the strategy
            existingEntities.Add(playerEntity);
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(wallTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            Assert.Null(position);
        }
        
        [Fact]
        public void PathfindingStrategy_WithDisconnectedTerrain_PlacesOnlyInReachableArea()
        {
            // Arrange - Create terrain with two disconnected walkable areas
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
            disconnectedTerrain.SetTile(2, 1, TileType.Ground);
            
            disconnectedTerrain.SetTile(5, 5, TileType.Ground); // Area 2 (disconnected)
            disconnectedTerrain.SetTile(5, 4, TileType.Ground);
            disconnectedTerrain.SetTile(4, 5, TileType.Ground);
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(1, 1); // In area 1
            existingEntities.Add(playerEntity);
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(disconnectedTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            if (position.HasValue)
            {
                // Enemy should be placed in the same connected area as the player (area 1)
                Assert.True(position.Value.X <= 2 && position.Value.Y <= 2);
                
                Assert.True(IsReachable(disconnectedTerrain, playerEntity.Position, position.Value));
            }
        }
        
        [Fact]
        public void PathfindingStrategy_WithTightConstraints_ReturnsNullWhenImpossible()
        {
            // Arrange - Create very tight constraints that are impossible to satisfy
            var smallTerrain = new TileMap(3, 3);
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    smallTerrain.SetTile(x, y, TileType.Ground);
                }
            }
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(1, 1); // Center
            existingEntities.Add(playerEntity);
            
            var config = new EntityConfig
            {
                Type = EntityType.Enemy,
                PlacementStrategy = "pathfinding",
                MinDistance = 5.0f, // Impossible in a 3x3 terrain
                MaxDistanceFromPlayer = 1.0f // Conflicting with MinDistance
            };
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(smallTerrain, config, existingEntities, enemyEntity, _random);
            
            // Assert
            Assert.Null(position);
        }
        
        [Fact]
        public void PathfindingStrategy_WithSingleWalkableTile_ReturnsNullWhenOccupied()
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
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(1, 1); // Occupies the only walkable tile
            existingEntities.Add(playerEntity);
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(singleTileTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            Assert.Null(position);
        }
        
        [Fact]
        public void PathfindingStrategy_WithMazelikeTerrain_FindsPathThroughMaze()
        {
            // Arrange - Create a simple maze-like terrain
            var mazeTerrain = new TileMap(9, 9);
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    mazeTerrain.SetTile(x, y, TileType.Wall);
                }
            }
            
            // Create a simple path through the maze
            for (int x = 1; x < 8; x++)
            {
                mazeTerrain.SetTile(x, 1, TileType.Ground); // Horizontal corridor
            }
            for (int y = 1; y < 8; y++)
            {
                mazeTerrain.SetTile(7, y, TileType.Ground); // Vertical corridor
            }
            for (int x = 1; x < 8; x++)
            {
                mazeTerrain.SetTile(x, 7, TileType.Ground); // Another horizontal corridor
            }
            
            var existingEntities = new List<Entity>();
            var playerEntity = EntityFactory.CreateEntity(EntityType.Player);
            playerEntity.Position = new Vector2(1, 1); // Start of maze
            existingEntities.Add(playerEntity);
            
            var enemyEntity = EntityFactory.CreateEntity(EntityType.Enemy);
            
            // Act
            var position = _pathfindingStrategy.FindPosition(mazeTerrain, _testConfig, existingEntities, enemyEntity, _random);
            
            // Assert
            if (position.HasValue)
            {
                Assert.True(mazeTerrain.IsWalkable((int)position.Value.X, (int)position.Value.Y));
                
                Assert.True(IsReachable(mazeTerrain, playerEntity.Position, position.Value));
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Simple reachability check using flood fill algorithm
        /// </summary>
        private bool IsReachable(TileMap terrain, Vector2 start, Vector2 target)
        {
            if (!terrain.IsWalkable((int)start.X, (int)start.Y) || 
                !terrain.IsWalkable((int)target.X, (int)target.Y))
                return false;
            
            var visited = new bool[terrain.Width, terrain.Height];
            var queue = new Queue<Vector2>();
            
            queue.Enqueue(start);
            visited[(int)start.X, (int)start.Y] = true;
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                if (current == target)
                    return true;
                
                int x = (int)current.X;
                int y = (int)current.Y;
                
                // Check 4-directional movement
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (Math.Abs(dx) + Math.Abs(dy) > 1) continue; // Only 4-directional
                        
                        int newX = x + dx;
                        int newY = y + dy;
                        
                        if (newX >= 0 && newX < terrain.Width && newY >= 0 && newY < terrain.Height &&
                            !visited[newX, newY] && terrain.IsWalkable(newX, newY))
                        {
                            visited[newX, newY] = true;
                            queue.Enqueue(new Vector2(newX, newY));
                        }
                    }
                }
            }
            
            return false;
        }
        
        #endregion
    }
}
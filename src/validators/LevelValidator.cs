using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Validators
{
    /// <summary>
    /// Implementation of level validator that ensures generated levels are playable and high quality
    /// </summary>
    public class LevelValidator : ILevelValidator
    {
        private const float MinNavigabilityRatio = 0.3f; // At least 30% of the level should be navigable
        private const float MaxNavigabilityRatio = 0.9f; // At most 90% should be navigable (need some obstacles)
        private const int MinConnectedArea = 100; // Minimum size of connected navigable area
        
        /// <summary>
        /// Validates a level and returns any issues found
        /// </summary>
        /// <param name="level">Level to validate</param>
        /// <param name="issues">List of validation issues</param>
        /// <returns>True if level is valid</returns>
        public bool ValidateLevel(Level level, out List<string> issues)
        {
            issues = new List<string>();
            
            if (level == null)
            {
                issues.Add("Level is null");
                return false;
            }

            // Validate terrain
            ValidateTerrain(level.Terrain, issues);
            
            // Validate entities
            ValidateEntities(level.Entities, level.Terrain, issues);
            
            // Validate level structure
            ValidateLevelStructure(level, issues);
            
            // Validate navigability
            ValidateNavigability(level.Terrain, issues);
            
            // Validate gameplay elements
            ValidateGameplayElements(level, issues);

            return issues.Count == 0;
        }

        /// <summary>
        /// Checks if a level is playable
        /// </summary>
        /// <param name="level">Level to check</param>
        /// <returns>True if level is playable</returns>
        public bool IsPlayable(Level level)
        {
            if (level?.Terrain == null)
                return false;

            // Check basic playability requirements
            var navigabilityRatio = CalculateNavigabilityRatio(level.Terrain);
            if (navigabilityRatio < MinNavigabilityRatio)
                return false;

            // Check if there's a connected navigable area
            var largestConnectedArea = FindLargestConnectedArea(level.Terrain);
            if (largestConnectedArea < MinConnectedArea)
                return false;

            // Check if entities are in valid positions
            if (level.Entities != null)
            {
                foreach (var entity in level.Entities)
                {
                    var pos = entity.Position;
                    if (!level.Terrain.IsWalkable((int)pos.X, (int)pos.Y))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Evaluates the quality of a level
        /// </summary>
        /// <param name="level">Level to evaluate</param>
        /// <returns>Quality score (0.0 to 1.0)</returns>
        public float EvaluateQuality(Level level)
        {
            if (level?.Terrain == null)
                return 0.0f;

            float totalScore = 0.0f;
            int criteriaCount = 0;

            // Navigability score (0.0 to 1.0)
            var navigabilityScore = EvaluateNavigability(level.Terrain);
            totalScore += navigabilityScore;
            criteriaCount++;

            // Entity placement score (0.0 to 1.0)
            var entityScore = EvaluateEntityPlacement(level.Entities, level.Terrain);
            totalScore += entityScore;
            criteriaCount++;

            // Terrain variety score (0.0 to 1.0)
            var varietyScore = EvaluateTerrainVariety(level.Terrain);
            totalScore += varietyScore;
            criteriaCount++;

            // Level structure score (0.0 to 1.0)
            var structureScore = EvaluateLevelStructure(level);
            totalScore += structureScore;
            criteriaCount++;

            // Gameplay balance score (0.0 to 1.0)
            var balanceScore = EvaluateGameplayBalance(level);
            totalScore += balanceScore;
            criteriaCount++;

            return criteriaCount > 0 ? totalScore / criteriaCount : 0.0f;
        }

        /// <summary>
        /// Validates terrain properties
        /// </summary>
        private void ValidateTerrain(TileMap terrain, List<string> issues)
        {
            if (terrain == null)
            {
                issues.Add("Terrain is null");
                return;
            }

            if (terrain.Width <= 0 || terrain.Height <= 0)
            {
                issues.Add($"Invalid terrain dimensions: {terrain.Width}x{terrain.Height}");
            }

            if (terrain.Width < 10 || terrain.Height < 10)
            {
                issues.Add($"Terrain too small: {terrain.Width}x{terrain.Height} (minimum 10x10)");
            }

            // Check for completely blocked terrain
            var navigableCount = 0;
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    if (terrain.IsWalkable(x, y))
                        navigableCount++;
                }
            }

            if (navigableCount == 0)
            {
                issues.Add("Terrain has no navigable tiles");
            }
        }

        /// <summary>
        /// Validates entity placement
        /// </summary>
        private void ValidateEntities(List<Entity> entities, TileMap terrain, List<string> issues)
        {
            if (entities == null || terrain == null)
                return;

            var entityPositions = new HashSet<Vector2>();

            foreach (var entity in entities)
            {
                // Check if entity is within terrain bounds
                var pos = entity.Position;
                if (pos.X < 0 || pos.X >= terrain.Width || pos.Y < 0 || pos.Y >= terrain.Height)
                {
                    issues.Add($"Entity {entity.Type} at ({pos.X}, {pos.Y}) is outside terrain bounds");
                    continue;
                }

                // Check if entity is on a walkable tile
                if (!terrain.IsWalkable((int)pos.X, (int)pos.Y))
                {
                    issues.Add($"Entity {entity.Type} at ({pos.X}, {pos.Y}) is placed on non-walkable terrain");
                }

                // Check for overlapping entities
                if (entityPositions.Contains(pos))
                {
                    issues.Add($"Multiple entities placed at position ({pos.X}, {pos.Y})");
                }
                else
                {
                    entityPositions.Add(pos);
                }
            }
        }

        /// <summary>
        /// Validates overall level structure
        /// </summary>
        private void ValidateLevelStructure(Level level, List<string> issues)
        {
            if (string.IsNullOrEmpty(level.Name))
            {
                issues.Add("Level has no name");
            }

            // Check for essential entity types
            if (level.Entities != null)
            {
                var entityTypes = level.Entities.Select(e => e.Type).ToHashSet();
                
                if (!entityTypes.Contains(EntityType.Player))
                {
                    issues.Add("Level has no player spawn point");
                }

                if (!entityTypes.Contains(EntityType.Exit))
                {
                    issues.Add("Level has no exit point");
                }
            }
        }

        /// <summary>
        /// Validates navigability requirements
        /// </summary>
        private void ValidateNavigability(TileMap terrain, List<string> issues)
        {
            var navigabilityRatio = CalculateNavigabilityRatio(terrain);
            
            if (navigabilityRatio < MinNavigabilityRatio)
            {
                issues.Add($"Level has insufficient navigable area: {navigabilityRatio:P1} (minimum {MinNavigabilityRatio:P1})");
            }
            
            if (navigabilityRatio > MaxNavigabilityRatio)
            {
                issues.Add($"Level has too much open space: {navigabilityRatio:P1} (maximum {MaxNavigabilityRatio:P1})");
            }

            var largestConnectedArea = FindLargestConnectedArea(terrain);
            if (largestConnectedArea < MinConnectedArea)
            {
                issues.Add($"Largest connected area is too small: {largestConnectedArea} tiles (minimum {MinConnectedArea})");
            }
        }

        /// <summary>
        /// Validates gameplay elements
        /// </summary>
        private void ValidateGameplayElements(Level level, List<string> issues)
        {
            if (level.Entities == null)
                return;

            var entityCounts = level.Entities.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());

            // Check for reasonable enemy count
            var enemyCount = entityCounts.GetValueOrDefault(EntityType.Enemy, 0);
            var totalArea = level.Terrain.Width * level.Terrain.Height;
            var enemyDensity = (float)enemyCount / totalArea;

            if (enemyDensity > 0.1f) // More than 10% enemy density might be too much
            {
                issues.Add($"Enemy density too high: {enemyDensity:P1} ({enemyCount} enemies in {totalArea} tiles)");
            }

            // Check for multiple player spawns
            var playerCount = entityCounts.GetValueOrDefault(EntityType.Player, 0);
            if (playerCount > 1)
            {
                issues.Add($"Multiple player spawn points found: {playerCount}");
            }
        }

        /// <summary>
        /// Calculates the ratio of navigable to total tiles
        /// </summary>
        private float CalculateNavigabilityRatio(TileMap terrain)
        {
            int navigableCount = 0;
            int totalCount = terrain.Width * terrain.Height;

            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    if (terrain.IsWalkable(x, y))
                        navigableCount++;
                }
            }

            return totalCount > 0 ? (float)navigableCount / totalCount : 0.0f;
        }

        /// <summary>
        /// Finds the largest connected area of navigable tiles using flood fill
        /// </summary>
        private int FindLargestConnectedArea(TileMap terrain)
        {
            var visited = new bool[terrain.Width, terrain.Height];
            int largestArea = 0;

            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    if (!visited[x, y] && terrain.IsWalkable(x, y))
                    {
                        int areaSize = FloodFill(terrain, visited, x, y);
                        largestArea = Math.Max(largestArea, areaSize);
                    }
                }
            }

            return largestArea;
        }

        /// <summary>
        /// Performs flood fill to calculate connected area size
        /// </summary>
        private int FloodFill(TileMap terrain, bool[,] visited, int startX, int startY)
        {
            var stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));
            int areaSize = 0;

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();

                if (x < 0 || x >= terrain.Width || y < 0 || y >= terrain.Height)
                    continue;

                if (visited[x, y] || !terrain.IsWalkable(x, y))
                    continue;

                visited[x, y] = true;
                areaSize++;

                // Add adjacent cells
                stack.Push((x + 1, y));
                stack.Push((x - 1, y));
                stack.Push((x, y + 1));
                stack.Push((x, y - 1));
            }

            return areaSize;
        }

        /// <summary>
        /// Evaluates navigability quality
        /// </summary>
        private float EvaluateNavigability(TileMap terrain)
        {
            var ratio = CalculateNavigabilityRatio(terrain);
            
            // Optimal range is 0.4 to 0.7
            if (ratio >= 0.4f && ratio <= 0.7f)
                return 1.0f;
            
            // Penalize ratios outside optimal range
            if (ratio < 0.4f)
                return ratio / 0.4f;
            
            return Math.Max(0.0f, 1.0f - (ratio - 0.7f) / 0.3f);
        }

        /// <summary>
        /// Evaluates entity placement quality
        /// </summary>
        private float EvaluateEntityPlacement(List<Entity> entities, TileMap terrain)
        {
            if (entities == null || entities.Count == 0)
                return 0.5f; // Neutral score for no entities

            int validPlacements = 0;

            foreach (var entity in entities)
            {
                var pos = entity.Position;
                if (terrain.IsWalkable((int)pos.X, (int)pos.Y))
                {
                    validPlacements++;
                }
            }

            // Score based on percentage of valid placements
            return entities.Count > 0 ? (float)validPlacements / entities.Count : 1.0f;
        }

        /// <summary>
        /// Evaluates terrain variety
        /// </summary>
        private float EvaluateTerrainVariety(TileMap terrain)
        {
            var tileCounts = new Dictionary<TileType, int>();
            int totalTiles = terrain.Width * terrain.Height;

            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tileType = terrain.GetTile(x, y);
                    tileCounts[tileType] = tileCounts.GetValueOrDefault(tileType, 0) + 1;
                }
            }

            // Score based on number of different tile types and their distribution
            int uniqueTypes = tileCounts.Count;
            if (uniqueTypes <= 1)
                return 0.0f;

            // Calculate entropy-like measure for variety
            float entropy = 0.0f;
            foreach (var count in tileCounts.Values)
            {
                float probability = (float)count / totalTiles;
                if (probability > 0)
                {
                    entropy -= probability * (float)Math.Log(probability, 2);
                }
            }

            // Normalize entropy (max entropy for 8 tile types is ~3)
            return Math.Min(1.0f, entropy / 3.0f);
        }

        /// <summary>
        /// Evaluates level structure quality
        /// </summary>
        private float EvaluateLevelStructure(Level level)
        {
            float score = 0.0f;
            int criteria = 0;

            // Check for proper naming
            if (!string.IsNullOrEmpty(level.Name))
            {
                score += 1.0f;
            }
            criteria++;

            // Check for metadata completeness
            if (level.Metadata != null && level.Metadata.Count > 0)
            {
                score += 1.0f;
            }
            criteria++;

            return criteria > 0 ? score / criteria : 0.0f;
        }

        /// <summary>
        /// Evaluates gameplay balance
        /// </summary>
        private float EvaluateGameplayBalance(Level level)
        {
            if (level.Entities == null)
                return 0.5f;

            var entityCounts = level.Entities.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());
            
            float score = 1.0f;

            // Check for reasonable enemy to item ratio
            var enemies = entityCounts.GetValueOrDefault(EntityType.Enemy, 0);
            var items = entityCounts.GetValueOrDefault(EntityType.Item, 0) + 
                       entityCounts.GetValueOrDefault(EntityType.PowerUp, 0);

            if (enemies > 0 && items == 0)
            {
                score -= 0.3f; // Penalize levels with enemies but no items
            }

            // Check for player spawn
            if (!entityCounts.ContainsKey(EntityType.Player))
            {
                score -= 0.5f;
            }

            // Check for exit
            if (!entityCounts.ContainsKey(EntityType.Exit))
            {
                score -= 0.3f;
            }

            return Math.Max(0.0f, score);
        }
    }
}
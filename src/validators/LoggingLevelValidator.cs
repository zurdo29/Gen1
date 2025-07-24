using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Core;

namespace ProceduralMiniGameGenerator.Validators
{
    /// <summary>
    /// Level validator with comprehensive logging integration
    /// </summary>
    public class LoggingLevelValidator : ILevelValidator
    {
        private readonly ILevelValidator _baseValidator;
        private readonly ILoggerService _loggerService;

        public LoggingLevelValidator(ILevelValidator baseValidator, ILoggerService loggerService)
        {
            _baseValidator = baseValidator ?? throw new ArgumentNullException(nameof(baseValidator));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Validates a level with comprehensive logging
        /// </summary>
        public bool ValidateLevel(Level level, out List<string> issues)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting level validation", 
                    new { 
                        OperationId = operationId,
                        LevelName = level?.Name,
                        TerrainSize = level?.Terrain != null ? $"{level.Terrain.Width}x{level.Terrain.Height}" : "null",
                        EntityCount = level?.Entities?.Count ?? 0,
                        Operation = "LevelValidation"
                    });

                var isValid = _baseValidator.ValidateLevel(level, out issues);
                
                stopwatch.Stop();
                
                // Calculate validation metrics
                var validationMetrics = CalculateValidationMetrics(level, issues);
                
                LogPerformanceSafely(
                    "LevelValidation",
                    stopwatch.Elapsed,
                    new {
                        IsValid = isValid,
                        IssueCount = issues.Count,
                        ValidationComplexity = validationMetrics.ValidationComplexity,
                        TerrainTiles = level?.Terrain != null ? level.Terrain.Width * level.Terrain.Height : 0,
                        EntityCount = level?.Entities?.Count ?? 0,
                        ValidationRate = validationMetrics.ValidationRate
                    });

                var logLevel = isValid ? LogLevel.Information : LogLevel.Warning;
                LogSafely(logLevel, 
                    $"Level validation completed - {(isValid ? "Valid" : "Invalid")}", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level?.Name,
                        IsValid = isValid,
                        IssueCount = issues.Count,
                        Issues = issues,
                        ValidationMetrics = validationMetrics
                    });
                
                return isValid;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                issues = new List<string> { $"Validation failed with exception: {ex.Message}" };
                
                LogErrorSafely(ex, 
                    "Level validation failed with exception", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level?.Name
                    });
                
                return false;
            }
        }

        /// <summary>
        /// Checks if a level is playable with logging
        /// </summary>
        public bool IsPlayable(Level level)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting level playability check", 
                    new { 
                        OperationId = operationId,
                        LevelName = level?.Name,
                        TerrainSize = level?.Terrain != null ? $"{level.Terrain.Width}x{level.Terrain.Height}" : "null",
                        EntityCount = level?.Entities?.Count ?? 0,
                        Operation = "PlayabilityCheck"
                    });

                var isPlayable = _baseValidator.IsPlayable(level);
                
                stopwatch.Stop();
                
                // Calculate playability metrics
                var playabilityMetrics = CalculatePlayabilityMetrics(level);
                
                LogPerformanceSafely(
                    "PlayabilityCheck",
                    stopwatch.Elapsed,
                    new {
                        IsPlayable = isPlayable,
                        PlayabilityMetrics = playabilityMetrics,
                        TerrainTiles = level?.Terrain != null ? level.Terrain.Width * level.Terrain.Height : 0,
                        EntityCount = level?.Entities?.Count ?? 0
                    });

                var logLevel = isPlayable ? LogLevel.Information : LogLevel.Warning;
                LogSafely(logLevel, 
                    $"Level playability check completed - {(isPlayable ? "Playable" : "Not Playable")}", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level?.Name,
                        IsPlayable = isPlayable,
                        PlayabilityMetrics = playabilityMetrics
                    });
                
                return isPlayable;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Level playability check failed with exception", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level?.Name
                    });
                
                return false;
            }
        }

        /// <summary>
        /// Evaluates the quality of a level with logging
        /// </summary>
        public float EvaluateQuality(Level level)
        {
            var stopwatch = Stopwatch.StartNew();
            var operationId = Guid.NewGuid().ToString();
            
            try
            {
                LogSafely(LogLevel.Information, 
                    "Starting level quality evaluation", 
                    new { 
                        OperationId = operationId,
                        LevelName = level?.Name,
                        TerrainSize = level?.Terrain != null ? $"{level.Terrain.Width}x{level.Terrain.Height}" : "null",
                        EntityCount = level?.Entities?.Count ?? 0,
                        Operation = "QualityEvaluation"
                    });

                var qualityScore = _baseValidator.EvaluateQuality(level);
                
                stopwatch.Stop();
                
                // Calculate quality breakdown metrics
                var qualityMetrics = CalculateQualityMetrics(level, qualityScore);
                
                LogPerformanceSafely(
                    "QualityEvaluation",
                    stopwatch.Elapsed,
                    new {
                        QualityScore = qualityScore,
                        QualityGrade = GetQualityGrade(qualityScore),
                        QualityMetrics = qualityMetrics,
                        TerrainTiles = level?.Terrain != null ? level.Terrain.Width * level.Terrain.Height : 0,
                        EntityCount = level?.Entities?.Count ?? 0
                    });

                var logLevel = qualityScore >= 0.7f ? LogLevel.Information : 
                              qualityScore >= 0.4f ? LogLevel.Warning : LogLevel.Error;
                
                LogSafely(logLevel, 
                    $"Level quality evaluation completed - Score: {qualityScore:F2} ({GetQualityGrade(qualityScore)})", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level?.Name,
                        QualityScore = qualityScore,
                        QualityGrade = GetQualityGrade(qualityScore),
                        QualityMetrics = qualityMetrics
                    });
                
                return qualityScore;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogErrorSafely(ex, 
                    "Level quality evaluation failed with exception", 
                    new { 
                        OperationId = operationId,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        LevelName = level?.Name
                    });
                
                return 0.0f;
            }
        }

        /// <summary>
        /// Calculates validation metrics for logging
        /// </summary>
        private ValidationMetrics CalculateValidationMetrics(Level level, List<string> issues)
        {
            var metrics = new ValidationMetrics();
            
            if (level?.Terrain != null)
            {
                metrics.TerrainTiles = level.Terrain.Width * level.Terrain.Height;
                metrics.ValidationComplexity = metrics.TerrainTiles / 1000.0; // Normalized complexity
            }
            
            metrics.EntityCount = level?.Entities?.Count ?? 0;
            metrics.IssueCount = issues.Count;
            metrics.ValidationRate = metrics.TerrainTiles + metrics.EntityCount; // Items validated per second
            
            // Categorize issues
            metrics.CriticalIssues = 0;
            metrics.WarningIssues = 0;
            
            foreach (var issue in issues)
            {
                if (issue.Contains("null") || issue.Contains("no player") || issue.Contains("no exit") || 
                    issue.Contains("outside bounds") || issue.Contains("no navigable"))
                {
                    metrics.CriticalIssues++;
                }
                else
                {
                    metrics.WarningIssues++;
                }
            }
            
            return metrics;
        }

        /// <summary>
        /// Calculates playability metrics for logging
        /// </summary>
        private PlayabilityMetrics CalculatePlayabilityMetrics(Level level)
        {
            var metrics = new PlayabilityMetrics();
            
            if (level?.Terrain != null)
            {
                // Calculate navigability ratio
                int navigableCount = 0;
                int totalTiles = level.Terrain.Width * level.Terrain.Height;
                
                for (int x = 0; x < level.Terrain.Width; x++)
                {
                    for (int y = 0; y < level.Terrain.Height; y++)
                    {
                        if (level.Terrain.IsWalkable(x, y))
                            navigableCount++;
                    }
                }
                
                metrics.NavigabilityRatio = totalTiles > 0 ? (double)navigableCount / totalTiles : 0.0;
                metrics.TotalTiles = totalTiles;
                metrics.NavigableTiles = navigableCount;
            }
            
            if (level?.Entities != null)
            {
                metrics.EntityCount = level.Entities.Count;
                
                // Count entity types
                var entityTypeCounts = new Dictionary<string, int>();
                foreach (var entity in level.Entities)
                {
                    var typeName = entity.Type.ToString();
                    entityTypeCounts[typeName] = entityTypeCounts.GetValueOrDefault(typeName, 0) + 1;
                }
                
                metrics.HasPlayer = entityTypeCounts.ContainsKey("Player");
                metrics.HasExit = entityTypeCounts.ContainsKey("Exit");
                metrics.EnemyCount = entityTypeCounts.GetValueOrDefault("Enemy", 0);
                metrics.ItemCount = entityTypeCounts.GetValueOrDefault("Item", 0) + 
                                   entityTypeCounts.GetValueOrDefault("PowerUp", 0);
            }
            
            return metrics;
        }

        /// <summary>
        /// Calculates quality metrics for logging
        /// </summary>
        private QualityMetrics CalculateQualityMetrics(Level level, float overallScore)
        {
            var metrics = new QualityMetrics
            {
                OverallScore = overallScore,
                QualityGrade = GetQualityGrade(overallScore)
            };
            
            if (level?.Terrain != null)
            {
                // Calculate terrain variety
                var tileCounts = new Dictionary<TileType, int>();
                for (int x = 0; x < level.Terrain.Width; x++)
                {
                    for (int y = 0; y < level.Terrain.Height; y++)
                    {
                        var tileType = level.Terrain.GetTile(x, y);
                        tileCounts[tileType] = tileCounts.GetValueOrDefault(tileType, 0) + 1;
                    }
                }
                
                metrics.TerrainVariety = tileCounts.Count;
                metrics.TerrainBalance = CalculateTerrainBalance(tileCounts, level.Terrain.Width * level.Terrain.Height);
            }
            
            if (level?.Entities != null)
            {
                var entityTypes = new HashSet<EntityType>();
                foreach (var entity in level.Entities)
                {
                    entityTypes.Add(entity.Type);
                }
                
                metrics.EntityVariety = entityTypes.Count;
                metrics.EntityDensity = level.Terrain != null ? 
                    (double)level.Entities.Count / (level.Terrain.Width * level.Terrain.Height) : 0.0;
            }
            
            return metrics;
        }

        /// <summary>
        /// Calculates terrain balance score
        /// </summary>
        private double CalculateTerrainBalance(Dictionary<TileType, int> tileCounts, int totalTiles)
        {
            if (tileCounts.Count <= 1) return 0.0;
            
            // Calculate entropy for balance
            double entropy = 0.0;
            foreach (var count in tileCounts.Values)
            {
                double probability = (double)count / totalTiles;
                if (probability > 0)
                {
                    entropy -= probability * Math.Log(probability, 2);
                }
            }
            
            // Normalize entropy (max entropy for 8 tile types is ~3)
            return Math.Min(1.0, entropy / 3.0);
        }

        /// <summary>
        /// Gets quality grade from score
        /// </summary>
        private string GetQualityGrade(float score)
        {
            return score switch
            {
                >= 0.9f => "Excellent",
                >= 0.8f => "Good",
                >= 0.7f => "Fair",
                >= 0.5f => "Poor",
                _ => "Very Poor"
            };
        }

        /// <summary>
        /// Safely logs a message without throwing exceptions
        /// </summary>
        private void LogSafely(LogLevel level, string message, object context = null)
        {
            try
            {
                _loggerService.LogAsync(level, message, context).Wait();
            }
            catch
            {
                // Ignore logging errors to prevent disrupting the main operation
            }
        }

        /// <summary>
        /// Safely logs performance metrics without throwing exceptions
        /// </summary>
        private void LogPerformanceSafely(string operation, TimeSpan duration, object metrics = null)
        {
            try
            {
                _loggerService.LogPerformanceAsync(operation, duration, metrics).Wait();
            }
            catch
            {
                // Ignore logging errors to prevent disrupting the main operation
            }
        }

        /// <summary>
        /// Safely logs errors without throwing exceptions
        /// </summary>
        private void LogErrorSafely(Exception exception, string context, object additionalData = null)
        {
            try
            {
                _loggerService.LogErrorAsync(exception, context, additionalData).Wait();
            }
            catch
            {
                // Ignore logging errors to prevent disrupting the main operation
            }
        }

        /// <summary>
        /// Validation metrics for logging
        /// </summary>
        private class ValidationMetrics
        {
            public int TerrainTiles { get; set; }
            public int EntityCount { get; set; }
            public int IssueCount { get; set; }
            public int CriticalIssues { get; set; }
            public int WarningIssues { get; set; }
            public double ValidationComplexity { get; set; }
            public double ValidationRate { get; set; }
        }

        /// <summary>
        /// Playability metrics for logging
        /// </summary>
        private class PlayabilityMetrics
        {
            public int TotalTiles { get; set; }
            public int NavigableTiles { get; set; }
            public double NavigabilityRatio { get; set; }
            public int EntityCount { get; set; }
            public bool HasPlayer { get; set; }
            public bool HasExit { get; set; }
            public int EnemyCount { get; set; }
            public int ItemCount { get; set; }
        }

        /// <summary>
        /// Quality metrics for logging
        /// </summary>
        private class QualityMetrics
        {
            public float OverallScore { get; set; }
            public string QualityGrade { get; set; }
            public int TerrainVariety { get; set; }
            public double TerrainBalance { get; set; }
            public int EntityVariety { get; set; }
            public double EntityDensity { get; set; }
        }
    }
}
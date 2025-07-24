using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Models.Entities;

namespace ProceduralMiniGameGenerator.Core
{
    /// <summary>
    /// Implementation of level export and import service
    /// </summary>
    public class LevelExportService : ILevelExportService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        
        public LevelExportService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }
        
        /// <summary>
        /// Exports a level to JSON format with metadata and generation parameters
        /// </summary>
        public ExportResult ExportLevel(Level level, GenerationConfig generationConfig, string outputPath)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ExportResult { ExportPath = outputPath };
            
            try
            {
                // Validate inputs
                if (level == null)
                {
                    result.Errors.Add("Level cannot be null");
                    return result;
                }
                
                if (string.IsNullOrEmpty(outputPath))
                {
                    result.Errors.Add("Output path cannot be null or empty");
                    return result;
                }
                
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    result.Warnings.Add($"Created directory: {directory}");
                }
                
                // Generate JSON
                var json = ExportLevelToJson(level, generationConfig);
                
                // Write to file
                File.WriteAllText(outputPath, json);
                
                // Get file info
                var fileInfo = new FileInfo(outputPath);
                result.FileSize = fileInfo.Length;
                result.Success = true;
                
                Console.WriteLine($"✓ Level exported successfully to: {outputPath}");
                Console.WriteLine($"  File size: {result.FileSize:N0} bytes");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Export failed: {ex.Message}");
                Console.WriteLine($"❌ Export failed: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.ExportTime = stopwatch.Elapsed;
            }
            
            return result;
        }
        
        /// <summary>
        /// Exports a level to JSON string with metadata and generation parameters
        /// </summary>
        public string ExportLevelToJson(Level level, GenerationConfig generationConfig)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            
            var exportData = new LevelExportData
            {
                FormatVersion = "1.0",
                ExportTimestamp = DateTime.UtcNow,
                Level = new LevelData
                {
                    Name = level.Name,
                    Width = level.Terrain?.Width ?? 0,
                    Height = level.Terrain?.Height ?? 0,
                    Terrain = SerializeTerrain(level.Terrain),
                    Entities = SerializeEntities(level.Entities),
                    Metadata = level.Metadata
                },
                GenerationConfig = generationConfig,
                Statistics = CalculateLevelStatistics(level)
            };
            
            return JsonSerializer.Serialize(exportData, _jsonOptions);
        }
        
        /// <summary>
        /// Imports a level from JSON file
        /// </summary>
        public ImportResult ImportLevel(string jsonPath)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ImportResult();
            
            try
            {
                if (string.IsNullOrEmpty(jsonPath))
                {
                    result.Errors.Add("JSON path cannot be null or empty");
                    return result;
                }
                
                if (!File.Exists(jsonPath))
                {
                    result.Errors.Add($"File not found: {jsonPath}");
                    return result;
                }
                
                var json = File.ReadAllText(jsonPath);
                var importResult = ImportLevelFromJson(json);
                
                // Copy results
                result.Success = importResult.Success;
                result.Level = importResult.Level;
                result.GenerationConfig = importResult.GenerationConfig;
                result.Errors.AddRange(importResult.Errors);
                result.Warnings.AddRange(importResult.Warnings);
                
                if (result.Success)
                {
                    Console.WriteLine($"✓ Level imported successfully from: {jsonPath}");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Import failed: {ex.Message}");
                Console.WriteLine($"❌ Import failed: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.ImportTime = stopwatch.Elapsed;
            }
            
            return result;
        }
        
        /// <summary>
        /// Imports a level from JSON string
        /// </summary>
        public ImportResult ImportLevelFromJson(string json)
        {
            var result = new ImportResult();
            
            try
            {
                if (string.IsNullOrEmpty(json))
                {
                    result.Errors.Add("JSON string cannot be null or empty");
                    return result;
                }
                
                var exportData = JsonSerializer.Deserialize<LevelExportData>(json, _jsonOptions);
                
                if (exportData == null)
                {
                    result.Errors.Add("Failed to deserialize JSON data");
                    return result;
                }
                
                // Validate format version
                if (string.IsNullOrEmpty(exportData.FormatVersion))
                {
                    result.Warnings.Add("No format version specified, assuming version 1.0");
                }
                else if (exportData.FormatVersion != "1.0")
                {
                    result.Warnings.Add($"Format version {exportData.FormatVersion} may not be fully supported");
                }
                
                // Reconstruct level
                result.Level = ReconstructLevel(exportData.Level);
                result.GenerationConfig = exportData.GenerationConfig;
                
                // Validate reconstructed level
                var validationErrors = ValidateReconstructedLevel(result.Level);
                if (validationErrors.Count > 0)
                {
                    result.Errors.AddRange(validationErrors);
                    result.Success = false;
                }
                else
                {
                    result.Success = true;
                }
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Import error: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Validates that a JSON file contains valid level data
        /// </summary>
        public ValidationResult ValidateExportedLevel(string jsonPath)
        {
            var result = new ValidationResult();
            
            try
            {
                if (!File.Exists(jsonPath))
                {
                    result.Errors.Add($"File not found: {jsonPath}");
                    return result;
                }
                
                var json = File.ReadAllText(jsonPath);
                var importResult = ImportLevelFromJson(json);
                
                result.Errors.AddRange(importResult.Errors);
                result.Warnings.AddRange(importResult.Warnings);
                
                if (importResult.Success && importResult.Level != null)
                {
                    // Additional validation checks
                    if (string.IsNullOrEmpty(importResult.Level.Name))
                    {
                        result.Warnings.Add("Level has no name");
                    }
                    
                    if (importResult.Level.Terrain == null)
                    {
                        result.Errors.Add("Level has no terrain data");
                    }
                    
                    if (importResult.Level.Entities == null || importResult.Level.Entities.Count == 0)
                    {
                        result.Warnings.Add("Level has no entities");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Validation error: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets supported export formats
        /// </summary>
        public List<string> GetSupportedFormats()
        {
            return new List<string> { "JSON" };
        }
        
        private int[,] SerializeTerrain(TileMap? terrain)
        {
            if (terrain == null) 
                return new int[0, 0];
            
            var serialized = new int[terrain.Width, terrain.Height];
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    serialized[x, y] = (int)terrain.GetTile(x, y);
                }
            }
            return serialized;
        }
        
        private List<EntityData> SerializeEntities(List<Entity>? entities)
        {
            var serialized = new List<EntityData>();
            
            if (entities == null) 
                return serialized;
            
            foreach (var entity in entities)
            {
                serialized.Add(new EntityData
                {
                    Type = entity.Type.ToString(),
                    Position = new PositionData { X = entity.Position.X, Y = entity.Position.Y },
                    Properties = entity.Properties ?? new Dictionary<string, object>()
                });
            }
            
            return serialized;
        }
        
        private LevelStatistics CalculateLevelStatistics(Level level)
        {
            var stats = new LevelStatistics();
            
            if (level.Terrain != null)
            {
                stats.TotalTiles = level.Terrain.Width * level.Terrain.Height;
                
                // Count tile types
                var tileCounts = new Dictionary<TileType, int>();
                for (int x = 0; x < level.Terrain.Width; x++)
                {
                    for (int y = 0; y < level.Terrain.Height; y++)
                    {
                        var tileType = level.Terrain.GetTile(x, y);
                        tileCounts[tileType] = tileCounts.GetValueOrDefault(tileType, 0) + 1;
                    }
                }
                
                stats.WalkableTiles = tileCounts.GetValueOrDefault(TileType.Ground, 0) + 
                                     tileCounts.GetValueOrDefault(TileType.Grass, 0) + 
                                     tileCounts.GetValueOrDefault(TileType.Sand, 0);
                stats.WallTiles = tileCounts.GetValueOrDefault(TileType.Wall, 0);
                stats.WaterTiles = tileCounts.GetValueOrDefault(TileType.Water, 0);
            }
            
            if (level.Entities != null)
            {
                stats.TotalEntities = level.Entities.Count;
                
                var entityCounts = new Dictionary<EntityType, int>();
                foreach (var entity in level.Entities)
                {
                    entityCounts[entity.Type] = entityCounts.GetValueOrDefault(entity.Type, 0) + 1;
                }
                
                stats.PlayerCount = entityCounts.GetValueOrDefault(EntityType.Player, 0);
                stats.EnemyCount = entityCounts.GetValueOrDefault(EntityType.Enemy, 0);
                stats.ItemCount = entityCounts.GetValueOrDefault(EntityType.Item, 0);
            }
            
            return stats;
        }
        
        private Level ReconstructLevel(LevelData levelData)
        {
            var level = new Level
            {
                Name = levelData.Name ?? "Imported Level",
                Metadata = levelData.Metadata ?? new Dictionary<string, object>()
            };
            
            // Reconstruct terrain
            if (levelData.Terrain != null && levelData.Width > 0 && levelData.Height > 0)
            {
                level.Terrain = new TileMap(levelData.Width, levelData.Height);
                
                for (int x = 0; x < levelData.Width && x < levelData.Terrain.GetLength(0); x++)
                {
                    for (int y = 0; y < levelData.Height && y < levelData.Terrain.GetLength(1); y++)
                    {
                        var tileValue = levelData.Terrain[x, y];
                        if (Enum.IsDefined(typeof(TileType), tileValue))
                        {
                            level.Terrain.SetTile(x, y, (TileType)tileValue);
                        }
                    }
                }
            }
            
            // Reconstruct entities
            if (levelData.Entities != null)
            {
                foreach (var entityData in levelData.Entities)
                {
                    if (Enum.TryParse<EntityType>(entityData.Type, out var entityType))
                    {
                        var entity = CreateEntityFromType(entityType);
                        entity.Position = new System.Numerics.Vector2(
                            entityData.Position?.X ?? 0, 
                            entityData.Position?.Y ?? 0);
                        entity.Properties = entityData.Properties ?? new Dictionary<string, object>();
                        
                        level.Entities.Add(entity);
                    }
                }
            }
            
            return level;
        }
        
        private Entity CreateEntityFromType(EntityType entityType)
        {
            // This is a simplified factory method
            // In a real implementation, you'd have specific entity classes
            return entityType switch
            {
                EntityType.Player => new PlayerEntity(),
                EntityType.Enemy => new EnemyEntity(),
                EntityType.Item => new ItemEntity(),
                EntityType.PowerUp => new PowerUpEntity(),
                EntityType.Checkpoint => new CheckpointEntity(),
                EntityType.Exit => new ExitEntity(),
                _ => new GenericEntity(entityType)
            };
        }
        
        private List<string> ValidateReconstructedLevel(Level? level)
        {
            var errors = new List<string>();
            
            if (level == null)
            {
                errors.Add("Reconstructed level is null");
                return errors;
            }
            
            if (level.Terrain == null)
            {
                errors.Add("Level has no terrain data");
            }
            else
            {
                if (level.Terrain.Width <= 0 || level.Terrain.Height <= 0)
                {
                    errors.Add($"Invalid terrain dimensions: {level.Terrain.Width}x{level.Terrain.Height}");
                }
            }
            
            if (level.Entities == null)
            {
                errors.Add("Level entities list is null");
            }
            
            return errors;
        }
    }
}
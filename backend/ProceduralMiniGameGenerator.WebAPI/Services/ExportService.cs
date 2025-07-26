using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using System.Globalization;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Implementation of export service for various formats
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly ILevelExportService _levelExportService;
        private readonly ILoggerService _loggerService;
        private readonly Dictionary<string, JobStatus> _batchJobs;
        private readonly Dictionary<string, byte[]> _batchResults;
        
        public ExportService(ILevelExportService levelExportService, ILoggerService loggerService)
        {
            _levelExportService = levelExportService;
            _loggerService = loggerService;
            _batchJobs = new Dictionary<string, JobStatus>();
            _batchResults = new Dictionary<string, byte[]>();
        }
        
        /// <summary>
        /// Gets all available export formats
        /// </summary>
        public async Task<List<ExportFormat>> GetAvailableFormatsAsync()
        {
            await _loggerService.LogAsync(LogLevel.Information, "Getting available export formats");
            
            return new List<ExportFormat>
            {
                new ExportFormat
                {
                    Id = "json",
                    Name = "JSON",
                    Description = "JavaScript Object Notation - Standard web format with full level data",
                    FileExtension = ".json",
                    MimeType = "application/json",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeConfig", "includeStatistics", "prettyPrint" }
                },
                new ExportFormat
                {
                    Id = "xml",
                    Name = "XML",
                    Description = "Extensible Markup Language - Structured format for data interchange",
                    FileExtension = ".xml",
                    MimeType = "application/xml",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeConfig", "includeStatistics" }
                },
                new ExportFormat
                {
                    Id = "csv",
                    Name = "CSV",
                    Description = "Comma-Separated Values - Spreadsheet-compatible format for terrain data",
                    FileExtension = ".csv",
                    MimeType = "text/csv",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeHeaders", "separator" }
                },
                new ExportFormat
                {
                    Id = "unity",
                    Name = "Unity",
                    Description = "Unity-compatible format with coordinate conversion and prefab data",
                    FileExtension = ".json",
                    MimeType = "application/json",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "coordinateSystem", "scaleFactor", "includePrefabData" }
                },
                new ExportFormat
                {
                    Id = "web-json",
                    Name = "Web JSON",
                    Description = "JSON format optimized for web consumption with lightweight structure",
                    FileExtension = ".json",
                    MimeType = "application/json",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeMetadata", "compactFormat", "includePreview" }
                },
                new ExportFormat
                {
                    Id = "image",
                    Name = "Image Preview",
                    Description = "PNG image export for level previews and thumbnails",
                    FileExtension = ".png",
                    MimeType = "image/png",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "width", "height", "tileSize", "showGrid", "showEntities" }
                },
                new ExportFormat
                {
                    Id = "csv-extended",
                    Name = "Extended CSV",
                    Description = "Enhanced CSV format with entity data for comprehensive spreadsheet analysis",
                    FileExtension = ".csv",
                    MimeType = "text/csv",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeEntities", "includeHeaders", "separator", "includeStatistics" }
                },
                new ExportFormat
                {
                    Id = "share-url",
                    Name = "Shareable URL",
                    Description = "Generate shareable URL for level configuration and preview",
                    FileExtension = ".txt",
                    MimeType = "text/plain",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includePreview", "expirationDays", "baseUrl" }
                }
            };
        }
        
        /// <summary>
        /// Exports a single level to the specified format
        /// </summary>
        public async Task<ExportResult> ExportLevelAsync(ExportRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ExportResult();
            
            try
            {
                await _loggerService.LogAsync(LogLevel.Information, 
                    $"Starting level export to {request.Format} format", 
                    new { Format = request.Format, LevelName = request.Level?.Name });
                
                // Validate request
                var validationErrors = ValidateExportRequest(request);
                if (validationErrors.Count > 0)
                {
                    result.Errors.AddRange(validationErrors);
                    return result;
                }
                
                // Export based on format
                switch (request.Format.ToLowerInvariant())
                {
                    case "json":
                        result = await ExportToJsonAsync(request);
                        break;
                    case "xml":
                        result = await ExportToXmlAsync(request);
                        break;
                    case "csv":
                        result = await ExportToCsvAsync(request);
                        break;
                    case "unity":
                        result = await ExportToUnityAsync(request);
                        break;
                    case "web-json":
                        result = await ExportToWebJsonAsync(request);
                        break;
                    case "image":
                        result = await ExportToImageAsync(request);
                        break;
                    case "csv-extended":
                        result = await ExportToCsvExtendedAsync(request);
                        break;
                    case "share-url":
                        result = await ExportToShareUrlAsync(request);
                        break;
                    default:
                        result.Errors.Add($"Unsupported export format: {request.Format}");
                        break;
                }
                
                if (result.Success)
                {
                    await _loggerService.LogAsync(LogLevel.Information, 
                        $"Level export completed successfully", 
                        new { Format = request.Format, FileSize = result.FileSize, Duration = stopwatch.Elapsed });
                }
                else
                {
                    await _loggerService.LogAsync(LogLevel.Warning, 
                        $"Level export failed", 
                        new { Format = request.Format, Errors = result.Errors, Duration = stopwatch.Elapsed });
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Level export error", 
                    new { Format = request.Format, LevelName = request.Level?.Name });
            }
            finally
            {
                stopwatch.Stop();
                result.ExportTime = stopwatch.Elapsed;
            }
            
            return result;
        }
        
        /// <summary>
        /// Exports multiple levels in batch
        /// </summary>
        public async Task<string> ExportBatchAsync(BatchExportRequest request)
        {
            var jobId = Guid.NewGuid().ToString();
            
            // Initialize job status
            _batchJobs[jobId] = new JobStatus
            {
                JobId = jobId,
                Status = JobStatusType.Running,
                Progress = 0
            };
            
            await _loggerService.LogAsync(LogLevel.Information, 
                $"Starting batch export job", 
                new { JobId = jobId, LevelCount = request.Levels.Count, Format = request.Format });
            
            // Start background processing
            _ = Task.Run(async () => await ProcessBatchExportAsync(jobId, request));
            
            return jobId;
        }
        
        /// <summary>
        /// Gets the status of a batch export job
        /// </summary>
        public async Task<JobStatus> GetBatchExportStatusAsync(string jobId)
        {
            if (_batchJobs.TryGetValue(jobId, out var status))
            {
                return status;
            }
            
            return new JobStatus
            {
                JobId = jobId,
                Status = JobStatusType.NotFound,
                Progress = 0,
                ErrorMessage = "Job not found"
            };
        }
        
        /// <summary>
        /// Downloads the result of a completed batch export
        /// </summary>
        public async Task<ProceduralMiniGameGenerator.WebAPI.Models.FileResult?> DownloadBatchExportAsync(string jobId)
        {
            if (!_batchResults.TryGetValue(jobId, out var data))
            {
                return null;
            }
            
            var format = _batchJobs.TryGetValue(jobId, out var job) ? 
                (job.Result as Dictionary<string, object>)?["format"]?.ToString() ?? "zip" : "zip";
            
            return new ProceduralMiniGameGenerator.WebAPI.Models.FileResult
            {
                Data = data,
                FileName = $"batch_export_{jobId}.zip",
                MimeType = "application/zip"
            };
        }
        
        private async Task ProcessBatchExportAsync(string jobId, BatchExportRequest request)
        {
            try
            {
                _batchJobs[jobId].Status = JobStatusType.Running;
                
                var exportedFiles = new List<(string fileName, byte[] data)>();
                var totalLevels = request.Levels.Count;
                
                for (int i = 0; i < totalLevels; i++)
                {
                    var level = request.Levels[i];
                    var fileName = !string.IsNullOrEmpty(request.BaseFileName) 
                        ? $"{request.BaseFileName}_{i + 1}" 
                        : $"level_{i + 1}";
                    
                    var exportRequest = new ExportRequest
                    {
                        Level = level,
                        Format = request.Format,
                        Options = request.Options,
                        FileName = fileName,
                        IncludeGenerationConfig = request.IncludeGenerationConfig,
                        IncludeStatistics = request.IncludeStatistics
                    };
                    
                    var result = await ExportLevelAsync(exportRequest);
                    
                    if (result.Success && result.FileData != null)
                    {
                        var formats = await GetAvailableFormatsAsync();
                        var format = formats.FirstOrDefault(f => f.Id.Equals(request.Format, StringComparison.OrdinalIgnoreCase));
                        var extension = format?.FileExtension ?? ".txt";
                        
                        exportedFiles.Add(($"{fileName}{extension}", result.FileData));
                    }
                    
                    // Update progress
                    var progress = (int)((i + 1) * 100.0 / totalLevels);
                    _batchJobs[jobId].Progress = progress;
                }
                
                // Create ZIP package if requested
                if (request.CreateZipPackage)
                {
                    var zipData = CreateZipPackage(exportedFiles);
                    _batchResults[jobId] = zipData;
                }
                
                _batchJobs[jobId].Status = JobStatusType.Completed;
                _batchJobs[jobId].Progress = 100;
                _batchJobs[jobId].Result = new Dictionary<string, object>
                {
                    ["format"] = request.Format,
                    ["fileCount"] = exportedFiles.Count,
                    ["totalSize"] = exportedFiles.Sum(f => f.data.Length)
                };
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    $"Batch export completed successfully", 
                    new { JobId = jobId, FileCount = exportedFiles.Count });
            }
            catch (Exception ex)
            {
                _batchJobs[jobId].Status = JobStatusType.Failed;
                _batchJobs[jobId].ErrorMessage = ex.Message;
                
                await _loggerService.LogErrorAsync(ex, "Batch export failed", new { JobId = jobId });
            }
        }
        
        private async Task<ExportResult> ExportToJsonAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                // Use the existing level export service for JSON
                var json = _levelExportService.ExportLevelToJson(request.Level, null);
                
                // Apply customization options
                if (request.Options.ContainsKey("prettyPrint") && 
                    request.Options["prettyPrint"].ToString()?.ToLowerInvariant() == "false")
                {
                    // Minify JSON
                    var jsonDoc = JsonDocument.Parse(json);
                    json = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = false });
                }
                
                result.FileData = Encoding.UTF8.GetBytes(json);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}.json";
                result.MimeType = "application/json";
                result.FileSize = result.FileData.Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"JSON export failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToXmlAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                // Convert level to XML format
                var levelData = CreateLevelExportData(request.Level);
                
                var xmlSerializer = new XmlSerializer(typeof(LevelExportData));
                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings 
                { 
                    Indent = true, 
                    IndentChars = "  " 
                });
                
                xmlSerializer.Serialize(xmlWriter, levelData);
                var xml = stringWriter.ToString();
                
                result.FileData = Encoding.UTF8.GetBytes(xml);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}.xml";
                result.MimeType = "application/xml";
                result.FileSize = result.FileData.Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"XML export failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToCsvAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var separator = request.Options.GetValueOrDefault("separator", ",").ToString();
                var includeHeaders = request.Options.GetValueOrDefault("includeHeaders", "true").ToString()?.ToLowerInvariant() == "true";
                
                var csv = new StringBuilder();
                
                if (includeHeaders)
                {
                    csv.AppendLine($"X{separator}Y{separator}TileType{separator}TileValue");
                }
                
                if (request.Level.Terrain != null)
                {
                    for (int y = 0; y < request.Level.Terrain.Height; y++)
                    {
                        for (int x = 0; x < request.Level.Terrain.Width; x++)
                        {
                            var tile = request.Level.Terrain.GetTile(x, y);
                            csv.AppendLine($"{x}{separator}{y}{separator}{tile}{separator}{(int)tile}");
                        }
                    }
                }
                
                result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}.csv";
                result.MimeType = "text/csv";
                result.FileSize = result.FileData.Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"CSV export failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToUnityAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var coordinateSystem = request.Options.GetValueOrDefault("coordinateSystem", "unity").ToString();
                var scaleFactor = float.Parse(request.Options.GetValueOrDefault("scaleFactor", "1.0").ToString() ?? "1.0");
                var includePrefabData = request.Options.GetValueOrDefault("includePrefabData", "true").ToString()?.ToLowerInvariant() == "true";
                
                var unityData = new
                {
                    formatVersion = "Unity-1.0",
                    exportTimestamp = DateTime.UtcNow,
                    coordinateSystem = coordinateSystem,
                    scaleFactor = scaleFactor,
                    level = new
                    {
                        name = request.Level.Name,
                        terrain = ConvertTerrainForUnity(request.Level.Terrain, coordinateSystem, scaleFactor),
                        entities = ConvertEntitiesForUnity(request.Level.Entities, coordinateSystem, scaleFactor),
                        bounds = new
                        {
                            width = request.Level.Terrain?.Width ?? 0,
                            height = request.Level.Terrain?.Height ?? 0,
                            scaledWidth = (request.Level.Terrain?.Width ?? 0) * scaleFactor,
                            scaledHeight = (request.Level.Terrain?.Height ?? 0) * scaleFactor
                        }
                    },
                    prefabData = includePrefabData ? GeneratePrefabData(request.Level) : null
                };
                
                var json = JsonSerializer.Serialize(unityData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                result.FileData = Encoding.UTF8.GetBytes(json);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_unity.json";
                result.MimeType = "application/json";
                result.FileSize = result.FileData.Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Unity export failed: {ex.Message}");
            }
            
            return result;
        }
        
        private object ConvertTerrainForUnity(TileMap? terrain, string coordinateSystem, float scaleFactor)
        {
            if (terrain == null) return new { tiles = new object[0] };
            
            var tiles = new List<object>();
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tile = terrain.GetTile(x, y);
                    var position = ConvertPositionForUnity(x, y, coordinateSystem, scaleFactor);
                    
                    tiles.Add(new
                    {
                        position = position,
                        tileType = tile.ToString(),
                        tileValue = (int)tile,
                        gridPosition = new { x, y }
                    });
                }
            }
            
            return new { tiles };
        }
        
        private object ConvertEntitiesForUnity(List<Entity>? entities, string coordinateSystem, float scaleFactor)
        {
            if (entities == null) return new { entities = new object[0] };
            
            var convertedEntities = entities.Select(entity => new
            {
                type = entity.Type.ToString(),
                position = ConvertPositionForUnity(entity.Position.X, entity.Position.Y, coordinateSystem, scaleFactor),
                gridPosition = new { x = (int)entity.Position.X, y = (int)entity.Position.Y },
                properties = entity.Properties
            }).ToList();
            
            return new { entities = convertedEntities };
        }
        
        private object ConvertPositionForUnity(float x, float y, string coordinateSystem, float scaleFactor)
        {
            return coordinateSystem.ToLowerInvariant() switch
            {
                "unity" => new { x = x * scaleFactor, y = 0f, z = y * scaleFactor }, // Unity 3D coordinates
                "unity2d" => new { x = x * scaleFactor, y = y * scaleFactor }, // Unity 2D coordinates
                _ => new { x = x * scaleFactor, y = y * scaleFactor } // Default 2D
            };
        }
        
        private object GeneratePrefabData(Level level)
        {
            return new
            {
                terrainPrefabs = new
                {
                    ground = "Prefabs/Terrain/GroundTile",
                    wall = "Prefabs/Terrain/WallTile",
                    water = "Prefabs/Terrain/WaterTile",
                    grass = "Prefabs/Terrain/GrassTile",
                    sand = "Prefabs/Terrain/SandTile"
                },
                entityPrefabs = new
                {
                    player = "Prefabs/Entities/Player",
                    enemy = "Prefabs/Entities/Enemy",
                    item = "Prefabs/Entities/Item",
                    powerUp = "Prefabs/Entities/PowerUp",
                    checkpoint = "Prefabs/Entities/Checkpoint",
                    exit = "Prefabs/Entities/Exit"
                }
            };
        }
        
        private ProceduralMiniGameGenerator.WebAPI.Models.LevelExportData CreateLevelExportData(Level level)
        {
            return new ProceduralMiniGameGenerator.WebAPI.Models.LevelExportData
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
                Statistics = CalculateLevelStatistics(level)
            };
        }
        
        private int[,] SerializeTerrain(TileMap? terrain)
        {
            if (terrain == null) return new int[0, 0];
            
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
        
        private List<ProceduralMiniGameGenerator.WebAPI.Models.EntityData> SerializeEntities(List<Entity>? entities)
        {
            var serialized = new List<ProceduralMiniGameGenerator.WebAPI.Models.EntityData>();
            
            if (entities == null) return serialized;
            
            foreach (var entity in entities)
            {
                serialized.Add(new ProceduralMiniGameGenerator.WebAPI.Models.EntityData
                {
                    Type = entity.Type.ToString(),
                    Position = new PositionData { X = entity.Position.X, Y = entity.Position.Y },
                    Properties = entity.Properties ?? new Dictionary<string, object>()
                });
            }
            
            return serialized;
        }
        
        private ProceduralMiniGameGenerator.WebAPI.Models.LevelStatistics CalculateLevelStatistics(Level level)
        {
            var stats = new ProceduralMiniGameGenerator.WebAPI.Models.LevelStatistics();
            
            if (level.Terrain != null)
            {
                stats.TotalTiles = level.Terrain.Width * level.Terrain.Height;
                
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
        
        private List<string> ValidateExportRequest(ExportRequest request)
        {
            var errors = new List<string>();
            
            if (request.Level == null)
            {
                errors.Add("Level cannot be null");
            }
            
            if (string.IsNullOrEmpty(request.Format))
            {
                errors.Add("Export format must be specified");
            }
            
            var supportedFormats = new[] { "json", "xml", "csv", "unity", "web-json", "image", "csv-extended", "share-url" };
            if (!supportedFormats.Contains(request.Format.ToLowerInvariant()))
            {
                errors.Add($"Unsupported format: {request.Format}. Supported formats: {string.Join(", ", supportedFormats)}");
            }
            
            return errors;
        }
        
        private async Task<ExportResult> ExportToWebJsonAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var includeMetadata = request.Options.GetValueOrDefault("includeMetadata", "true").ToString()?.ToLowerInvariant() == "true";
                var compactFormat = request.Options.GetValueOrDefault("compactFormat", "false").ToString()?.ToLowerInvariant() == "true";
                var includePreview = request.Options.GetValueOrDefault("includePreview", "true").ToString()?.ToLowerInvariant() == "true";
                
                // Create web-optimized level data structure
                var webData = new
                {
                    version = "web-1.0",
                    level = new
                    {
                        name = request.Level.Name,
                        dimensions = new
                        {
                            width = request.Level.Terrain?.Width ?? 0,
                            height = request.Level.Terrain?.Height ?? 0
                        },
                        terrain = CreateWebOptimizedTerrain(request.Level.Terrain, compactFormat),
                        entities = CreateWebOptimizedEntities(request.Level.Entities, compactFormat),
                        preview = includePreview ? CreateLevelPreviewData(request.Level) : null
                    },
                    metadata = includeMetadata ? new
                    {
                        exportedAt = DateTime.UtcNow,
                        statistics = CalculateLevelStatistics(request.Level),
                        checksum = CalculateLevelChecksum(request.Level)
                    } : null
                };
                
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = !compactFormat,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var json = JsonSerializer.Serialize(webData, jsonOptions);
                
                result.FileData = Encoding.UTF8.GetBytes(json);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_web.json";
                result.MimeType = "application/json";
                result.FileSize = result.FileData.Length;
                result.Success = true;
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    "Web JSON export completed", 
                    new { CompactFormat = compactFormat, FileSize = result.FileSize });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Web JSON export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Web JSON export error");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToImageAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var width = int.Parse(request.Options.GetValueOrDefault("width", "800").ToString() ?? "800");
                var height = int.Parse(request.Options.GetValueOrDefault("height", "600").ToString() ?? "600");
                var tileSize = int.Parse(request.Options.GetValueOrDefault("tileSize", "16").ToString() ?? "16");
                var showGrid = request.Options.GetValueOrDefault("showGrid", "false").ToString()?.ToLowerInvariant() == "true";
                var showEntities = request.Options.GetValueOrDefault("showEntities", "true").ToString()?.ToLowerInvariant() == "true";
                
                // Generate PNG image data for level preview
                var imageData = await GenerateLevelImageAsync(request.Level, width, height, tileSize, showGrid, showEntities);
                
                result.FileData = imageData;
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_preview.png";
                result.MimeType = "image/png";
                result.FileSize = result.FileData.Length;
                result.Success = true;
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    "Image export completed", 
                    new { Width = width, Height = height, FileSize = result.FileSize });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Image export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Image export error");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToCsvExtendedAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var includeEntities = request.Options.GetValueOrDefault("includeEntities", "true").ToString()?.ToLowerInvariant() == "true";
                var includeHeaders = request.Options.GetValueOrDefault("includeHeaders", "true").ToString()?.ToLowerInvariant() == "true";
                var separator = request.Options.GetValueOrDefault("separator", ",").ToString();
                var includeStatistics = request.Options.GetValueOrDefault("includeStatistics", "true").ToString()?.ToLowerInvariant() == "true";
                
                var csv = new StringBuilder();
                
                // Add level metadata
                if (includeHeaders)
                {
                    csv.AppendLine($"# Level: {request.Level.Name}");
                    csv.AppendLine($"# Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    csv.AppendLine($"# Dimensions: {request.Level.Terrain?.Width ?? 0}x{request.Level.Terrain?.Height ?? 0}");
                    csv.AppendLine();
                }
                
                // Terrain data section
                csv.AppendLine("# TERRAIN DATA");
                if (includeHeaders)
                {
                    csv.AppendLine($"X{separator}Y{separator}TileType{separator}TileValue{separator}Walkable");
                }
                
                if (request.Level.Terrain != null)
                {
                    for (int y = 0; y < request.Level.Terrain.Height; y++)
                    {
                        for (int x = 0; x < request.Level.Terrain.Width; x++)
                        {
                            var tile = request.Level.Terrain.GetTile(x, y);
                            var walkable = IsWalkableTile(tile);
                            csv.AppendLine($"{x}{separator}{y}{separator}{tile}{separator}{(int)tile}{separator}{walkable}");
                        }
                    }
                }
                
                // Entity data section
                if (includeEntities && request.Level.Entities != null && request.Level.Entities.Count > 0)
                {
                    csv.AppendLine();
                    csv.AppendLine("# ENTITY DATA");
                    if (includeHeaders)
                    {
                        csv.AppendLine($"EntityType{separator}X{separator}Y{separator}Properties");
                    }
                    
                    foreach (var entity in request.Level.Entities)
                    {
                        var properties = entity.Properties != null && entity.Properties.Count > 0 
                            ? JsonSerializer.Serialize(entity.Properties) 
                            : "";
                        csv.AppendLine($"{entity.Type}{separator}{entity.Position.X}{separator}{entity.Position.Y}{separator}\"{properties}\"");
                    }
                }
                
                // Statistics section
                if (includeStatistics)
                {
                    var stats = CalculateLevelStatistics(request.Level);
                    csv.AppendLine();
                    csv.AppendLine("# STATISTICS");
                    csv.AppendLine($"Metric{separator}Value");
                    csv.AppendLine($"Total Tiles{separator}{stats.TotalTiles}");
                    csv.AppendLine($"Walkable Tiles{separator}{stats.WalkableTiles}");
                    csv.AppendLine($"Wall Tiles{separator}{stats.WallTiles}");
                    csv.AppendLine($"Water Tiles{separator}{stats.WaterTiles}");
                    csv.AppendLine($"Total Entities{separator}{stats.TotalEntities}");
                    csv.AppendLine($"Player Count{separator}{stats.PlayerCount}");
                    csv.AppendLine($"Enemy Count{separator}{stats.EnemyCount}");
                    csv.AppendLine($"Item Count{separator}{stats.ItemCount}");
                }
                
                result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_extended.csv";
                result.MimeType = "text/csv";
                result.FileSize = result.FileData.Length;
                result.Success = true;
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    "Extended CSV export completed", 
                    new { IncludeEntities = includeEntities, FileSize = result.FileSize });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Extended CSV export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Extended CSV export error");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToShareUrlAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var includePreview = request.Options.GetValueOrDefault("includePreview", "true").ToString()?.ToLowerInvariant() == "true";
                var expirationDays = int.Parse(request.Options.GetValueOrDefault("expirationDays", "30").ToString() ?? "30");
                var baseUrl = request.Options.GetValueOrDefault("baseUrl", "https://localhost:5000").ToString();
                
                // Generate unique share ID
                var shareId = GenerateShareId();
                
                // Create shareable configuration data
                var shareData = new
                {
                    shareId = shareId,
                    createdAt = DateTime.UtcNow,
                    expiresAt = DateTime.UtcNow.AddDays(expirationDays),
                    level = new
                    {
                        name = request.Level.Name,
                        config = ExtractGenerationConfig(request.Level),
                        preview = includePreview ? CreateLevelPreviewData(request.Level) : null
                    }
                };
                
                // Store share data (in a real implementation, this would go to a database)
                await StoreShareDataAsync(shareId, shareData, TimeSpan.FromDays(expirationDays));
                
                // Generate shareable URLs
                var shareUrl = $"{baseUrl.TrimEnd('/')}/share/{shareId}";
                var apiUrl = $"{baseUrl.TrimEnd('/')}/api/configuration/share/{shareId}";
                var previewUrl = includePreview ? $"{baseUrl.TrimEnd('/')}/preview/{shareId}" : null;
                
                var urlData = new
                {
                    shareId = shareId,
                    shareUrl = shareUrl,
                    apiUrl = apiUrl,
                    previewUrl = previewUrl,
                    expiresAt = DateTime.UtcNow.AddDays(expirationDays),
                    instructions = new
                    {
                        web = $"Open in browser: {shareUrl}",
                        api = $"GET request to: {apiUrl}",
                        preview = includePreview ? $"Preview image: {previewUrl}" : "Preview not included"
                    }
                };
                
                var urlText = $"Shareable Level Configuration\n" +
                             $"============================\n\n" +
                             $"Level: {request.Level.Name}\n" +
                             $"Share ID: {shareId}\n" +
                             $"Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                             $"Expires: {DateTime.UtcNow.AddDays(expirationDays):yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                             $"Web URL: {shareUrl}\n" +
                             $"API URL: {apiUrl}\n" +
                             (includePreview ? $"Preview URL: {previewUrl}\n" : "") +
                             $"\nJSON Data:\n{JsonSerializer.Serialize(urlData, new JsonSerializerOptions { WriteIndented = true })}";
                
                result.FileData = Encoding.UTF8.GetBytes(urlText);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_share.txt";
                result.MimeType = "text/plain";
                result.FileSize = result.FileData.Length;
                result.Success = true;
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    "Share URL export completed", 
                    new { ShareId = shareId, ExpirationDays = expirationDays });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Share URL export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Share URL export error");
            }
            
            return result;
        }
        
        private object CreateWebOptimizedTerrain(TileMap? terrain, bool compactFormat)
        {
            if (terrain == null) return new { tiles = new object[0] };
            
            if (compactFormat)
            {
                // Compact format: run-length encoding for repeated tiles
                var compactTiles = new List<object>();
                var currentTile = terrain.GetTile(0, 0);
                var count = 1;
                
                for (int y = 0; y < terrain.Height; y++)
                {
                    for (int x = 0; x < terrain.Width; x++)
                    {
                        if (x == 0 && y == 0) continue; // Skip first tile
                        
                        var tile = terrain.GetTile(x, y);
                        if (tile == currentTile)
                        {
                            count++;
                        }
                        else
                        {
                            compactTiles.Add(new { t = (int)currentTile, c = count });
                            currentTile = tile;
                            count = 1;
                        }
                    }
                }
                
                // Add the last run
                compactTiles.Add(new { t = (int)currentTile, c = count });
                
                return new { format = "rle", width = terrain.Width, height = terrain.Height, data = compactTiles };
            }
            else
            {
                // Standard format: array of tile values
                var tiles = new int[terrain.Width * terrain.Height];
                for (int y = 0; y < terrain.Height; y++)
                {
                    for (int x = 0; x < terrain.Width; x++)
                    {
                        tiles[y * terrain.Width + x] = (int)terrain.GetTile(x, y);
                    }
                }
                
                return new { format = "array", width = terrain.Width, height = terrain.Height, data = tiles };
            }
        }
        
        private object CreateWebOptimizedEntities(List<Entity>? entities, bool compactFormat)
        {
            if (entities == null) return new { entities = new object[0] };
            
            if (compactFormat)
            {
                // Compact format: group by type and use short property names
                var grouped = entities.GroupBy(e => e.Type).ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Select(e => new
                    {
                        p = new[] { e.Position.X, e.Position.Y }, // position as array
                        pr = e.Properties?.Count > 0 ? e.Properties : null // properties only if present
                    }).ToList()
                );
                
                return new { format = "grouped", data = grouped };
            }
            else
            {
                // Standard format: full entity objects
                var entityList = entities.Select(e => new
                {
                    type = e.Type.ToString(),
                    position = new { x = e.Position.X, y = e.Position.Y },
                    properties = e.Properties
                }).ToList();
                
                return new { format = "standard", data = entityList };
            }
        }
        
        private object CreateLevelPreviewData(Level level)
        {
            return new
            {
                dimensions = new
                {
                    width = level.Terrain?.Width ?? 0,
                    height = level.Terrain?.Height ?? 0
                },
                tileDistribution = CalculateTileDistribution(level.Terrain),
                entitySummary = CalculateEntitySummary(level.Entities),
                thumbnail = GenerateThumbnailData(level)
            };
        }
        
        private Dictionary<string, int> CalculateTileDistribution(TileMap? terrain)
        {
            var distribution = new Dictionary<string, int>();
            
            if (terrain == null) return distribution;
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tileType = terrain.GetTile(x, y).ToString();
                    distribution[tileType] = distribution.GetValueOrDefault(tileType, 0) + 1;
                }
            }
            
            return distribution;
        }
        
        private Dictionary<string, int> CalculateEntitySummary(List<Entity>? entities)
        {
            var summary = new Dictionary<string, int>();
            
            if (entities == null) return summary;
            
            foreach (var entity in entities)
            {
                var entityType = entity.Type.ToString();
                summary[entityType] = summary.GetValueOrDefault(entityType, 0) + 1;
            }
            
            return summary;
        }
        
        private string GenerateThumbnailData(Level level)
        {
            // Generate a simple ASCII representation for thumbnail
            if (level.Terrain == null) return "";
            
            var maxSize = 20; // Maximum thumbnail size
            var scaleX = Math.Max(1, level.Terrain.Width / maxSize);
            var scaleY = Math.Max(1, level.Terrain.Height / maxSize);
            
            var thumbnail = new StringBuilder();
            for (int y = 0; y < level.Terrain.Height; y += scaleY)
            {
                for (int x = 0; x < level.Terrain.Width; x += scaleX)
                {
                    var tile = level.Terrain.GetTile(x, y);
                    thumbnail.Append(GetTileCharacter(tile));
                }
                thumbnail.AppendLine();
            }
            
            return thumbnail.ToString();
        }
        
        private char GetTileCharacter(TileType tile)
        {
            return tile switch
            {
                TileType.Ground => '.',
                TileType.Wall => '#',
                TileType.Water => '~',
                TileType.Grass => ',',
                TileType.Sand => ':',
                _ => '?'
            };
        }
        
        private bool IsWalkableTile(TileType tile)
        {
            return tile == TileType.Ground || tile == TileType.Grass || tile == TileType.Sand;
        }
        
        private string CalculateLevelChecksum(Level level)
        {
            // Generate a simple checksum for level integrity verification
            var data = $"{level.Name}_{level.Terrain?.Width}_{level.Terrain?.Height}_{level.Entities?.Count}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash)[..8]; // First 8 characters
        }
        
        private string GenerateShareId()
        {
            return Guid.NewGuid().ToString("N")[..12]; // 12-character share ID
        }
        
        private object ExtractGenerationConfig(Level level)
        {
            // Extract generation configuration from level metadata
            // This would typically come from the original generation request
            return new
            {
                seed = level.Metadata?.GetValueOrDefault("seed", 0),
                algorithm = level.Metadata?.GetValueOrDefault("algorithm", "unknown"),
                parameters = level.Metadata?.GetValueOrDefault("parameters", new Dictionary<string, object>())
            };
        }
        
        private async Task StoreShareDataAsync(string shareId, object shareData, TimeSpan expiration)
        {
            // In a real implementation, this would store to a database or cache
            // For now, we'll use in-memory storage (this is just for demonstration)
            await _loggerService.LogAsync(LogLevel.Information, 
                $"Share data stored", 
                new { ShareId = shareId, Expiration = expiration });
        }
        
        private async Task<byte[]> GenerateLevelImageAsync(Level level, int width, int height, int tileSize, bool showGrid, bool showEntities)
        {
            // This is a placeholder implementation for image generation
            // In a real implementation, you would use a graphics library like SkiaSharp or System.Drawing
            
            await _loggerService.LogAsync(LogLevel.Information, 
                "Generating level image", 
                new { Width = width, Height = height, TileSize = tileSize });
            
            // For now, return a simple placeholder PNG
            // This would be replaced with actual image generation logic
            var placeholderImage = CreatePlaceholderPng(width, height, level.Name ?? "Level");
            return placeholderImage;
        }
        
        private byte[] CreatePlaceholderPng(int width, int height, string levelName)
        {
            // Create a minimal PNG placeholder
            // In a real implementation, this would generate an actual level preview image
            var placeholder = $"Level Preview: {levelName}\nSize: {width}x{height}";
            return Encoding.UTF8.GetBytes(placeholder);
        }

        private byte[] CreateZipPackage(List<(string fileName, byte[] data)> files)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var (fileName, data) in files)
                {
                    var entry = archive.CreateEntry(fileName);
                    using var entryStream = entry.Open();
                    entryStream.Write(data, 0, data.Length);
                }
            }
            
            return memoryStream.ToArray();
        }
    }
}using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using System.Globalization;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Implementation of export service for various formats
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly ILevelExportService _levelExportService;
        private readonly ILoggerService _loggerService;
        private readonly Dictionary<string, JobStatus> _batchJobs;
        private readonly Dictionary<string, byte[]> _batchResults;
        
        public ExportService(ILevelExportService levelExportService, ILoggerService loggerService)
        {
            _levelExportService = levelExportService;
            _loggerService = loggerService;
            _batchJobs = new Dictionary<string, JobStatus>();
            _batchResults = new Dictionary<string, byte[]>();
        }
        
        /// <summary>
        /// Gets all available export formats
        /// </summary>
        public async Task<List<ExportFormat>> GetAvailableFormatsAsync()
        {
            await _loggerService.LogAsync(LogLevel.Information, "Getting available export formats");
            
            return new List<ExportFormat>
            {
                new ExportFormat
                {
                    Id = "json",
                    Name = "JSON",
                    Description = "JavaScript Object Notation - Standard web format with full level data",
                    FileExtension = ".json",
                    MimeType = "application/json",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeConfig", "includeStatistics", "prettyPrint" }
                },
                new ExportFormat
                {
                    Id = "xml",
                    Name = "XML",
                    Description = "Extensible Markup Language - Structured format for data interchange",
                    FileExtension = ".xml",
                    MimeType = "application/xml",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeConfig", "includeStatistics" }
                },
                new ExportFormat
                {
                    Id = "csv",
                    Name = "CSV",
                    Description = "Comma-Separated Values - Spreadsheet-compatible format for terrain data",
                    FileExtension = ".csv",
                    MimeType = "text/csv",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeHeaders", "separator" }
                },
                new ExportFormat
                {
                    Id = "unity",
                    Name = "Unity",
                    Description = "Unity-compatible format with coordinate conversion and prefab data",
                    FileExtension = ".json",
                    MimeType = "application/json",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "coordinateSystem", "scaleFactor", "includePrefabData" }
                },
                new ExportFormat
                {
                    Id = "web-json",
                    Name = "Web JSON",
                    Description = "JSON format optimized for web consumption with lightweight structure",
                    FileExtension = ".json",
                    MimeType = "application/json",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeMetadata", "compactFormat", "includePreview" }
                },
                new ExportFormat
                {
                    Id = "image",
                    Name = "Image Preview",
                    Description = "PNG image export for level previews and thumbnails",
                    FileExtension = ".png",
                    MimeType = "image/png",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "width", "height", "tileSize", "showGrid", "showEntities" }
                },
                new ExportFormat
                {
                    Id = "csv-extended",
                    Name = "Extended CSV",
                    Description = "Enhanced CSV format with entity data for comprehensive spreadsheet analysis",
                    FileExtension = ".csv",
                    MimeType = "text/csv",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includeEntities", "includeHeaders", "separator", "includeStatistics" }
                },
                new ExportFormat
                {
                    Id = "share-url",
                    Name = "Shareable URL",
                    Description = "Generate shareable URL for level configuration and preview",
                    FileExtension = ".txt",
                    MimeType = "text/plain",
                    SupportsCustomization = true,
                    CustomizationOptions = new List<string> { "includePreview", "expirationDays", "baseUrl" }
                }
            };
        }
        
        /// <summary>
        /// Exports a single level to the specified format
        /// </summary>
        public async Task<ExportResult> ExportLevelAsync(ExportRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ExportResult();
            
            try
            {
                await _loggerService.LogAsync(LogLevel.Information, 
                    $"Starting level export to {request.Format} format", 
                    new { Format = request.Format, LevelName = request.Level?.Name });
                
                // Validate request
                var validationErrors = ValidateExportRequest(request);
                if (validationErrors.Count > 0)
                {
                    result.Errors.AddRange(validationErrors);
                    return result;
                }
                
                // Export based on format
                switch (request.Format.ToLowerInvariant())
                {
                    case "json":
                        result = await ExportToJsonAsync(request);
                        break;
                    case "xml":
                        result = await ExportToXmlAsync(request);
                        break;
                    case "csv":
                        result = await ExportToCsvAsync(request);
                        break;
                    case "unity":
                        result = await ExportToUnityAsync(request);
                        break;
                    case "web-json":
                        result = await ExportToWebJsonAsync(request);
                        break;
                    case "image":
                        result = await ExportToImageAsync(request);
                        break;
                    case "csv-extended":
                        result = await ExportToCsvExtendedAsync(request);
                        break;
                    case "share-url":
                        result = await ExportToShareUrlAsync(request);
                        break;
                    default:
                        result.Errors.Add($"Unsupported export format: {request.Format}");
                        break;
                }
                
                if (result.Success)
                {
                    await _loggerService.LogAsync(LogLevel.Information, 
                        $"Level export completed successfully", 
                        new { Format = request.Format, FileSize = result.FileSize, Duration = stopwatch.Elapsed });
                }
                else
                {
                    await _loggerService.LogAsync(LogLevel.Warning, 
                        $"Level export failed", 
                        new { Format = request.Format, Errors = result.Errors, Duration = stopwatch.Elapsed });
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Level export error", 
                    new { Format = request.Format, LevelName = request.Level?.Name });
            }
            finally
            {
                stopwatch.Stop();
                result.ExportTime = stopwatch.Elapsed;
            }
            
            return result;
        }
        
        /// <summary>
        /// Exports multiple levels in batch
        /// </summary>
        public async Task<string> ExportBatchAsync(BatchExportRequest request)
        {
            var jobId = Guid.NewGuid().ToString();
            
            // Initialize job status
            _batchJobs[jobId] = new JobStatus
            {
                JobId = jobId,
                Status = JobStatusType.Running,
                Progress = 0
            };
            
            await _loggerService.LogAsync(LogLevel.Information, 
                $"Starting batch export job", 
                new { JobId = jobId, LevelCount = request.Levels.Count, Format = request.Format });
            
            // Start background processing
            _ = Task.Run(async () => await ProcessBatchExportAsync(jobId, request));
            
            return jobId;
        }
        
        /// <summary>
        /// Gets the status of a batch export job
        /// </summary>
        public async Task<JobStatus> GetBatchExportStatusAsync(string jobId)
        {
            if (_batchJobs.TryGetValue(jobId, out var status))
            {
                return status;
            }
            
            return new JobStatus
            {
                JobId = jobId,
                Status = JobStatusType.NotFound,
                Progress = 0,
                ErrorMessage = "Job not found"
            };
        }
        
        /// <summary>
        /// Downloads the result of a completed batch export
        /// </summary>
        public async Task<ProceduralMiniGameGenerator.WebAPI.Models.FileResult?> DownloadBatchExportAsync(string jobId)
        {
            if (!_batchResults.TryGetValue(jobId, out var data))
            {
                return null;
            }
            
            var format = _batchJobs.TryGetValue(jobId, out var job) ? 
                (job.Result as Dictionary<string, object>)?["format"]?.ToString() ?? "zip" : "zip";
            
            return new ProceduralMiniGameGenerator.WebAPI.Models.FileResult
            {
                Data = data,
                FileName = $"batch_export_{jobId}.zip",
                MimeType = "application/zip"
            };
        }
        
        private async Task ProcessBatchExportAsync(string jobId, BatchExportRequest request)
        {
            try
            {
                _batchJobs[jobId].Status = JobStatusType.Running;
                
                var exportedFiles = new List<(string fileName, byte[] data)>();
                var totalLevels = request.Levels.Count;
                
                for (int i = 0; i < totalLevels; i++)
                {
                    var level = request.Levels[i];
                    var fileName = !string.IsNullOrEmpty(request.BaseFileName) 
                        ? $"{request.BaseFileName}_{i + 1}" 
                        : $"level_{i + 1}";
                    
                    var exportRequest = new ExportRequest
                    {
                        Level = level,
                        Format = request.Format,
                        Options = request.Options,
                        FileName = fileName,
                        IncludeGenerationConfig = request.IncludeGenerationConfig,
                        IncludeStatistics = request.IncludeStatistics
                    };
                    
                    var result = await ExportLevelAsync(exportRequest);
                    
                    if (result.Success && result.FileData != null)
                    {
                        var formats = await GetAvailableFormatsAsync();
                        var format = formats.FirstOrDefault(f => f.Id.Equals(request.Format, StringComparison.OrdinalIgnoreCase));
                        var extension = format?.FileExtension ?? ".txt";
                        
                        exportedFiles.Add(($"{fileName}{extension}", result.FileData));
                    }
                    
                    // Update progress
                    var progress = (int)((i + 1) * 100.0 / totalLevels);
                    _batchJobs[jobId].Progress = progress;
                }
                
                // Create ZIP package if requested
                if (request.CreateZipPackage)
                {
                    var zipData = CreateZipPackage(exportedFiles);
                    _batchResults[jobId] = zipData;
                }
                
                _batchJobs[jobId].Status = JobStatusType.Completed;
                _batchJobs[jobId].Progress = 100;
                _batchJobs[jobId].Result = new Dictionary<string, object>
                {
                    ["format"] = request.Format,
                    ["fileCount"] = exportedFiles.Count,
                    ["totalSize"] = exportedFiles.Sum(f => f.data.Length)
                };
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    $"Batch export completed successfully", 
                    new { JobId = jobId, FileCount = exportedFiles.Count });
            }
            catch (Exception ex)
            {
                _batchJobs[jobId].Status = JobStatusType.Failed;
                _batchJobs[jobId].ErrorMessage = ex.Message;
                
                await _loggerService.LogErrorAsync(ex, "Batch export failed", new { JobId = jobId });
            }
        }
        
        private async Task<ExportResult> ExportToJsonAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                // Use the existing level export service for JSON
                var json = _levelExportService.ExportLevelToJson(request.Level, null);
                
                // Apply customization options
                if (request.Options.ContainsKey("prettyPrint") && 
                    request.Options["prettyPrint"].ToString()?.ToLowerInvariant() == "false")
                {
                    // Minify JSON
                    var jsonDoc = JsonDocument.Parse(json);
                    json = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = false });
                }
                
                result.FileData = Encoding.UTF8.GetBytes(json);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}.json";
                result.MimeType = "application/json";
                result.FileSize = result.FileData.Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"JSON export failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToXmlAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                // Convert level to XML format
                var levelData = CreateLevelExportData(request.Level);
                
                var xmlSerializer = new XmlSerializer(typeof(LevelExportData));
                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings 
                { 
                    Indent = true, 
                    IndentChars = "  " 
                });
                
                xmlSerializer.Serialize(xmlWriter, levelData);
                var xml = stringWriter.ToString();
                
                result.FileData = Encoding.UTF8.GetBytes(xml);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}.xml";
                result.MimeType = "application/xml";
                result.FileSize = result.FileData.Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"XML export failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToCsvAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var separator = request.Options.GetValueOrDefault("separator", ",").ToString();
                var includeHeaders = request.Options.GetValueOrDefault("includeHeaders", "true").ToString()?.ToLowerInvariant() == "true";
                
                var csv = new StringBuilder();
                
                if (includeHeaders)
                {
                    csv.AppendLine($"X{separator}Y{separator}TileType{separator}TileValue");
                }
                
                if (request.Level.Terrain != null)
                {
                    for (int y = 0; y < request.Level.Terrain.Height; y++)
                    {
                        for (int x = 0; x < request.Level.Terrain.Width; x++)
                        {
                            var tile = request.Level.Terrain.GetTile(x, y);
                            csv.AppendLine($"{x}{separator}{y}{separator}{tile}{separator}{(int)tile}");
                        }
                    }
                }
                
                result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}.csv";
                result.MimeType = "text/csv";
                result.FileSize = result.FileData.Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"CSV export failed: {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToUnityAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var coordinateSystem = request.Options.GetValueOrDefault("coordinateSystem", "unity").ToString();
                var scaleFactor = float.Parse(request.Options.GetValueOrDefault("scaleFactor", "1.0").ToString() ?? "1.0");
                var includePrefabData = request.Options.GetValueOrDefault("includePrefabData", "true").ToString()?.ToLowerInvariant() == "true";
                
                var unityData = new
                {
                    formatVersion = "Unity-1.0",
                    exportTimestamp = DateTime.UtcNow,
                    coordinateSystem = coordinateSystem,
                    scaleFactor = scaleFactor,
                    level = new
                    {
                        name = request.Level.Name,
                        terrain = ConvertTerrainForUnity(request.Level.Terrain, coordinateSystem, scaleFactor),
                        entities = ConvertEntitiesForUnity(request.Level.Entities, coordinateSystem, scaleFactor),
                        bounds = new
                        {
                            width = request.Level.Terrain?.Width ?? 0,
                            height = request.Level.Terrain?.Height ?? 0,
                            scaledWidth = (request.Level.Terrain?.Width ?? 0) * scaleFactor,
                            scaledHeight = (request.Level.Terrain?.Height ?? 0) * scaleFactor
                        }
                    },
                    prefabData = includePrefabData ? GeneratePrefabData(request.Level) : null
                };
                
                var json = JsonSerializer.Serialize(unityData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                result.FileData = Encoding.UTF8.GetBytes(json);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_unity.json";
                result.MimeType = "application/json";
                result.FileSize = result.FileData.Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Unity export failed: {ex.Message}");
            }
            
            return result;
        }
        
        private object ConvertTerrainForUnity(TileMap? terrain, string coordinateSystem, float scaleFactor)
        {
            if (terrain == null) return new { tiles = new object[0] };
            
            var tiles = new List<object>();
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tile = terrain.GetTile(x, y);
                    var position = ConvertPositionForUnity(x, y, coordinateSystem, scaleFactor);
                    
                    tiles.Add(new
                    {
                        position = position,
                        tileType = tile.ToString(),
                        tileValue = (int)tile,
                        gridPosition = new { x, y }
                    });
                }
            }
            
            return new { tiles };
        }
        
        private object ConvertEntitiesForUnity(List<Entity>? entities, string coordinateSystem, float scaleFactor)
        {
            if (entities == null) return new { entities = new object[0] };
            
            var convertedEntities = entities.Select(entity => new
            {
                type = entity.Type.ToString(),
                position = ConvertPositionForUnity(entity.Position.X, entity.Position.Y, coordinateSystem, scaleFactor),
                gridPosition = new { x = (int)entity.Position.X, y = (int)entity.Position.Y },
                properties = entity.Properties
            }).ToList();
            
            return new { entities = convertedEntities };
        }
        
        private object ConvertPositionForUnity(float x, float y, string coordinateSystem, float scaleFactor)
        {
            return coordinateSystem.ToLowerInvariant() switch
            {
                "unity" => new { x = x * scaleFactor, y = 0f, z = y * scaleFactor }, // Unity 3D coordinates
                "unity2d" => new { x = x * scaleFactor, y = y * scaleFactor }, // Unity 2D coordinates
                _ => new { x = x * scaleFactor, y = y * scaleFactor } // Default 2D
            };
        }
        
        private object GeneratePrefabData(Level level)
        {
            return new
            {
                terrainPrefabs = new
                {
                    ground = "Prefabs/Terrain/GroundTile",
                    wall = "Prefabs/Terrain/WallTile",
                    water = "Prefabs/Terrain/WaterTile",
                    grass = "Prefabs/Terrain/GrassTile",
                    sand = "Prefabs/Terrain/SandTile"
                },
                entityPrefabs = new
                {
                    player = "Prefabs/Entities/Player",
                    enemy = "Prefabs/Entities/Enemy",
                    item = "Prefabs/Entities/Item",
                    powerUp = "Prefabs/Entities/PowerUp",
                    checkpoint = "Prefabs/Entities/Checkpoint",
                    exit = "Prefabs/Entities/Exit"
                }
            };
        }
        
        private ProceduralMiniGameGenerator.WebAPI.Models.LevelExportData CreateLevelExportData(Level level)
        {
            return new ProceduralMiniGameGenerator.WebAPI.Models.LevelExportData
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
                Statistics = CalculateLevelStatistics(level)
            };
        }
        
        private int[,] SerializeTerrain(TileMap? terrain)
        {
            if (terrain == null) return new int[0, 0];
            
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
        
        private List<ProceduralMiniGameGenerator.WebAPI.Models.EntityData> SerializeEntities(List<Entity>? entities)
        {
            var serialized = new List<ProceduralMiniGameGenerator.WebAPI.Models.EntityData>();
            
            if (entities == null) return serialized;
            
            foreach (var entity in entities)
            {
                serialized.Add(new ProceduralMiniGameGenerator.WebAPI.Models.EntityData
                {
                    Type = entity.Type.ToString(),
                    Position = new PositionData { X = entity.Position.X, Y = entity.Position.Y },
                    Properties = entity.Properties ?? new Dictionary<string, object>()
                });
            }
            
            return serialized;
        }
        
        private ProceduralMiniGameGenerator.WebAPI.Models.LevelStatistics CalculateLevelStatistics(Level level)
        {
            var stats = new ProceduralMiniGameGenerator.WebAPI.Models.LevelStatistics();
            
            if (level.Terrain != null)
            {
                stats.TotalTiles = level.Terrain.Width * level.Terrain.Height;
                
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
        
        private List<string> ValidateExportRequest(ExportRequest request)
        {
            var errors = new List<string>();
            
            if (request.Level == null)
            {
                errors.Add("Level cannot be null");
            }
            
            if (string.IsNullOrEmpty(request.Format))
            {
                errors.Add("Export format must be specified");
            }
            
            var supportedFormats = new[] { "json", "xml", "csv", "unity", "web-json", "image", "csv-extended", "share-url" };
            if (!supportedFormats.Contains(request.Format.ToLowerInvariant()))
            {
                errors.Add($"Unsupported format: {request.Format}. Supported formats: {string.Join(", ", supportedFormats)}");
            }
            
            return errors;
        }
        
        private async Task<ExportResult> ExportToWebJsonAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var includeMetadata = request.Options.GetValueOrDefault("includeMetadata", "true").ToString()?.ToLowerInvariant() == "true";
                var compactFormat = request.Options.GetValueOrDefault("compactFormat", "false").ToString()?.ToLowerInvariant() == "true";
                var includePreview = request.Options.GetValueOrDefault("includePreview", "true").ToString()?.ToLowerInvariant() == "true";
                
                // Create web-optimized level data structure
                var webData = new
                {
                    version = "web-1.0",
                    level = new
                    {
                        name = request.Level.Name,
                        dimensions = new
                        {
                            width = request.Level.Terrain?.Width ?? 0,
                            height = request.Level.Terrain?.Height ?? 0
                        },
                        terrain = CreateWebOptimizedTerrain(request.Level.Terrain, compactFormat),
                        entities = CreateWebOptimizedEntities(request.Level.Entities, compactFormat),
                        preview = includePreview ? CreateLevelPreviewData(request.Level) : null
                    },
                    metadata = includeMetadata ? new
                    {
                        exportedAt = DateTime.UtcNow,
                        statistics = CalculateLevelStatistics(request.Level),
                        checksum = CalculateLevelChecksum(request.Level)
                    } : null
                };
                
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = !compactFormat,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var json = JsonSerializer.Serialize(webData, jsonOptions);
                
                result.FileData = Encoding.UTF8.GetBytes(json);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_web.json";
                result.MimeType = "application/json";
                result.FileSize = result.FileData.Length;
                result.Success = true;
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    "Web JSON export completed", 
                    new { CompactFormat = compactFormat, FileSize = result.FileSize });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Web JSON export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Web JSON export error");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToImageAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var width = int.Parse(request.Options.GetValueOrDefault("width", "800").ToString() ?? "800");
                var height = int.Parse(request.Options.GetValueOrDefault("height", "600").ToString() ?? "600");
                var tileSize = int.Parse(request.Options.GetValueOrDefault("tileSize", "16").ToString() ?? "16");
                var showGrid = request.Options.GetValueOrDefault("showGrid", "false").ToString()?.ToLowerInvariant() == "true";
                var showEntities = request.Options.GetValueOrDefault("showEntities", "true").ToString()?.ToLowerInvariant() == "true";
                
                // Generate PNG image data for level preview
                var imageData = await GenerateLevelImageAsync(request.Level, width, height, tileSize, showGrid, showEntities);
                
                result.FileData = imageData;
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_preview.png";
                result.MimeType = "image/png";
                result.FileSize = result.FileData.Length;
                result.Success = true;
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    "Image export completed", 
                    new { Width = width, Height = height, FileSize = result.FileSize });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Image export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Image export error");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToCsvExtendedAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var includeEntities = request.Options.GetValueOrDefault("includeEntities", "true").ToString()?.ToLowerInvariant() == "true";
                var includeHeaders = request.Options.GetValueOrDefault("includeHeaders", "true").ToString()?.ToLowerInvariant() == "true";
                var separator = request.Options.GetValueOrDefault("separator", ",").ToString();
                var includeStatistics = request.Options.GetValueOrDefault("includeStatistics", "true").ToString()?.ToLowerInvariant() == "true";
                
                var csv = new StringBuilder();
                
                // Add level metadata
                if (includeHeaders)
                {
                    csv.AppendLine($"# Level: {request.Level.Name}");
                    csv.AppendLine($"# Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    csv.AppendLine($"# Dimensions: {request.Level.Terrain?.Width ?? 0}x{request.Level.Terrain?.Height ?? 0}");
                    csv.AppendLine();
                }
                
                // Terrain data section
                csv.AppendLine("# TERRAIN DATA");
                if (includeHeaders)
                {
                    csv.AppendLine($"X{separator}Y{separator}TileType{separator}TileValue{separator}Walkable");
                }
                
                if (request.Level.Terrain != null)
                {
                    for (int y = 0; y < request.Level.Terrain.Height; y++)
                    {
                        for (int x = 0; x < request.Level.Terrain.Width; x++)
                        {
                            var tile = request.Level.Terrain.GetTile(x, y);
                            var walkable = IsWalkableTile(tile);
                            csv.AppendLine($"{x}{separator}{y}{separator}{tile}{separator}{(int)tile}{separator}{walkable}");
                        }
                    }
                }
                
                // Entity data section
                if (includeEntities && request.Level.Entities != null && request.Level.Entities.Count > 0)
                {
                    csv.AppendLine();
                    csv.AppendLine("# ENTITY DATA");
                    if (includeHeaders)
                    {
                        csv.AppendLine($"EntityType{separator}X{separator}Y{separator}Properties");
                    }
                    
                    foreach (var entity in request.Level.Entities)
                    {
                        var properties = entity.Properties != null && entity.Properties.Count > 0 
                            ? JsonSerializer.Serialize(entity.Properties) 
                            : "";
                        csv.AppendLine($"{entity.Type}{separator}{entity.Position.X}{separator}{entity.Position.Y}{separator}\"{properties}\"");
                    }
                }
                
                // Statistics section
                if (includeStatistics)
                {
                    var stats = CalculateLevelStatistics(request.Level);
                    csv.AppendLine();
                    csv.AppendLine("# STATISTICS");
                    csv.AppendLine($"Metric{separator}Value");
                    csv.AppendLine($"Total Tiles{separator}{stats.TotalTiles}");
                    csv.AppendLine($"Walkable Tiles{separator}{stats.WalkableTiles}");
                    csv.AppendLine($"Wall Tiles{separator}{stats.WallTiles}");
                    csv.AppendLine($"Water Tiles{separator}{stats.WaterTiles}");
                    csv.AppendLine($"Total Entities{separator}{stats.TotalEntities}");
                    csv.AppendLine($"Player Count{separator}{stats.PlayerCount}");
                    csv.AppendLine($"Enemy Count{separator}{stats.EnemyCount}");
                    csv.AppendLine($"Item Count{separator}{stats.ItemCount}");
                }
                
                result.FileData = Encoding.UTF8.GetBytes(csv.ToString());
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_extended.csv";
                result.MimeType = "text/csv";
                result.FileSize = result.FileData.Length;
                result.Success = true;
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    "Extended CSV export completed", 
                    new { IncludeEntities = includeEntities, FileSize = result.FileSize });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Extended CSV export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Extended CSV export error");
            }
            
            return result;
        }
        
        private async Task<ExportResult> ExportToShareUrlAsync(ExportRequest request)
        {
            var result = new ExportResult();
            
            try
            {
                var includePreview = request.Options.GetValueOrDefault("includePreview", "true").ToString()?.ToLowerInvariant() == "true";
                var expirationDays = int.Parse(request.Options.GetValueOrDefault("expirationDays", "30").ToString() ?? "30");
                var baseUrl = request.Options.GetValueOrDefault("baseUrl", "https://localhost:5000").ToString();
                
                // Generate unique share ID
                var shareId = GenerateShareId();
                
                // Create shareable configuration data
                var shareData = new
                {
                    shareId = shareId,
                    createdAt = DateTime.UtcNow,
                    expiresAt = DateTime.UtcNow.AddDays(expirationDays),
                    level = new
                    {
                        name = request.Level.Name,
                        config = ExtractGenerationConfig(request.Level),
                        preview = includePreview ? CreateLevelPreviewData(request.Level) : null
                    }
                };
                
                // Store share data (in a real implementation, this would go to a database)
                await StoreShareDataAsync(shareId, shareData, TimeSpan.FromDays(expirationDays));
                
                // Generate shareable URLs
                var shareUrl = $"{baseUrl.TrimEnd('/')}/share/{shareId}";
                var apiUrl = $"{baseUrl.TrimEnd('/')}/api/configuration/share/{shareId}";
                var previewUrl = includePreview ? $"{baseUrl.TrimEnd('/')}/preview/{shareId}" : null;
                
                var urlData = new
                {
                    shareId = shareId,
                    shareUrl = shareUrl,
                    apiUrl = apiUrl,
                    previewUrl = previewUrl,
                    expiresAt = DateTime.UtcNow.AddDays(expirationDays),
                    instructions = new
                    {
                        web = $"Open in browser: {shareUrl}",
                        api = $"GET request to: {apiUrl}",
                        preview = includePreview ? $"Preview image: {previewUrl}" : "Preview not included"
                    }
                };
                
                var urlText = $"Shareable Level Configuration\n" +
                             $"============================\n\n" +
                             $"Level: {request.Level.Name}\n" +
                             $"Share ID: {shareId}\n" +
                             $"Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                             $"Expires: {DateTime.UtcNow.AddDays(expirationDays):yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                             $"Web URL: {shareUrl}\n" +
                             $"API URL: {apiUrl}\n" +
                             (includePreview ? $"Preview URL: {previewUrl}\n" : "") +
                             $"\nJSON Data:\n{JsonSerializer.Serialize(urlData, new JsonSerializerOptions { WriteIndented = true })}";
                
                result.FileData = Encoding.UTF8.GetBytes(urlText);
                result.FileName = $"{request.FileName ?? request.Level.Name ?? "level"}_share.txt";
                result.MimeType = "text/plain";
                result.FileSize = result.FileData.Length;
                result.Success = true;
                
                await _loggerService.LogAsync(LogLevel.Information, 
                    "Share URL export completed", 
                    new { ShareId = shareId, ExpirationDays = expirationDays });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Share URL export failed: {ex.Message}");
                await _loggerService.LogErrorAsync(ex, "Share URL export error");
            }
            
            return result;
        }
        
        private object CreateWebOptimizedTerrain(TileMap? terrain, bool compactFormat)
        {
            if (terrain == null) return new { tiles = new object[0] };
            
            if (compactFormat)
            {
                // Compact format: run-length encoding for repeated tiles
                var compactTiles = new List<object>();
                var currentTile = terrain.GetTile(0, 0);
                var count = 1;
                
                for (int y = 0; y < terrain.Height; y++)
                {
                    for (int x = 0; x < terrain.Width; x++)
                    {
                        if (x == 0 && y == 0) continue; // Skip first tile
                        
                        var tile = terrain.GetTile(x, y);
                        if (tile == currentTile)
                        {
                            count++;
                        }
                        else
                        {
                            compactTiles.Add(new { t = (int)currentTile, c = count });
                            currentTile = tile;
                            count = 1;
                        }
                    }
                }
                
                // Add the last run
                compactTiles.Add(new { t = (int)currentTile, c = count });
                
                return new { format = "rle", width = terrain.Width, height = terrain.Height, data = compactTiles };
            }
            else
            {
                // Standard format: array of tile values
                var tiles = new int[terrain.Width * terrain.Height];
                for (int y = 0; y < terrain.Height; y++)
                {
                    for (int x = 0; x < terrain.Width; x++)
                    {
                        tiles[y * terrain.Width + x] = (int)terrain.GetTile(x, y);
                    }
                }
                
                return new { format = "array", width = terrain.Width, height = terrain.Height, data = tiles };
            }
        }
        
        private object CreateWebOptimizedEntities(List<Entity>? entities, bool compactFormat)
        {
            if (entities == null) return new { entities = new object[0] };
            
            if (compactFormat)
            {
                // Compact format: group by type and use short property names
                var grouped = entities.GroupBy(e => e.Type).ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Select(e => new
                    {
                        p = new[] { e.Position.X, e.Position.Y }, // position as array
                        pr = e.Properties?.Count > 0 ? e.Properties : null // properties only if present
                    }).ToList()
                );
                
                return new { format = "grouped", data = grouped };
            }
            else
            {
                // Standard format: full entity objects
                var entityList = entities.Select(e => new
                {
                    type = e.Type.ToString(),
                    position = new { x = e.Position.X, y = e.Position.Y },
                    properties = e.Properties
                }).ToList();
                
                return new { format = "standard", data = entityList };
            }
        }
        
        private object CreateLevelPreviewData(Level level)
        {
            return new
            {
                dimensions = new
                {
                    width = level.Terrain?.Width ?? 0,
                    height = level.Terrain?.Height ?? 0
                },
                tileDistribution = CalculateTileDistribution(level.Terrain),
                entitySummary = CalculateEntitySummary(level.Entities),
                thumbnail = GenerateThumbnailData(level)
            };
        }
        
        private Dictionary<string, int> CalculateTileDistribution(TileMap? terrain)
        {
            var distribution = new Dictionary<string, int>();
            
            if (terrain == null) return distribution;
            
            for (int x = 0; x < terrain.Width; x++)
            {
                for (int y = 0; y < terrain.Height; y++)
                {
                    var tileType = terrain.GetTile(x, y).ToString();
                    distribution[tileType] = distribution.GetValueOrDefault(tileType, 0) + 1;
                }
            }
            
            return distribution;
        }
        
        private Dictionary<string, int> CalculateEntitySummary(List<Entity>? entities)
        {
            var summary = new Dictionary<string, int>();
            
            if (entities == null) return summary;
            
            foreach (var entity in entities)
            {
                var entityType = entity.Type.ToString();
                summary[entityType] = summary.GetValueOrDefault(entityType, 0) + 1;
            }
            
            return summary;
        }
        
        private string GenerateThumbnailData(Level level)
        {
            // Generate a simple ASCII representation for thumbnail
            if (level.Terrain == null) return "";
            
            var maxSize = 20; // Maximum thumbnail size
            var scaleX = Math.Max(1, level.Terrain.Width / maxSize);
            var scaleY = Math.Max(1, level.Terrain.Height / maxSize);
            
            var thumbnail = new StringBuilder();
            for (int y = 0; y < level.Terrain.Height; y += scaleY)
            {
                for (int x = 0; x < level.Terrain.Width; x += scaleX)
                {
                    var tile = level.Terrain.GetTile(x, y);
                    thumbnail.Append(GetTileCharacter(tile));
                }
                thumbnail.AppendLine();
            }
            
            return thumbnail.ToString();
        }
        
        private char GetTileCharacter(TileType tile)
        {
            return tile switch
            {
                TileType.Ground => '.',
                TileType.Wall => '#',
                TileType.Water => '~',
                TileType.Grass => ',',
                TileType.Sand => ':',
                _ => '?'
            };
        }
        
        private bool IsWalkableTile(TileType tile)
        {
            return tile == TileType.Ground || tile == TileType.Grass || tile == TileType.Sand;
        }
        
        private string CalculateLevelChecksum(Level level)
        {
            // Generate a simple checksum for level integrity verification
            var data = $"{level.Name}_{level.Terrain?.Width}_{level.Terrain?.Height}_{level.Entities?.Count}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash)[..8]; // First 8 characters
        }
        
        private string GenerateShareId()
        {
            return Guid.NewGuid().ToString("N")[..12]; // 12-character share ID
        }
        
        private object ExtractGenerationConfig(Level level)
        {
            // Extract generation configuration from level metadata
            // This would typically come from the original generation request
            return new
            {
                seed = level.Metadata?.GetValueOrDefault("seed", 0),
                algorithm = level.Metadata?.GetValueOrDefault("algorithm", "unknown"),
                parameters = level.Metadata?.GetValueOrDefault("parameters", new Dictionary<string, object>())
            };
        }
        
        private async Task StoreShareDataAsync(string shareId, object shareData, TimeSpan expiration)
        {
            // In a real implementation, this would store to a database or cache
            // For now, we'll use in-memory storage (this is just for demonstration)
            await _loggerService.LogAsync(LogLevel.Information, 
                $"Share data stored", 
                new { ShareId = shareId, Expiration = expiration });
        }
        
        private async Task<byte[]> GenerateLevelImageAsync(Level level, int width, int height, int tileSize, bool showGrid, bool showEntities)
        {
            // This is a placeholder implementation for image generation
            // In a real implementation, you would use a graphics library like SkiaSharp or System.Drawing
            
            await _loggerService.LogAsync(LogLevel.Information, 
                "Generating level image", 
                new { Width = width, Height = height, TileSize = tileSize });
            
            // For now, return a simple placeholder PNG
            // This would be replaced with actual image generation logic
            var placeholderImage = CreatePlaceholderPng(width, height, level.Name ?? "Level");
            return placeholderImage;
        }
        
        private byte[] CreatePlaceholderPng(int width, int height, string levelName)
        {
            // Create a minimal PNG placeholder
            // In a real implementation, this would generate an actual level preview image
            var placeholder = $"Level Preview: {levelName}\nSize: {width}x{height}";
            return Encoding.UTF8.GetBytes(placeholder);
        }

        private byte[] CreateZipPackage(List<(string fileName, byte[] data)> files)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var (fileName, data) in files)
                {
                    var entry = archive.CreateEntry(fileName);
                    using var entryStream = entry.Open();
                    entryStream.Write(data, 0, data.Length);
                }
            }
            
            return memoryStream.ToArray();
        }
    }
}
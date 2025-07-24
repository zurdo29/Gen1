using ProceduralMiniGameGenerator.WebAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Comprehensive server-side validation service
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly ILoggerService _logger;
        private readonly IConfiguration _configuration;
        
        // Validation constants
        private static readonly string[] VALID_ALGORITHMS = { "perlin", "cellular", "maze", "rooms" };
        private static readonly string[] VALID_TERRAIN_TYPES = { "ground", "wall", "water", "grass", "stone", "sand", "lava", "ice" };
        private static readonly string[] VALID_ENTITY_TYPES = { "Player", "Enemy", "Item", "PowerUp", "NPC", "Exit", "Checkpoint", "Obstacle", "Trigger" };
        private static readonly string[] VALID_PLACEMENT_STRATEGIES = { "random", "clustered", "spread", "near_walls", "center", "far_from_player", "corners" };
        private static readonly string[] VALID_DIFFICULTY_LEVELS = { "easy", "normal", "hard", "extreme" };
        private static readonly string[] VALID_VICTORY_CONDITIONS = { "reach_exit", "collect_all_items", "defeat_all_enemies", "survive_time", "reach_score" };
        
        public ValidationService(ILoggerService logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        public async Task<ValidationResult> ValidateGenerationConfigAsync(object config, ValidationOptions? options = null)
        {
            options ??= new ValidationOptions();
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug, 
                    "Starting generation config validation", new { Config = config, Options = options });
                
                // Convert to JSON for easier property access
                var jsonConfig = JsonSerializer.Serialize(config);
                var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfig);
                
                if (configDict == null)
                {
                    errors.Add(new ValidationError 
                    { 
                        Field = "root", 
                        Message = "Invalid configuration format", 
                        Code = "INVALID_FORMAT" 
                    });
                    return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
                }
                
                // Basic field validation
                await ValidateBasicFields(configDict, errors, warnings);
                
                // Cross-field validation
                if (options.CrossFieldValidation)
                {
                    await ValidateCrossFieldsInternal(configDict, errors, warnings);
                }
                
                // Performance validation
                if (options.PerformanceCheck)
                {
                    await ValidatePerformanceConstraintsInternal(configDict, errors, warnings, options);
                }
                
                // Business rule validation
                if (options.BusinessRuleCheck)
                {
                    await ValidateBusinessRulesInternal(configDict, errors, warnings);
                }
                
                // Convert warnings to errors if strict mode
                if (options.Strict && warnings.Any())
                {
                    foreach (var warning in warnings)
                    {
                        errors.Add(new ValidationError
                        {
                            Field = warning.Field,
                            Message = warning.Message,
                            Code = "STRICT_" + (warning.Code ?? "WARNING")
                        });
                    }
                    if (options.Strict) warnings.Clear();
                }
                
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                    "Completed generation config validation", 
                    new { ErrorCount = errors.Count, WarningCount = warnings.Count });
                
                return new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors,
                    Warnings = options.IncludeWarnings ? warnings : new List<ValidationWarning>()
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Error during generation config validation", new { Config = config });
                
                errors.Add(new ValidationError
                {
                    Field = "root",
                    Message = "Validation process failed",
                    Code = "VALIDATION_ERROR"
                });
                
                return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
            }
        }
        
        public async Task<ValidationResult> ValidateEntityConfigAsync(object entity, ValidationContext? context = null)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            try
            {
                var jsonEntity = JsonSerializer.Serialize(entity);
                var entityDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonEntity);
                
                if (entityDict == null)
                {
                    errors.Add(new ValidationError 
                    { 
                        Field = "entity", 
                        Message = "Invalid entity format", 
                        Code = "INVALID_FORMAT" 
                    });
                    return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
                }
                
                // Validate entity type
                if (entityDict.TryGetValue("type", out var typeObj) && typeObj != null)
                {
                    var type = typeObj.ToString();
                    if (!VALID_ENTITY_TYPES.Contains(type))
                    {
                        errors.Add(new ValidationError
                        {
                            Field = "type",
                            Message = $"Invalid entity type '{type}'. Must be one of: {string.Join(", ", VALID_ENTITY_TYPES)}",
                            Code = "INVALID_ENTITY_TYPE"
                        });
                    }
                }
                else
                {
                    errors.Add(new ValidationError
                    {
                        Field = "type",
                        Message = "Entity type is required",
                        Code = "MISSING_ENTITY_TYPE"
                    });
                }
                
                // Validate count
                if (entityDict.TryGetValue("count", out var countObj) && countObj != null)
                {
                    if (int.TryParse(countObj.ToString(), out var count))
                    {
                        if (count < 0)
                        {
                            errors.Add(new ValidationError
                            {
                                Field = "count",
                                Message = "Entity count cannot be negative",
                                Code = "NEGATIVE_COUNT"
                            });
                        }
                        else if (count > 1000)
                        {
                            errors.Add(new ValidationError
                            {
                                Field = "count",
                                Message = "Entity count cannot exceed 1000",
                                Code = "COUNT_TOO_HIGH"
                            });
                        }
                        else if (count > 100)
                        {
                            warnings.Add(new ValidationWarning
                            {
                                Field = "count",
                                Message = "High entity count may affect performance",
                                Suggestion = "Consider reducing entity count for better performance"
                            });
                        }
                    }
                    else
                    {
                        errors.Add(new ValidationError
                        {
                            Field = "count",
                            Message = "Entity count must be a valid number",
                            Code = "INVALID_COUNT_FORMAT"
                        });
                    }
                }
                
                // Validate placement strategy
                if (entityDict.TryGetValue("placementStrategy", out var strategyObj) && strategyObj != null)
                {
                    var strategy = strategyObj.ToString();
                    if (!VALID_PLACEMENT_STRATEGIES.Contains(strategy))
                    {
                        errors.Add(new ValidationError
                        {
                            Field = "placementStrategy",
                            Message = $"Invalid placement strategy '{strategy}'. Must be one of: {string.Join(", ", VALID_PLACEMENT_STRATEGIES)}",
                            Code = "INVALID_PLACEMENT_STRATEGY"
                        });
                    }
                }
                
                // Validate distance constraints
                var minDistance = GetNumericValue(entityDict, "minDistance");
                var maxDistance = GetNumericValue(entityDict, "maxDistanceFromPlayer");
                
                if (minDistance.HasValue && maxDistance.HasValue && minDistance > maxDistance)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "minDistance",
                        Message = "Minimum distance cannot be greater than maximum distance from player",
                        Code = "INVALID_DISTANCE_CONSTRAINT"
                    });
                }
                
                return new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Error during entity validation", new { Entity = entity, Context = context });
                
                errors.Add(new ValidationError
                {
                    Field = "entity",
                    Message = "Entity validation failed",
                    Code = "VALIDATION_ERROR"
                });
                
                return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
            }
        }
        
        public async Task<ValidationResult> ValidateExportRequestAsync(ExportRequest request)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            try
            {
                if (request == null)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "request",
                        Message = "Export request is required",
                        Code = "MISSING_REQUEST"
                    });
                    return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
                }
                
                // Validate format
                if (string.IsNullOrWhiteSpace(request.Format))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "format",
                        Message = "Export format is required",
                        Code = "MISSING_FORMAT"
                    });
                }
                else
                {
                    var validFormats = new[] { "json", "xml", "csv", "unity" };
                    if (!validFormats.Contains(request.Format.ToLowerInvariant()))
                    {
                        errors.Add(new ValidationError
                        {
                            Field = "format",
                            Message = $"Invalid export format '{request.Format}'. Must be one of: {string.Join(", ", validFormats)}",
                            Code = "INVALID_FORMAT"
                        });
                    }
                }
                
                // Validate level
                if (request.Level == null)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "level",
                        Message = "Level data is required for export",
                        Code = "MISSING_LEVEL"
                    });
                }
                else
                {
                    // Check level size for export limits
                    var levelSize = EstimateLevelSize(request.Level);
                    if (levelSize > 50 * 1024 * 1024) // 50MB limit
                    {
                        errors.Add(new ValidationError
                        {
                            Field = "level",
                            Message = "Level is too large to export",
                            Code = "LEVEL_TOO_LARGE"
                        });
                    }
                    else if (levelSize > 10 * 1024 * 1024) // 10MB warning
                    {
                        warnings.Add(new ValidationWarning
                        {
                            Field = "level",
                            Message = "Large level may take longer to export",
                            Suggestion = "Consider exporting in a compressed format"
                        });
                    }
                }
                
                return new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Error during export request validation", new { Request = request });
                
                errors.Add(new ValidationError
                {
                    Field = "request",
                    Message = "Export request validation failed",
                    Code = "VALIDATION_ERROR"
                });
                
                return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
            }
        }
        
        public async Task<ValidationResult> ValidateBatchGenerationRequestAsync(BatchGenerationRequest request)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            try
            {
                if (request == null)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "request",
                        Message = "Batch generation request is required",
                        Code = "MISSING_REQUEST"
                    });
                    return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
                }
                
                // Validate count
                if (request.Count <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "count",
                        Message = "Batch count must be greater than 0",
                        Code = "INVALID_COUNT"
                    });
                }
                else if (request.Count > 50)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "count",
                        Message = "Batch count cannot exceed 50",
                        Code = "COUNT_TOO_HIGH"
                    });
                }
                else if (request.Count > 10)
                {
                    warnings.Add(new ValidationWarning
                    {
                        Field = "count",
                        Message = "Large batch size may take longer to process",
                        Suggestion = "Consider processing in smaller batches"
                    });
                }
                
                // Validate base configuration
                if (request.BaseConfig != null)
                {
                    var configValidation = await ValidateGenerationConfigAsync(request.BaseConfig);
                    errors.AddRange(configValidation.Errors);
                    warnings.AddRange(configValidation.Warnings);
                }
                
                return new ValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Error during batch generation request validation", new { Request = request });
                
                errors.Add(new ValidationError
                {
                    Field = "request",
                    Message = "Batch generation request validation failed",
                    Code = "VALIDATION_ERROR"
                });
                
                return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
            }
        }
        
        public async Task<ValidationResult> ValidateCrossFieldsAsync(object config)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            var jsonConfig = JsonSerializer.Serialize(config);
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfig);
            
            if (configDict != null)
            {
                await ValidateCrossFieldsInternal(configDict, errors, warnings);
            }
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }
        
        public async Task<ValidationResult> ValidatePerformanceConstraintsAsync(object config)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            var jsonConfig = JsonSerializer.Serialize(config);
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfig);
            
            if (configDict != null)
            {
                await ValidatePerformanceConstraintsInternal(configDict, errors, warnings, new ValidationOptions());
            }
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }
        
        public async Task<ValidationResult> ValidateBusinessRulesAsync(object config)
        {
            var errors = new List<ValidationError>();
            var warnings = new List<ValidationWarning>();
            
            var jsonConfig = JsonSerializer.Serialize(config);
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfig);
            
            if (configDict != null)
            {
                await ValidateBusinessRulesInternal(configDict, errors, warnings);
            }
            
            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }
        
        private async Task ValidateBasicFields(Dictionary<string, object> config, List<ValidationError> errors, List<ValidationWarning> warnings)
        {
            // Validate width
            var width = GetNumericValue(config, "width");
            if (!width.HasValue)
            {
                errors.Add(new ValidationError { Field = "width", Message = "Width is required", Code = "MISSING_WIDTH" });
            }
            else if (width < 10)
            {
                errors.Add(new ValidationError { Field = "width", Message = "Width must be at least 10", Code = "WIDTH_TOO_SMALL" });
            }
            else if (width > 1000)
            {
                errors.Add(new ValidationError { Field = "width", Message = "Width cannot exceed 1000", Code = "WIDTH_TOO_LARGE" });
            }
            
            // Validate height
            var height = GetNumericValue(config, "height");
            if (!height.HasValue)
            {
                errors.Add(new ValidationError { Field = "height", Message = "Height is required", Code = "MISSING_HEIGHT" });
            }
            else if (height < 10)
            {
                errors.Add(new ValidationError { Field = "height", Message = "Height must be at least 10", Code = "HEIGHT_TOO_SMALL" });
            }
            else if (height > 1000)
            {
                errors.Add(new ValidationError { Field = "height", Message = "Height cannot exceed 1000", Code = "HEIGHT_TOO_LARGE" });
            }
            
            // Validate generation algorithm
            if (config.TryGetValue("generationAlgorithm", out var algorithmObj) && algorithmObj != null)
            {
                var algorithm = algorithmObj.ToString();
                if (!VALID_ALGORITHMS.Contains(algorithm))
                {
                    errors.Add(new ValidationError
                    {
                        Field = "generationAlgorithm",
                        Message = $"Invalid generation algorithm '{algorithm}'. Must be one of: {string.Join(", ", VALID_ALGORITHMS)}",
                        Code = "INVALID_ALGORITHM"
                    });
                }
            }
            else
            {
                errors.Add(new ValidationError { Field = "generationAlgorithm", Message = "Generation algorithm is required", Code = "MISSING_ALGORITHM" });
            }
            
            await Task.CompletedTask; // Make method async
        }
        
        private async Task ValidateCrossFieldsInternal(Dictionary<string, object> config, List<ValidationError> errors, List<ValidationWarning> warnings)
        {
            var width = GetNumericValue(config, "width");
            var height = GetNumericValue(config, "height");
            
            // Level size vs entity count validation
            if (width.HasValue && height.HasValue && config.TryGetValue("entities", out var entitiesObj))
            {
                var levelArea = width.Value * height.Value;
                var entities = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(entitiesObj.ToString() ?? "[]");
                
                if (entities != null)
                {
                    var totalEntities = entities.Sum(e => GetNumericValue(e, "count") ?? 0);
                    var entityDensity = (double)totalEntities / levelArea;
                    
                    if (entityDensity > 0.5)
                    {
                        errors.Add(new ValidationError
                        {
                            Field = "entities",
                            Message = "Too many entities for the level size. This may cause placement conflicts.",
                            Code = "ENTITY_DENSITY_TOO_HIGH"
                        });
                    }
                    else if (entityDensity > 0.3)
                    {
                        warnings.Add(new ValidationWarning
                        {
                            Field = "entities",
                            Message = "High entity density may affect performance.",
                            Suggestion = "Consider reducing entity count or increasing level size."
                        });
                    }
                    
                    // Check for required entities
                    var playerEntities = entities.Where(e => GetStringValue(e, "type") == "Player").ToList();
                    if (playerEntities.Count == 0)
                    {
                        errors.Add(new ValidationError
                        {
                            Field = "entities",
                            Message = "At least one Player entity is required.",
                            Code = "MISSING_PLAYER_ENTITY"
                        });
                    }
                    else if (playerEntities.Count > 1)
                    {
                        warnings.Add(new ValidationWarning
                        {
                            Field = "entities",
                            Message = "Multiple Player entities detected.",
                            Suggestion = "Consider using only one Player entity for clarity."
                        });
                    }
                }
            }
            
            await Task.CompletedTask; // Make method async
        }
        
        private async Task ValidatePerformanceConstraintsInternal(Dictionary<string, object> config, List<ValidationError> errors, List<ValidationWarning> warnings, ValidationOptions options)
        {
            var width = GetNumericValue(config, "width");
            var height = GetNumericValue(config, "height");
            
            // Level size performance check
            if (width.HasValue && height.HasValue)
            {
                var levelArea = width.Value * height.Value;
                
                if (levelArea > options.MaxLevelArea)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "width,height",
                        Message = "Level size too large. May cause performance issues or generation timeouts.",
                        Code = "LEVEL_SIZE_TOO_LARGE"
                    });
                }
                else if (levelArea > options.MaxLevelArea / 2)
                {
                    warnings.Add(new ValidationWarning
                    {
                        Field = "width,height",
                        Message = "Large level size may affect performance.",
                        Suggestion = "Consider reducing dimensions for better performance."
                    });
                }
            }
            
            await Task.CompletedTask; // Make method async
        }
        
        private async Task ValidateBusinessRulesInternal(Dictionary<string, object> config, List<ValidationError> errors, List<ValidationWarning> warnings)
        {
            // Validate terrain types
            if (config.TryGetValue("terrainTypes", out var terrainTypesObj))
            {
                var terrainTypes = JsonSerializer.Deserialize<List<string>>(terrainTypesObj.ToString() ?? "[]");
                if (terrainTypes == null || terrainTypes.Count == 0)
                {
                    errors.Add(new ValidationError
                    {
                        Field = "terrainTypes",
                        Message = "At least one terrain type is required.",
                        Code = "NO_TERRAIN_TYPES"
                    });
                }
                else
                {
                    foreach (var terrainType in terrainTypes)
                    {
                        if (!VALID_TERRAIN_TYPES.Contains(terrainType))
                        {
                            errors.Add(new ValidationError
                            {
                                Field = "terrainTypes",
                                Message = $"Invalid terrain type '{terrainType}'. Must be one of: {string.Join(", ", VALID_TERRAIN_TYPES)}",
                                Code = "INVALID_TERRAIN_TYPE"
                            });
                        }
                    }
                }
            }
            
            await Task.CompletedTask; // Make method async
        }
        
        private static double? GetNumericValue(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (double.TryParse(value.ToString(), out var numericValue))
                {
                    return numericValue;
                }
            }
            return null;
        }
        
        private static string? GetStringValue(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                return value.ToString();
            }
            return null;
        }
        
        private static long EstimateLevelSize(object level)
        {
            // Simple estimation based on JSON serialization
            var json = JsonSerializer.Serialize(level);
            return System.Text.Encoding.UTF8.GetByteCount(json);
        }
    }
}
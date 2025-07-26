using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for comprehensive server-side validation
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Validates a generation configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <param name="options">Validation options</param>
        /// <returns>Validation result with errors and warnings</returns>
        Task<WebApiModels.ValidationResult> ValidateGenerationConfigAsync(object config, ValidationOptions? options = null);
        
        /// <summary>
        /// Validates an entity configuration
        /// </summary>
        /// <param name="entity">Entity configuration to validate</param>
        /// <param name="context">Validation context</param>
        /// <returns>Validation result</returns>
        Task<WebApiModels.ValidationResult> ValidateEntityConfigAsync(object entity, ValidationContext? context = null);
        
        /// <summary>
        /// Validates export request
        /// </summary>
        /// <param name="request">Export request to validate</param>
        /// <returns>Validation result</returns>
        Task<WebApiModels.ValidationResult> ValidateExportRequestAsync(WebApiModels.ExportRequest request);
        
        /// <summary>
        /// Validates batch generation request
        /// </summary>
        /// <param name="request">Batch generation request to validate</param>
        /// <returns>Validation result</returns>
        Task<WebApiModels.ValidationResult> ValidateBatchGenerationRequestAsync(WebApiModels.BatchGenerationRequest request);
        
        /// <summary>
        /// Performs cross-field validation
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Cross-field validation result</returns>
        Task<WebApiModels.ValidationResult> ValidateCrossFieldsAsync(object config);
        
        /// <summary>
        /// Validates performance constraints
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Performance validation result</returns>
        Task<WebApiModels.ValidationResult> ValidatePerformanceConstraintsAsync(object config);
        
        /// <summary>
        /// Validates business rules
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Business rule validation result</returns>
        Task<WebApiModels.ValidationResult> ValidateBusinessRulesAsync(object config);
    }
    
    /// <summary>
    /// Validation options for controlling validation behavior
    /// </summary>
    public class ValidationOptions
    {
        /// <summary>
        /// Whether to perform strict validation (treat warnings as errors)
        /// </summary>
        public bool Strict { get; set; } = false;
        
        /// <summary>
        /// Whether to include warnings in the result
        /// </summary>
        public bool IncludeWarnings { get; set; } = true;
        
        /// <summary>
        /// Whether to perform cross-field validation
        /// </summary>
        public bool CrossFieldValidation { get; set; } = true;
        
        /// <summary>
        /// Whether to perform performance validation
        /// </summary>
        public bool PerformanceCheck { get; set; } = true;
        
        /// <summary>
        /// Whether to perform business rule validation
        /// </summary>
        public bool BusinessRuleCheck { get; set; } = true;
        
        /// <summary>
        /// Maximum allowed level area (width * height)
        /// </summary>
        public int MaxLevelArea { get; set; } = 100000;
        
        /// <summary>
        /// Maximum allowed entity count
        /// </summary>
        public int MaxEntityCount { get; set; } = 1000;
        
        /// <summary>
        /// Maximum allowed generation time in seconds
        /// </summary>
        public int MaxGenerationTimeSeconds { get; set; } = 30;
    }
    
    /// <summary>
    /// Context for validation operations
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// The operation being performed
        /// </summary>
        public string Operation { get; set; } = string.Empty;
        
        /// <summary>
        /// User identifier for context
        /// </summary>
        public string? UserId { get; set; }
        
        /// <summary>
        /// Session identifier for context
        /// </summary>
        public string? SessionId { get; set; }
        
        /// <summary>
        /// Additional context data
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new();
        
        /// <summary>
        /// Parent configuration for nested validation
        /// </summary>
        public object? ParentConfig { get; set; }
        
        /// <summary>
        /// Field path for nested validation
        /// </summary>
        public string? FieldPath { get; set; }
    }
}
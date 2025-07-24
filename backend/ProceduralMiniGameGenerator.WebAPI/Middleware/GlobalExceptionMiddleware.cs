using ProceduralMiniGameGenerator.WebAPI.Services;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ProceduralMiniGameGenerator.WebAPI.Middleware
{
    /// <summary>
    /// Global exception handling middleware that converts exceptions to user-friendly error responses
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerService _loggerService;
        private readonly IWebHostEnvironment _environment;
        
        public GlobalExceptionMiddleware(
            RequestDelegate next, 
            ILoggerService loggerService,
            IWebHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
        
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
            
            // Log the exception with context
            await _loggerService.LogErrorAsync(exception, "Unhandled exception in request pipeline", new
            {
                RequestId = requestId,
                Path = context.Request.Path,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
            });
            
            var errorResponse = CreateErrorResponse(exception, requestId);
            
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = errorResponse.StatusCode;
            
            var jsonResponse = JsonSerializer.Serialize(errorResponse.Body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            
            await context.Response.WriteAsync(jsonResponse);
        }
        
        private ErrorResponse CreateErrorResponse(Exception exception, string requestId)
        {
            return exception switch
            {
                ValidationException validationEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = new ErrorResponseBody
                    {
                        Code = "VALIDATION_ERROR",
                        Title = "Validation Error",
                        Message = "One or more validation errors occurred.",
                        Details = validationEx.Message,
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow,
                        ValidationErrors = validationEx.ValidationErrors?.Select(e => new ValidationErrorDetail
                        {
                            Field = e.Field,
                            Message = e.Message,
                            Code = e.Code
                        }).ToList()
                    }
                },
                
                ConfigurationException configEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = new ErrorResponseBody
                    {
                        Code = "INVALID_CONFIGURATION",
                        Title = "Configuration Error",
                        Message = "The provided configuration is invalid.",
                        Details = configEx.Message,
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow,
                        TroubleshootingUrl = "/help/troubleshooting#configuration-errors"
                    }
                },
                
                GenerationException genEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.UnprocessableEntity,
                    Body = new ErrorResponseBody
                    {
                        Code = DetermineGenerationErrorCode(genEx),
                        Title = "Generation Error",
                        Message = "Failed to generate level with the provided configuration.",
                        Details = genEx.Message,
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow,
                        TroubleshootingUrl = "/help/troubleshooting#generation-errors"
                    }
                },
                
                ExportException exportEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.UnprocessableEntity,
                    Body = new ErrorResponseBody
                    {
                        Code = DetermineExportErrorCode(exportEx),
                        Title = "Export Error",
                        Message = "Failed to export level in the requested format.",
                        Details = exportEx.Message,
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow,
                        TroubleshootingUrl = "/help/troubleshooting#export-errors"
                    }
                },
                
                TimeoutException timeoutEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.RequestTimeout,
                    Body = new ErrorResponseBody
                    {
                        Code = "OPERATION_TIMEOUT",
                        Title = "Operation Timeout",
                        Message = "The operation took too long to complete.",
                        Details = timeoutEx.Message,
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow,
                        TroubleshootingUrl = "/help/troubleshooting#timeout-errors"
                    }
                },
                
                UnauthorizedAccessException unauthorizedEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.Forbidden,
                    Body = new ErrorResponseBody
                    {
                        Code = "ACCESS_DENIED",
                        Title = "Access Denied",
                        Message = "You do not have permission to perform this operation.",
                        Details = unauthorizedEx.Message,
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow
                    }
                },
                
                ArgumentException argEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = new ErrorResponseBody
                    {
                        Code = "INVALID_ARGUMENT",
                        Title = "Invalid Argument",
                        Message = "One or more arguments are invalid.",
                        Details = argEx.Message,
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow
                    }
                },
                
                NotSupportedException notSupportedEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.NotImplemented,
                    Body = new ErrorResponseBody
                    {
                        Code = "NOT_SUPPORTED",
                        Title = "Operation Not Supported",
                        Message = "The requested operation is not supported.",
                        Details = notSupportedEx.Message,
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow
                    }
                },
                
                OutOfMemoryException memoryEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.UnprocessableEntity,
                    Body = new ErrorResponseBody
                    {
                        Code = "MEMORY_LIMIT_EXCEEDED",
                        Title = "Memory Limit Exceeded",
                        Message = "The operation requires too much memory to complete.",
                        Details = "Try reducing the level size or complexity.",
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow,
                        TroubleshootingUrl = "/help/troubleshooting#memory-errors"
                    }
                },
                
                _ => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = new ErrorResponseBody
                    {
                        Code = "INTERNAL_SERVER_ERROR",
                        Title = "Internal Server Error",
                        Message = "An unexpected error occurred on the server.",
                        Details = _environment.IsDevelopment() ? exception.Message : "Please try again later or contact support.",
                        RequestId = requestId,
                        Timestamp = DateTime.UtcNow,
                        TroubleshootingUrl = "/help/troubleshooting#server-errors",
                        StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
                    }
                }
            };
        }
        
        private string DetermineGenerationErrorCode(GenerationException exception)
        {
            var message = exception.Message.ToLowerInvariant();
            
            if (message.Contains("timeout") || message.Contains("time limit"))
                return "GENERATION_TIMEOUT";
            if (message.Contains("memory") || message.Contains("out of memory"))
                return "MEMORY_LIMIT_EXCEEDED";
            if (message.Contains("invalid") || message.Contains("configuration"))
                return "INVALID_CONFIGURATION";
            if (message.Contains("size") || message.Contains("too large"))
                return "LEVEL_TOO_LARGE";
            
            return "GENERATION_FAILED";
        }
        
        private string DetermineExportErrorCode(ExportException exception)
        {
            var message = exception.Message.ToLowerInvariant();
            
            if (message.Contains("format") || message.Contains("unsupported"))
                return "UNSUPPORTED_FORMAT";
            if (message.Contains("size") || message.Contains("too large"))
                return "EXPORT_SIZE_LIMIT";
            if (message.Contains("timeout"))
                return "EXPORT_TIMEOUT";
            
            return "EXPORT_FAILED";
        }
    }
    
    /// <summary>
    /// Error response structure
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public ErrorResponseBody Body { get; set; } = new();
    }
    
    /// <summary>
    /// Error response body structure
    /// </summary>
    public class ErrorResponseBody
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? TroubleshootingUrl { get; set; }
        public string? StackTrace { get; set; }
        public List<ValidationErrorDetail>? ValidationErrors { get; set; }
    }
    
    /// <summary>
    /// Validation error detail
    /// </summary>
    public class ValidationErrorDetail
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Custom exception types for better error handling
    /// </summary>
    public class ValidationException : Exception
    {
        public List<ValidationError>? ValidationErrors { get; }
        
        public ValidationException(string message) : base(message) { }
        
        public ValidationException(string message, List<ValidationError> validationErrors) : base(message)
        {
            ValidationErrors = validationErrors;
        }
    }
    
    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
    
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class GenerationException : Exception
    {
        public GenerationException(string message) : base(message) { }
        public GenerationException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class ExportException : Exception
    {
        public ExportException(string message) : base(message) { }
        public ExportException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    /// <summary>
    /// Extension methods for registering the global exception middleware
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        /// <summary>
        /// Adds the global exception handling middleware to the application pipeline
        /// </summary>
        /// <param name="builder">Application builder</param>
        /// <returns>Application builder for chaining</returns>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
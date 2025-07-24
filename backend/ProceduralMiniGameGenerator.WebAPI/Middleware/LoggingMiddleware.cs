using ProceduralMiniGameGenerator.WebAPI.Services;
using System.Diagnostics;

namespace ProceduralMiniGameGenerator.WebAPI.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses with performance tracking
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerService _loggerService;
        
        public LoggingMiddleware(RequestDelegate next, ILoggerService loggerService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            
            // Add request ID to context for correlation
            context.Items["RequestId"] = requestId;
            
            // Log request start
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information, 
                "Request started", 
                new { 
                    RequestId = requestId, 
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    QueryString = context.Request.QueryString.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
                });
            
            try
            {
                await _next(context);
                
                stopwatch.Stop();
                
                // Log successful request completion
                await _loggerService.LogRequestAsync(
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Log request failure
                await _loggerService.LogErrorAsync(ex, "Request failed", 
                    new { 
                        RequestId = requestId, 
                        Duration = stopwatch.Elapsed,
                        Path = context.Request.Path,
                        Method = context.Request.Method
                    });
                
                // Set error status code if not already set
                if (context.Response.StatusCode == 200)
                {
                    context.Response.StatusCode = 500;
                }
                
                // Log the failed request
                await _loggerService.LogRequestAsync(
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.Elapsed);
                
                throw;
            }
        }
    }
    
    /// <summary>
    /// Extension methods for registering the logging middleware
    /// </summary>
    public static class LoggingMiddlewareExtensions
    {
        /// <summary>
        /// Adds the logging middleware to the application pipeline
        /// </summary>
        /// <param name="builder">Application builder</param>
        /// <returns>Application builder for chaining</returns>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoggingMiddleware>();
        }
    }
}
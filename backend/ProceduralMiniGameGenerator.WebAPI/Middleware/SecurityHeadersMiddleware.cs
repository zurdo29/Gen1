namespace ProceduralMiniGameGenerator.WebAPI.Middleware;

/// <summary>
/// Middleware for adding security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    
    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        AddSecurityHeaders(context);
        
        await _next(context);
    }
    
    private static void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        
        // Prevent clickjacking
        headers.Add("X-Frame-Options", "DENY");
        
        // Prevent MIME type sniffing
        headers.Add("X-Content-Type-Options", "nosniff");
        
        // Enable XSS protection
        headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Referrer policy
        headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Content Security Policy
        var csp = "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data: https:; " +
                  "font-src 'self' data:; " +
                  "connect-src 'self' ws: wss:; " +
                  "frame-ancestors 'none'; " +
                  "base-uri 'self'; " +
                  "form-action 'self'";
        
        headers.Add("Content-Security-Policy", csp);
        
        // Strict Transport Security (HSTS) - only add if HTTPS
        if (context.Request.IsHttps)
        {
            headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }
        
        // Permissions Policy (formerly Feature Policy)
        var permissionsPolicy = "camera=(), " +
                               "microphone=(), " +
                               "geolocation=(), " +
                               "payment=(), " +
                               "usb=(), " +
                               "magnetometer=(), " +
                               "gyroscope=(), " +
                               "accelerometer=()";
        
        headers.Add("Permissions-Policy", permissionsPolicy);
        
        // Remove server information
        headers.Remove("Server");
        headers.Add("Server", "WebAPI");
        
        // Add custom security header for API identification
        headers.Add("X-API-Version", "1.0");
        headers.Add("X-Security-Policy", "strict");
    }
}
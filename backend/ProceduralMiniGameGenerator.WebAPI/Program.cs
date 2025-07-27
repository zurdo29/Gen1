using Hangfire;
using Hangfire.MemoryStorage;
using Serilog;
using FluentValidation;
using ProceduralMiniGameGenerator.WebAPI.Middleware;
using ProceduralMiniGameGenerator.WebAPI.Services;
using ProceduralMiniGameGenerator.WebAPI.Configuration;
using ProceduralMiniGameGenerator.WebAPI.Validators;
using ProceduralMiniGameGenerator.WebAPI.HealthChecks;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using System.Text.Json.Serialization;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/webapi-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for existing models
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Procedural Mini Game Generator API",
        Version = "v1",
        Description = "Web API for procedural level generation and configuration management",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Procedural Mini Game Generator",
            Url = new Uri("https://github.com/your-repo/procedural-mini-game-generator")
        }
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // More permissive in development
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // More restrictive in production with enhanced security
            policy.WithOrigins(corsOrigins)
                  .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-API-Key")
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight for 10 minutes
        }
    });
});

// Add Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());

builder.Services.AddHangfireServer();

// Configure strongly-typed configuration
builder.Services.Configure<ApiConfiguration>(
    builder.Configuration.GetSection(ApiConfiguration.SectionName));
builder.Services.Configure<GenerationConfiguration>(
    builder.Configuration.GetSection(GenerationConfiguration.SectionName));

// Add memory caching with size limit
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = builder.Configuration.GetValue<int>("Api:MaxCacheSizeMB", 100) * 1024 * 1024; // Convert MB to bytes
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<GenerationConfigValidator>();

// Add custom logging service
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.ILoggerService, ProceduralMiniGameGenerator.WebAPI.Services.LoggerService>();

// Add plugin loader service
builder.Services.AddSingleton<ProceduralMiniGameGenerator.WebAPI.Services.IPluginLoader, ProceduralMiniGameGenerator.WebAPI.Services.PluginLoader>();

// Add QR code service
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IQRCodeService, ProceduralMiniGameGenerator.WebAPI.Services.QRCodeService>();

// Add social preview service
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.ISocialPreviewService, ProceduralMiniGameGenerator.WebAPI.Services.SocialPreviewService>();

// Add configuration management service
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IConfigurationService, ProceduralMiniGameGenerator.WebAPI.Services.ConfigurationService>();

// Add generation services (new architecture)
builder.Services.AddScoped<ProceduralMiniGameGenerator.Configuration.IConfigurationParser, ProceduralMiniGameGenerator.WebAPI.Services.SimpleConfigurationParser>();
builder.Services.AddScoped<ProceduralMiniGameGenerator.Generators.IGenerationManager, ProceduralMiniGameGenerator.WebAPI.Services.SimpleGenerationManager>();

// Add new focused services
builder.Services.AddScoped<ILevelGenerationService, LevelGenerationService>();
builder.Services.AddScoped<IBatchGenerationService, BatchGenerationService>();
builder.Services.AddScoped<IJobStatusService, JobStatusService>();

// Add legacy services for backward compatibility
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IGenerationService, ProceduralMiniGameGenerator.WebAPI.Services.GenerationService>();

// Add core export service
builder.Services.AddScoped<ProceduralMiniGameGenerator.Core.ILevelExportService, ProceduralMiniGameGenerator.Core.LevelExportService>();

// Add web export service
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IExportService, ProceduralMiniGameGenerator.WebAPI.Services.ExportService>();

// Add real-time generation service
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IRealTimeGenerationService, ProceduralMiniGameGenerator.WebAPI.Services.RealTimeGenerationService>();

// Add validation service
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IValidationService, ProceduralMiniGameGenerator.WebAPI.Services.ValidationService>();

// Add security service
builder.Services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.ISecurityService, ProceduralMiniGameGenerator.WebAPI.Services.SecurityService>();

// Add response caching
builder.Services.AddResponseCaching();

// Add data protection for sensitive data
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("./keys"))
    .SetApplicationName("ProceduralMiniGameGenerator");

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<GenerationServiceHealthCheck>("generation-service")
    .AddCheck<CacheHealthCheck>("memory-cache");

// Add SignalR for real-time updates
builder.Services.AddSignalR();

var app = builder.Build();

// Log startup
Log.Information("Starting Procedural Mini Game Generator Web API...");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Procedural Mini Game Generator API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

// Add security headers (must be early in pipeline)
app.UseSecurityHeaders();

// Add rate limiting (before other middleware)
app.UseRateLimiting();

// Add response caching
app.UseResponseCaching();

app.UseCors("AllowFrontend");

// Add global exception handling
app.UseGlobalExceptionHandling();

// Add request logging middleware
app.UseRequestLogging();

app.UseAuthorization();

app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");

// Map SignalR hubs
app.MapHub<ProceduralMiniGameGenerator.WebAPI.Hubs.GenerationHub>("/hubs/generation");

// Add Hangfire dashboard (only in development for security)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.Run();
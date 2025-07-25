using Hangfire;
using Hangfire.MemoryStorage;
using FluentValidation;
using ProceduralMiniGameGenerator.WebAPI.Services;
using ProceduralMiniGameGenerator.WebAPI.Configuration;
using ProceduralMiniGameGenerator.WebAPI.Validators;
using ProceduralMiniGameGenerator.WebAPI.HealthChecks;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using Asp.Versioning;

namespace ProceduralMiniGameGenerator.WebAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGenerationServices(this IServiceCollection services)
        {
            // Core generation services
            services.AddScoped<ProceduralMiniGameGenerator.Configuration.IConfigurationParser, ProceduralMiniGameGenerator.WebAPI.Services.SimpleConfigurationParser>();
            services.AddScoped<ProceduralMiniGameGenerator.Generators.IGenerationManager, ProceduralMiniGameGenerator.WebAPI.Services.SimpleGenerationManager>();
            
            // New focused services
            services.AddScoped<ILevelGenerationService, LevelGenerationService>();
            services.AddScoped<IBatchGenerationService, BatchGenerationService>();
            services.AddScoped<IJobStatusService, JobStatusService>();
            services.AddScoped<IConfigurationCloningService, ConfigurationCloningService>();
            services.AddScoped<IVariationApplicationService, VariationApplicationService>();
            services.AddScoped<IConfigurationCombinationService, ConfigurationCombinationService>();
            
            // Legacy services for backward compatibility
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IGenerationService, ProceduralMiniGameGenerator.WebAPI.Services.GenerationService>();
            
            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.ILoggerService, ProceduralMiniGameGenerator.WebAPI.Services.LoggerService>();
            services.AddSingleton<ProceduralMiniGameGenerator.WebAPI.Services.IPluginLoader, ProceduralMiniGameGenerator.WebAPI.Services.PluginLoader>();
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IQRCodeService, ProceduralMiniGameGenerator.WebAPI.Services.QRCodeService>();
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.ISocialPreviewService, ProceduralMiniGameGenerator.WebAPI.Services.SocialPreviewService>();
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IConfigurationService, ProceduralMiniGameGenerator.WebAPI.Services.ConfigurationService>();
            services.AddScoped<ProceduralMiniGameGenerator.Core.ILevelExportService, ProceduralMiniGameGenerator.Core.LevelExportService>();
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IExportService, ProceduralMiniGameGenerator.WebAPI.Services.ExportService>();
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IRealTimeGenerationService, ProceduralMiniGameGenerator.WebAPI.Services.RealTimeGenerationService>();
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.IValidationService, ProceduralMiniGameGenerator.WebAPI.Services.ValidationService>();
            services.AddScoped<ProceduralMiniGameGenerator.WebAPI.Services.ISecurityService, ProceduralMiniGameGenerator.WebAPI.Services.SecurityService>();
            
            return services;
        }

        public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
        {
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage());

            services.AddHangfireServer();
            return services;
        }

        public static IServiceCollection AddApiVersioningWithSwagger(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
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

            return services;
        }

        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<GenerationServiceHealthCheck>("generation-service")
                .AddCheck<CacheHealthCheck>("memory-cache");

            return services;
        }
    }
}
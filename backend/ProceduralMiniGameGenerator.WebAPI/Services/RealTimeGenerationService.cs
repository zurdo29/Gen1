using Microsoft.AspNetCore.SignalR;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Hubs;
using ProceduralMiniGameGenerator.WebAPI.Models;
using System.Collections.Concurrent;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for handling real-time generation with debouncing
    /// </summary>
    public class RealTimeGenerationService : IRealTimeGenerationService
    {
        private readonly IHubContext<GenerationHub, IGenerationHubClient> _hubContext;
        private readonly IGenerationService _generationService;
        private readonly ILoggerService _loggerService;
        
        // Track pending operations and their cancellation tokens
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingOperations = new();
        private readonly ConcurrentDictionary<string, PreviewStatus> _sessionStatus = new();

        public RealTimeGenerationService(
            IHubContext<GenerationHub, IGenerationHubClient> hubContext,
            IGenerationService generationService,
            ILoggerService loggerService)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Request a debounced preview generation
        /// </summary>
        public async Task RequestDebouncedPreview(string sessionId, GenerationConfig config, int debounceMs = 500)
        {
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                "Debounced preview requested",
                new { SessionId = sessionId, DebounceMs = debounceMs });

            // Cancel any existing operation for this session
            await CancelPendingPreview(sessionId);

            // Update status to pending
            var status = GetOrCreateStatus(sessionId);
            status.Status = "pending";
            status.LastConfig = config;
            status.LastUpdated = DateTime.UtcNow;
            status.Message = "Preview generation pending...";

            // Create new cancellation token
            var cancellationTokenSource = new CancellationTokenSource();
            _pendingOperations[sessionId] = cancellationTokenSource;

            try
            {
                // Send immediate feedback to client
                await _hubContext.Clients.Group($"session_{sessionId}")
                    .GenerationProgress(sessionId, 0, "Preview generation pending...");

                // Wait for debounce period
                await Task.Delay(debounceMs, cancellationTokenSource.Token);

                // Check if operation was cancelled during debounce
                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                // Start actual generation
                await GeneratePreview(sessionId, config, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, this is expected
                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                    "Preview generation cancelled",
                    new { SessionId = sessionId });
            }
            catch (Exception ex)
            {
                await _loggerService.LogErrorAsync(ex, "Error during debounced preview generation", 
                    new { SessionId = sessionId });

                status.Status = "error";
                status.ErrorMessage = ex.Message;
                status.LastUpdated = DateTime.UtcNow;

                await _hubContext.Clients.Group($"session_{sessionId}")
                    .GenerationError(sessionId, ex.Message);
            }
            finally
            {
                // Clean up
                _pendingOperations.TryRemove(sessionId, out _);
            }
        }

        /// <summary>
        /// Cancel any pending preview for a session
        /// </summary>
        public async Task CancelPendingPreview(string sessionId)
        {
            if (_pendingOperations.TryRemove(sessionId, out var existingOperation))
            {
                existingOperation.Cancel();
                existingOperation.Dispose();

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                    "Cancelled pending preview operation",
                    new { SessionId = sessionId });
            }
        }

        /// <summary>
        /// Get the current status of a session's preview generation
        /// </summary>
        public async Task<PreviewStatus> GetPreviewStatus(string sessionId)
        {
            await Task.CompletedTask; // Make async for consistency
            return _sessionStatus.GetValueOrDefault(sessionId, new PreviewStatus 
            { 
                SessionId = sessionId, 
                Status = "idle" 
            });
        }

        /// <summary>
        /// Generate preview with progress updates
        /// </summary>
        private async Task GeneratePreview(string sessionId, GenerationConfig config, CancellationToken cancellationToken)
        {
            var status = GetOrCreateStatus(sessionId);
            status.Status = "generating";
            status.Progress = 0;
            status.Message = "Starting generation...";
            status.LastUpdated = DateTime.UtcNow;

            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                "Starting real-time preview generation",
                new { SessionId = sessionId, ConfigSize = $"{config.Width}x{config.Height}" });

            try
            {
                // Validate configuration first
                await UpdateProgress(sessionId, 10, "Validating configuration...");
                var validationResult = _generationService.ValidateConfiguration(config);
                
                if (!validationResult.IsValid)
                {
                    await _hubContext.Clients.Group($"session_{sessionId}")
                        .ValidationResult(sessionId, validationResult);
                    
                    status.Status = "error";
                    status.ErrorMessage = string.Join(", ", validationResult.Errors);
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Create generation request
                var request = new WebGenerationRequest
                {
                    Config = config,
                    SessionId = sessionId,
                    IncludePreview = true,
                    UseBackgroundProcessing = false // Force synchronous for real-time preview
                };

                await UpdateProgress(sessionId, 20, "Initializing generation...");
                cancellationToken.ThrowIfCancellationRequested();

                // Generate level with progress updates
                var level = await GenerateWithProgressUpdates(request, sessionId, cancellationToken);

                await UpdateProgress(sessionId, 100, "Generation completed!");

                // Update status
                status.Status = "completed";
                status.Progress = 100;
                status.LastResult = level;
                status.LastUpdated = DateTime.UtcNow;
                status.Message = "Preview generated successfully";

                // Send result to client
                await _hubContext.Clients.Group($"session_{sessionId}")
                    .PreviewGenerated(sessionId, level);

                await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Real-time preview generation completed",
                    new { 
                        SessionId = sessionId, 
                        LevelName = level.Name,
                        EntityCount = level.Entities.Count
                    });
            }
            catch (OperationCanceledException)
            {
                status.Status = "cancelled";
                status.Message = "Generation cancelled";
                throw;
            }
            catch (Exception ex)
            {
                status.Status = "error";
                status.ErrorMessage = ex.Message;
                status.LastUpdated = DateTime.UtcNow;
                throw;
            }
        }

        /// <summary>
        /// Generate level with progress updates sent to client
        /// </summary>
        private async Task<Level> GenerateWithProgressUpdates(WebGenerationRequest request, string sessionId, CancellationToken cancellationToken)
        {
            // Simulate generation steps with progress updates
            await UpdateProgress(sessionId, 30, "Generating terrain...");
            cancellationToken.ThrowIfCancellationRequested();
            
            // Small delay to simulate work
            await Task.Delay(100, cancellationToken);

            await UpdateProgress(sessionId, 50, "Placing entities...");
            cancellationToken.ThrowIfCancellationRequested();
            
            await Task.Delay(100, cancellationToken);

            await UpdateProgress(sessionId, 70, "Applying visual themes...");
            cancellationToken.ThrowIfCancellationRequested();
            
            await Task.Delay(100, cancellationToken);

            await UpdateProgress(sessionId, 90, "Finalizing level...");
            cancellationToken.ThrowIfCancellationRequested();

            // Generate the actual level
            var level = await _generationService.GenerateLevelAsync(request);
            
            return level;
        }

        /// <summary>
        /// Send progress update to client
        /// </summary>
        private async Task UpdateProgress(string sessionId, int progress, string message)
        {
            var status = GetOrCreateStatus(sessionId);
            status.Progress = progress;
            status.Message = message;
            status.LastUpdated = DateTime.UtcNow;

            await _hubContext.Clients.Group($"session_{sessionId}")
                .GenerationProgress(sessionId, progress, message);
        }

        /// <summary>
        /// Get or create status for a session
        /// </summary>
        private PreviewStatus GetOrCreateStatus(string sessionId)
        {
            return _sessionStatus.GetOrAdd(sessionId, id => new PreviewStatus 
            { 
                SessionId = id, 
                Status = "idle" 
            });
        }
    }
}
using Microsoft.AspNetCore.SignalR;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;
using ProceduralMiniGameGenerator.WebAPI.Services;

namespace ProceduralMiniGameGenerator.WebAPI.Hubs
{
    /// <summary>
    /// SignalR hub for real-time generation updates
    /// </summary>
    public class GenerationHub : Hub
    {
        private readonly ILoggerService _loggerService;

        public GenerationHub(ILoggerService loggerService)
        {
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        /// <summary>
        /// Called when a client connects
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                "Client connected to GenerationHub",
                new { ConnectionId = Context.ConnectionId });

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                "Client disconnected from GenerationHub",
                new { ConnectionId = Context.ConnectionId, Exception = exception?.Message });

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a session group for receiving updates
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
            
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                "Client joined session group",
                new { ConnectionId = Context.ConnectionId, SessionId = sessionId });
        }

        /// <summary>
        /// Leave a session group
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
            
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                "Client left session group",
                new { ConnectionId = Context.ConnectionId, SessionId = sessionId });
        }

        /// <summary>
        /// Request real-time preview for configuration changes
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="config">Generation configuration</param>
        public async Task RequestPreview(string sessionId, GenerationConfig config)
        {
            await _loggerService.LogAsync(Microsoft.Extensions.Logging.LogLevel.Debug,
                "Real-time preview requested",
                new { 
                    ConnectionId = Context.ConnectionId, 
                    SessionId = sessionId,
                    ConfigSize = $"{config.Width}x{config.Height}"
                });

            // Send acknowledgment that preview request was received
            await Clients.Caller.SendAsync("PreviewRequested", sessionId);
        }
    }

    /// <summary>
    /// Interface for sending real-time updates to clients
    /// </summary>
    public interface IGenerationHubClient
    {
        /// <summary>
        /// Send generation progress update
        /// </summary>
        Task GenerationProgress(string sessionId, int progress, string message);

        /// <summary>
        /// Send completed level preview
        /// </summary>
        Task PreviewGenerated(string sessionId, Level level);

        /// <summary>
        /// Send generation error
        /// </summary>
        Task GenerationError(string sessionId, string error);

        /// <summary>
        /// Acknowledge preview request received
        /// </summary>
        Task PreviewRequested(string sessionId);

        /// <summary>
        /// Send validation result
        /// </summary>
        Task ValidationResult(string sessionId, ValidationResult result);
    }
}
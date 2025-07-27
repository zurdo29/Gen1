using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for managing background job operations
    /// </summary>
    public interface IJobManagementService
    {
        string StartBackgroundJob<T>(string jobType, T request, string? sessionId = null);
        WebApiModels.JobStatus GetJobStatus(string jobId);
        bool CancelJob(string jobId);
        Task UpdateJobProgress(string jobId, int progress);
        Task CompleteJob(string jobId, object? result = null);
        Task FailJob(string jobId, string errorMessage);
    }
}
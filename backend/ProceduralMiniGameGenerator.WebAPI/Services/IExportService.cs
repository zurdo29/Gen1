using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for exporting levels in various formats
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Gets all available export formats
        /// </summary>
        /// <returns>List of available export formats</returns>
        Task<List<ExportFormat>> GetAvailableFormatsAsync();
        
        /// <summary>
        /// Exports a single level to the specified format
        /// </summary>
        /// <param name="request">Export request with level and format details</param>
        /// <returns>Export result with file data</returns>
        Task<ExportResult> ExportLevelAsync(ExportRequest request);
        
        /// <summary>
        /// Exports multiple levels in batch
        /// </summary>
        /// <param name="request">Batch export request</param>
        /// <returns>Job ID for tracking batch export progress</returns>
        Task<string> ExportBatchAsync(BatchExportRequest request);
        
        /// <summary>
        /// Gets the status of a batch export job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Job status with progress information</returns>
        Task<JobStatus> GetBatchExportStatusAsync(string jobId);
        
        /// <summary>
        /// Downloads the result of a completed batch export
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>File result with batch export data</returns>
        Task<ProceduralMiniGameGenerator.WebAPI.Models.FileResult?> DownloadBatchExportAsync(string jobId);
    }
    
    /// <summary>
    /// Result of an export operation
    /// </summary>
    public class ExportResult
    {
        /// <summary>
        /// Whether the export was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Exported file data
        /// </summary>
        public byte[]? FileData { get; set; }
        
        /// <summary>
        /// Filename for the exported file
        /// </summary>
        public string? FileName { get; set; }
        
        /// <summary>
        /// MIME type of the exported file
        /// </summary>
        public string? MimeType { get; set; }
        
        /// <summary>
        /// Size of the exported file in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Any errors that occurred during export
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
        
        /// <summary>
        /// Any warnings generated during export
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// Time taken for the export operation
        /// </summary>
        public TimeSpan ExportTime { get; set; }
    }
    

}
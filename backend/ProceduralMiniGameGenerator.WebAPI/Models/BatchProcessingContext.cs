using System.Diagnostics;
using WebApiModels = ProceduralMiniGameGenerator.WebAPI.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Models
{
    /// <summary>
    /// Context object for batch processing operations
    /// </summary>
    public class BatchProcessingContext
    {
        public string JobId { get; }
        public WebApiModels.BatchGenerationRequest Request { get; }
        public List<object> Results { get; }
        public Stopwatch Stopwatch { get; }

        public BatchProcessingContext(string jobId, WebApiModels.BatchGenerationRequest request)
        {
            JobId = jobId;
            Request = request;
            Results = new List<object>();
            Stopwatch = Stopwatch.StartNew();
        }

        public int TotalLevels => CalculateTotalBatchLevels();

        private int CalculateTotalBatchLevels()
        {
            if (Request.Variations == null || Request.Variations.Count == 0)
                return Request.Count;

            var totalCombinations = Request.Variations.Aggregate(1, (total, variation) => 
                total * Math.Max(variation.Values?.Count ?? 1, 1));

            return totalCombinations * Request.Count;
        }
    }
}
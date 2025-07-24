using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Implementation of social media preview generation service
    /// </summary>
    public class SocialPreviewService : ISocialPreviewService
    {
        private readonly ILoggerService _logger;

        public SocialPreviewService(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GeneratePreviewImageAsync(GenerationConfig config, int width = 1200, int height = 630)
        {
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generating social media preview image",
                    new { Width = width, Height = height, ConfigSize = $"{config.Width}x{config.Height}" });

                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                
                // Set high quality rendering
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                
                // Background gradient
                using var backgroundBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    Color.FromArgb(45, 55, 72),
                    Color.FromArgb(26, 32, 44),
                    LinearGradientMode.Vertical);
                graphics.FillRectangle(backgroundBrush, 0, 0, width, height);
                
                // Title
                using var titleFont = new Font("Arial", 48, FontStyle.Bold);
                using var titleBrush = new SolidBrush(Color.White);
                var title = "Procedural Level Generator";
                var titleSize = graphics.MeasureString(title, titleFont);
                graphics.DrawString(title, titleFont, titleBrush, 
                    (width - titleSize.Width) / 2, 50);
                
                // Configuration details
                using var detailFont = new Font("Arial", 24, FontStyle.Regular);
                using var detailBrush = new SolidBrush(Color.FromArgb(203, 213, 224));
                
                var details = new[]
                {
                    $"Size: {config.Width} × {config.Height}",
                    $"Algorithm: {config.GenerationAlgorithm}",
                    $"Seed: {config.Seed}"
                };
                
                var yOffset = 150;
                foreach (var detail in details)
                {
                    var detailSize = graphics.MeasureString(detail, detailFont);
                    graphics.DrawString(detail, detailFont, detailBrush,
                        (width - detailSize.Width) / 2, yOffset);
                    yOffset += 40;
                }
                
                // Simple level visualization
                var levelWidth = 400;
                var levelHeight = 300;
                var levelX = (width - levelWidth) / 2;
                var levelY = height - levelHeight - 50;
                
                using var levelBrush = new SolidBrush(Color.FromArgb(68, 90, 120));
                graphics.FillRectangle(levelBrush, levelX, levelY, levelWidth, levelHeight);
                
                using var borderPen = new Pen(Color.FromArgb(160, 174, 192), 2);
                graphics.DrawRectangle(borderPen, levelX, levelY, levelWidth, levelHeight);
                
                // Add some visual elements to represent the level
                var random = new Random(config.Seed);
                using var elementBrush = new SolidBrush(Color.FromArgb(129, 140, 248));
                
                for (int i = 0; i < 20; i++)
                {
                    var x = levelX + random.Next(levelWidth - 20);
                    var y = levelY + random.Next(levelHeight - 20);
                    graphics.FillRectangle(elementBrush, x, y, 15, 15);
                }
                
                // Convert to data URL
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                var bytes = stream.ToArray();
                var base64 = Convert.ToBase64String(bytes);
                var dataUrl = $"data:image/png;base64,{base64}";

                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Social media preview image generated successfully",
                    new { DataUrlLength = dataUrl.Length });

                return dataUrl;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Failed to generate social media preview image",
                    new { Width = width, Height = height, Config = config });
                throw;
            }
        }

        public async Task<string> GenerateThumbnailAsync(GenerationConfig config, int size = 300)
        {
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generating thumbnail preview",
                    new { Size = size, ConfigSize = $"{config.Width}x{config.Height}" });

                using var bitmap = new Bitmap(size, size);
                using var graphics = Graphics.FromImage(bitmap);
                
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Background
                using var backgroundBrush = new SolidBrush(Color.FromArgb(45, 55, 72));
                graphics.FillRectangle(backgroundBrush, 0, 0, size, size);
                
                // Simple grid representation
                var gridSize = size - 40;
                var gridX = 20;
                var gridY = 20;
                
                using var gridBrush = new SolidBrush(Color.FromArgb(68, 90, 120));
                graphics.FillRectangle(gridBrush, gridX, gridY, gridSize, gridSize);
                
                // Add pattern based on algorithm
                var random = new Random(config.Seed);
                using var patternBrush = new SolidBrush(Color.FromArgb(129, 140, 248));
                
                var cellSize = gridSize / 10;
                for (int x = 0; x < 10; x++)
                {
                    for (int y = 0; y < 10; y++)
                    {
                        if (random.NextDouble() > 0.6)
                        {
                            graphics.FillRectangle(patternBrush,
                                gridX + x * cellSize,
                                gridY + y * cellSize,
                                cellSize - 1,
                                cellSize - 1);
                        }
                    }
                }
                
                // Convert to data URL
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                var bytes = stream.ToArray();
                var base64 = Convert.ToBase64String(bytes);
                var dataUrl = $"data:image/png;base64,{base64}";

                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Thumbnail preview generated successfully",
                    new { DataUrlLength = dataUrl.Length });

                return dataUrl;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Failed to generate thumbnail preview",
                    new { Size = size, Config = config });
                throw;
            }
        }
    }
}
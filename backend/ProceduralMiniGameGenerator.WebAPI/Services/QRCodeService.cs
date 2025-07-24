using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Implementation of QR code generation service using QRCoder library
    /// </summary>
    public class QRCodeService : IQRCodeService
    {
        private readonly ILoggerService _logger;

        public QRCodeService(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GenerateQRCodeDataUrlAsync(string text, int size = 200)
        {
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generating QR code data URL",
                    new { TextLength = text.Length, Size = size });

                var qrBytes = await GenerateQRCodeBytesAsync(text, size);
                var base64String = Convert.ToBase64String(qrBytes);
                var dataUrl = $"data:image/png;base64,{base64String}";

                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "QR code data URL generated successfully",
                    new { DataUrlLength = dataUrl.Length });

                return dataUrl;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Failed to generate QR code data URL", 
                    new { Text = text, Size = size });
                throw;
            }
        }

        public async Task<byte[]> GenerateQRCodeBytesAsync(string text, int size = 200)
        {
            try
            {
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "Generating QR code bytes",
                    new { TextLength = text.Length, Size = size });

                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                
                var qrCodeBytes = qrCode.GetGraphic(size / 25); // Scale factor for size
                
                await _logger.LogAsync(Microsoft.Extensions.Logging.LogLevel.Information,
                    "QR code bytes generated successfully",
                    new { BytesLength = qrCodeBytes.Length });

                return qrCodeBytes;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync(ex, "Failed to generate QR code bytes", 
                    new { Text = text, Size = size });
                throw;
            }
        }
    }
}
namespace ProceduralMiniGameGenerator.WebAPI.Services
{
    /// <summary>
    /// Service for generating QR codes
    /// </summary>
    public interface IQRCodeService
    {
        /// <summary>
        /// Generates a QR code as a data URL for the given text
        /// </summary>
        /// <param name="text">Text to encode in QR code</param>
        /// <param name="size">Size of the QR code (default: 200)</param>
        /// <returns>QR code as data URL</returns>
        Task<string> GenerateQRCodeDataUrlAsync(string text, int size = 200);
        
        /// <summary>
        /// Generates a QR code as PNG bytes
        /// </summary>
        /// <param name="text">Text to encode in QR code</param>
        /// <param name="size">Size of the QR code (default: 200)</param>
        /// <returns>QR code as PNG bytes</returns>
        Task<byte[]> GenerateQRCodeBytesAsync(string text, int size = 200);
    }
}
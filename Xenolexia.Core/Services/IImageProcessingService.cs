namespace Xenolexia.Core.Services;

/// <summary>
/// Image processing service interface for cover extraction and manipulation
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Extract cover image from EPUB file
    /// </summary>
    Task<string?> ExtractCoverFromEpubAsync(string epubPath, string outputDirectory);

    /// <summary>
    /// Download and save cover image from URL
    /// </summary>
    Task<string?> DownloadCoverAsync(string coverUrl, string bookId, string outputDirectory);

    /// <summary>
    /// Resize image to specified dimensions
    /// </summary>
    Task<string?> ResizeImageAsync(string imagePath, int maxWidth, int maxHeight, string outputPath);

    /// <summary>
    /// Convert image format
    /// </summary>
    Task<string?> ConvertImageFormatAsync(string imagePath, string outputPath, ImageFormat format);
}

/// <summary>
/// Supported image formats
/// </summary>
public enum ImageFormat
{
    Jpeg,
    Png,
    WebP
}

using System.Net.Http;
using VersOne.Epub;

namespace Xenolexia.Core.Services;

/// <summary>
/// Image processing using VersOne.Epub for EPUB cover extraction and HttpClient for URL covers.
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly HttpClient _httpClient;

    public ImageProcessingService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string?> ExtractCoverFromEpubAsync(string epubPath, string outputDirectory)
    {
        if (!File.Exists(epubPath))
            return null;

        try
        {
            using var bookRef = await EpubReader.OpenBookAsync(epubPath);
            var coverBytes = await bookRef.ReadCoverAsync();
            if (coverBytes == null || coverBytes.Length == 0)
                return null;

            Directory.CreateDirectory(outputDirectory);
            var bookId = Path.GetFileNameWithoutExtension(epubPath);
            var extension = ".jpg";
            var outputPath = Path.Combine(outputDirectory, $"{bookId}_cover{extension}");
            await File.WriteAllBytesAsync(outputPath, coverBytes);
            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting cover: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> DownloadCoverAsync(string coverUrl, string bookId, string outputDirectory)
    {
        if (string.IsNullOrEmpty(coverUrl))
            return null;

        try
        {
            var response = await _httpClient.GetAsync(coverUrl);
            response.EnsureSuccessStatusCode();

            Directory.CreateDirectory(outputDirectory);
            var extension = Path.GetExtension(new Uri(coverUrl).LocalPath).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || extension.Length > 4)
                extension = ".jpg";

            var outputPath = Path.Combine(outputDirectory, $"{bookId}_cover{extension}");

            using var imageStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(outputPath);
            await imageStream.CopyToAsync(fileStream);

            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading cover: {ex.Message}");
            return null;
        }
    }

    public Task<string?> ResizeImageAsync(string imagePath, int maxWidth, int maxHeight, string outputPath)
    {
        if (!File.Exists(imagePath))
            return Task.FromResult<string?>(null);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(imagePath, outputPath, true);
            return Task.FromResult<string?>(outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resizing image: {ex.Message}");
            return Task.FromResult<string?>(null);
        }
    }

    public Task<string?> ConvertImageFormatAsync(string imagePath, string outputPath, ImageFormat format)
    {
        if (!File.Exists(imagePath))
            return Task.FromResult<string?>(null);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(imagePath, outputPath, true);
            return Task.FromResult<string?>(outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting image: {ex.Message}");
            return Task.FromResult<string?>(null);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

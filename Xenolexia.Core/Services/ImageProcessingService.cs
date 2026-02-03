using System.Net.Http;
using EpubCore;

namespace Xenolexia.Core.Services;

/// <summary>
/// Image processing using EpubCore for EPUB cover extraction and HttpClient for URL covers.
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
        if (string.IsNullOrWhiteSpace(epubPath))
            return null;
        if (string.IsNullOrWhiteSpace(outputDirectory))
            return null;
        if (!File.Exists(epubPath))
            return null;
        var ext = Path.GetExtension(epubPath).ToLowerInvariant();
        if (ext != ".epub")
            return null;

        try
        {
            EpubBook book;
            try
            {
                book = await Task.Run(() => EpubReader.Read(epubPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading EPUB for cover: {ex.Message}");
                return null;
            }
            if (book == null)
                return null;

            byte[]? coverBytes = null;
            var extension = ".jpg";
            if (book.CoverImage is byte[] bytes && bytes.Length > 0)
            {
                coverBytes = bytes;
            }
            else if (!string.IsNullOrEmpty(book.CoverImageHref) && book.Resources?.Images != null)
            {
                var href = book.CoverImageHref.Replace('\\', '/').TrimStart('/');
                var img = book.Resources.Images.FirstOrDefault(i => string.Equals((i.Href ?? "").Replace('\\', '/'), href, StringComparison.OrdinalIgnoreCase));
                if (img?.Content != null && img.Content.Length > 0)
                {
                    coverBytes = img.Content;
                    var imgExt = Path.GetExtension(img.Href ?? "").ToLowerInvariant();
                    if (!string.IsNullOrEmpty(imgExt) && imgExt.Length <= 5)
                        extension = imgExt;
                }
            }
            if (coverBytes == null || coverBytes.Length == 0)
                return null;

            Directory.CreateDirectory(outputDirectory);
            var bookId = Path.GetFileNameWithoutExtension(epubPath);
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

using System.IO.Compression;
using System.Net.Http;
using System.Xml.Linq;

namespace Xenolexia.Core.Services;

/// <summary>
/// Image processing service implementation
/// Handles cover extraction from EPUB files and image manipulation
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
            using var zip = ZipFile.OpenRead(epubPath);

            // Read container.xml to find OPF file
            var containerEntry = zip.GetEntry("META-INF/container.xml");
            if (containerEntry == null)
                return null;

            var containerXml = await ReadZipEntryAsync(containerEntry);
            var containerDoc = XDocument.Parse(containerXml);
            var opfPath = containerDoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "rootfile")?
                .Attribute("full-path")?.Value;

            if (string.IsNullOrEmpty(opfPath))
                return null;

            // Read OPF file
            var opfEntry = zip.GetEntry(opfPath);
            if (opfEntry == null)
                return null;

            var opfXml = await ReadZipEntryAsync(opfEntry);
            var opfDoc = XDocument.Parse(opfXml);
            var ns = opfDoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Find cover image reference
            var metadata = opfDoc.Descendants(ns + "metadata").FirstOrDefault();
            var coverMeta = metadata?.Elements()
                .FirstOrDefault(e => e.Attribute("name")?.Value == "cover");

            if (coverMeta == null)
                return null;

            var coverId = coverMeta.Attribute("content")?.Value;
            if (string.IsNullOrEmpty(coverId))
                return null;

            // Find manifest item with cover ID
            var manifest = opfDoc.Descendants(ns + "manifest").FirstOrDefault();
            var coverItem = manifest?.Elements(ns + "item")
                .FirstOrDefault(i => i.Attribute("id")?.Value == coverId);

            if (coverItem == null)
                return null;

            var coverHref = coverItem.Attribute("href")?.Value;
            if (string.IsNullOrEmpty(coverHref))
                return null;

            // Resolve relative path
            var opfDir = Path.GetDirectoryName(opfPath)?.Replace('\\', '/');
            var coverPath = opfDir != null && !string.IsNullOrEmpty(opfDir)
                ? $"{opfDir}/{coverHref}".Replace("//", "/")
                : coverHref;

            // Extract cover image
            var coverEntry = zip.GetEntry(coverPath);
            if (coverEntry == null)
                return null;

            Directory.CreateDirectory(outputDirectory);
            var bookId = Path.GetFileNameWithoutExtension(epubPath);
            var extension = Path.GetExtension(coverHref).ToLowerInvariant();
            var outputPath = Path.Combine(outputDirectory, $"{bookId}_cover{extension}");

            using var coverStream = coverEntry.Open();
            using var fileStream = File.Create(outputPath);
            await coverStream.CopyToAsync(fileStream);

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
            var extension = Path.GetExtension(coverUrl).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !extension.StartsWith("."))
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

    public async Task<string?> ResizeImageAsync(string imagePath, int maxWidth, int maxHeight, string outputPath)
    {
        // TODO: Implement image resizing using ImageSharp or System.Drawing
        // For now, just copy the file
        if (!File.Exists(imagePath))
            return null;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(imagePath, outputPath, true);
            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resizing image: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> ConvertImageFormatAsync(string imagePath, string outputPath, ImageFormat format)
    {
        // TODO: Implement image format conversion using ImageSharp
        // For now, just copy the file
        if (!File.Exists(imagePath))
            return null;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(imagePath, outputPath, true);
            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting image: {ex.Message}");
            return null;
        }
    }

    private async Task<string> ReadZipEntryAsync(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

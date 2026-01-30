using System.Security.Cryptography;
using System.Text;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Imports books from local storage into the app library.
/// Supports EPUB, PDF, TXT, FB2 via FOSS libraries (VersOne.Epub, PdfPig, .NET BCL, Fb2.Document). MOBI omitted (no FOSS full-text library).
/// </summary>
public class BookImportService : IBookImportService
{
    private readonly string _booksDirectory;
    private readonly string _coversDirectory;
    private readonly IStorageService _storageService;
    private readonly IBookParserService _bookParserService;
    private readonly IImageProcessingService _imageProcessingService;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".epub", ".pdf", ".txt", ".fb2"
    };

    public BookImportService(
        string booksDirectory,
        string coversDirectory,
        IStorageService storageService,
        IBookParserService bookParserService,
        IImageProcessingService imageProcessingService)
    {
        _booksDirectory = booksDirectory;
        _coversDirectory = coversDirectory;
        _storageService = storageService;
        _bookParserService = bookParserService;
        _imageProcessingService = imageProcessingService;
        Directory.CreateDirectory(_booksDirectory);
        Directory.CreateDirectory(_coversDirectory);
    }

    public static bool IsSupportedFormat(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(ext) && SupportedExtensions.Contains(ext);
    }

    public static BookFormat GetFormatFromPath(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".epub" => BookFormat.Epub,
            ".fb2" => BookFormat.Fb2,
            ".mobi" or ".azw" or ".azw3" => BookFormat.Mobi,
            ".pdf" => BookFormat.Pdf,
            ".txt" => BookFormat.Txt,
            _ => throw new NotSupportedException($"Unsupported file format: {ext}")
        };
    }

    public async Task<Book> ImportFromFileAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Book file not found.", sourceFilePath);

        if (!IsSupportedFormat(sourceFilePath))
            throw new NotSupportedException($"File format not supported. Supported: {string.Join(", ", SupportedExtensions)}");

        var format = GetFormatFromPath(sourceFilePath);
        var fileInfo = new FileInfo(sourceFilePath);
        var bookId = GenerateBookId(sourceFilePath);

        var destDir = Path.Combine(_booksDirectory, bookId);
        Directory.CreateDirectory(destDir);
        var destFileName = $"book{Path.GetExtension(sourceFilePath)}";
        var destFilePath = Path.Combine(destDir, destFileName);
        await Task.Run(() => File.Copy(sourceFilePath, destFilePath, overwrite: true));

        BookMetadata metadata;
        try
        {
            metadata = await _bookParserService.GetMetadataAsync(destFilePath);
        }
        catch (NotSupportedException)
        {
            metadata = new BookMetadata
            {
                Title = Path.GetFileNameWithoutExtension(sourceFilePath),
                Author = "Unknown"
            };
        }

        string? coverPath = null;
        if (format == BookFormat.Epub)
        {
            coverPath = await _imageProcessingService.ExtractCoverFromEpubAsync(destFilePath, _coversDirectory);
            if (!string.IsNullOrEmpty(coverPath))
            {
                var coverFileName = $"{bookId}_cover{Path.GetExtension(coverPath)}";
                var coverDest = Path.Combine(_coversDirectory, coverFileName);
                if (coverPath != coverDest)
                {
                    File.Copy(coverPath, coverDest, overwrite: true);
                    try { File.Delete(coverPath); } catch { /* ignore */ }
                    coverPath = coverDest;
                }
            }
        }

        var book = new Book
        {
            Id = bookId,
            Title = metadata.Title ?? Path.GetFileNameWithoutExtension(sourceFilePath),
            Author = metadata.Author ?? "Unknown",
            CoverPath = coverPath,
            FilePath = destFilePath,
            Format = format,
            FileSize = fileInfo.Length,
            AddedAt = DateTime.UtcNow,
            LanguagePair = new LanguagePair { SourceLanguage = Language.En, TargetLanguage = Language.En },
            ProficiencyLevel = ProficiencyLevel.Intermediate,
            WordDensity = 0.5,
            Progress = 0,
            CurrentChapter = 0,
            TotalChapters = 0,
            CurrentPage = 0,
            TotalPages = 0,
            ReadingTimeMinutes = 0,
            IsDownloaded = false
        };

        await _storageService.AddBookAsync(book);
        return book;
    }

    public async Task<Book> AddDownloadedBookAsync(string existingFilePath, BookMetadata? metadata = null)
    {
        if (!File.Exists(existingFilePath))
            throw new FileNotFoundException("Book file not found.", existingFilePath);

        if (!IsSupportedFormat(existingFilePath))
            throw new NotSupportedException($"File format not supported. Supported: {string.Join(", ", SupportedExtensions)}");

        var format = GetFormatFromPath(existingFilePath);
        var fileInfo = new FileInfo(existingFilePath);
        var bookDir = Path.GetDirectoryName(existingFilePath)!;
        var bookId = Path.GetFileName(bookDir);

        BookMetadata meta;
        try
        {
            meta = await _bookParserService.GetMetadataAsync(existingFilePath);
        }
        catch (NotSupportedException)
        {
            meta = metadata ?? new BookMetadata
            {
                Title = Path.GetFileNameWithoutExtension(existingFilePath),
                Author = "Unknown"
            };
        }

        if (metadata != null)
        {
            if (!string.IsNullOrEmpty(metadata.Title)) meta.Title = metadata.Title;
            if (!string.IsNullOrEmpty(metadata.Author)) meta.Author = metadata.Author;
            if (!string.IsNullOrEmpty(metadata.CoverUrl)) meta.CoverUrl = metadata.CoverUrl;
        }

        string? coverPath = null;
        if (format == BookFormat.Epub)
        {
            coverPath = await _imageProcessingService.ExtractCoverFromEpubAsync(existingFilePath, _coversDirectory);
            if (!string.IsNullOrEmpty(coverPath))
            {
                var coverFileName = $"{bookId}_cover{Path.GetExtension(coverPath)}";
                var coverDest = Path.Combine(_coversDirectory, coverFileName);
                if (coverPath != coverDest)
                {
                    File.Copy(coverPath, coverDest, overwrite: true);
                    try { File.Delete(coverPath); } catch { /* ignore */ }
                    coverPath = coverDest;
                }
            }
        }

        if (string.IsNullOrEmpty(coverPath) && !string.IsNullOrEmpty(meta.CoverUrl))
            coverPath = await _imageProcessingService.DownloadCoverAsync(meta.CoverUrl, bookId, _coversDirectory);

        var book = new Book
        {
            Id = bookId,
            Title = meta.Title ?? Path.GetFileNameWithoutExtension(existingFilePath),
            Author = meta.Author ?? "Unknown",
            CoverPath = coverPath,
            FilePath = existingFilePath,
            Format = format,
            FileSize = fileInfo.Length,
            AddedAt = DateTime.UtcNow,
            LanguagePair = new LanguagePair { SourceLanguage = Language.En, TargetLanguage = Language.En },
            ProficiencyLevel = ProficiencyLevel.Intermediate,
            WordDensity = 0.5,
            Progress = 0,
            CurrentChapter = 0,
            TotalChapters = 0,
            CurrentPage = 0,
            TotalPages = 0,
            ReadingTimeMinutes = 0,
            SourceUrl = meta.CoverUrl,
            IsDownloaded = true
        };

        await _storageService.AddBookAsync(book);
        return book;
    }

    private static string GenerateBookId(string sourceFilePath)
    {
        var combined = $"{sourceFilePath}_{DateTime.UtcNow.Ticks}";
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}

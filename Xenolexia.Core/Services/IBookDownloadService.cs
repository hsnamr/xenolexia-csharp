using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Book download service interface
/// </summary>
public interface IBookDownloadService
{
    /// <summary>
    /// Search for books from online sources
    /// </summary>
    Task<BookSearchResponse> SearchBooksAsync(string query, EbookSource source = EbookSource.Gutenberg);

    /// <summary>
    /// Download a book from a URL
    /// </summary>
    Task<DownloadResult> DownloadBookAsync(string url, string bookId, IProgress<DownloadProgress>? progress = null);

    /// <summary>
    /// Download a book from a search result
    /// </summary>
    Task<DownloadResult> DownloadBookFromSearchResultAsync(BookSearchResult searchResult, IProgress<DownloadProgress>? progress = null);
}

/// <summary>
/// Ebook source types
/// </summary>
public enum EbookSource
{
    Gutenberg,
    StandardEbooks,
    OpenLibrary,
    DirectUrl
}

/// <summary>
/// Book search result
/// </summary>
public class BookSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public BookFormat Format { get; set; }
    public EbookSource Source { get; set; }
    public string Language { get; set; } = "en";
    public string? Description { get; set; }
    public int? PublishYear { get; set; }
}

/// <summary>
/// Book search response
/// </summary>
public class BookSearchResponse
{
    public List<BookSearchResult> Results { get; set; } = new();
    public string? Error { get; set; }
    public EbookSource Source { get; set; }
}

/// <summary>
/// Download progress
/// </summary>
public class DownloadProgress
{
    public string BookId { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public int Percentage => TotalBytes > 0 ? (int)((BytesDownloaded * 100) / TotalBytes) : 0;
    public DownloadStatus Status { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Download status
/// </summary>
public enum DownloadStatus
{
    Pending,
    Downloading,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Download result
/// </summary>
public class DownloadResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public BookFormat Format { get; set; }
    public long FileSize { get; set; }
    public string? Error { get; set; }
    public BookMetadata? Metadata { get; set; }
}

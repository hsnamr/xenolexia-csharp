using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Service for importing books from local storage
/// </summary>
public interface IBookImportService
{
    /// <summary>
    /// Import a book from a local file path.
    /// Copies the file to the app books directory, extracts metadata and cover when possible, and adds to the library.
    /// </summary>
    /// <param name="sourceFilePath">Full path to the ebook file (EPUB, PDF, TXT, FB2, MOBI)</param>
    /// <returns>The created book entity</returns>
    Task<Book> ImportFromFileAsync(string sourceFilePath);

    /// <summary>
    /// Add a book that was already downloaded into the app books directory (e.g. from online search).
    /// Does not copy the file; uses existing path and optional metadata/cover URL.
    /// </summary>
    /// <param name="existingFilePath">Path to the file inside the app books directory</param>
    /// <param name="metadata">Optional metadata (title, author, cover URL) from the download source</param>
    /// <returns>The created book entity</returns>
    Task<Book> AddDownloadedBookAsync(string existingFilePath, BookMetadata? metadata = null);
}

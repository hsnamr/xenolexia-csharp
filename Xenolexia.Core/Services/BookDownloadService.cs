using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Book download service implementation
/// Supports Project Gutenberg, Standard Ebooks, and Open Library
/// </summary>
public class BookDownloadService : IBookDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly string _booksDirectory;

    public BookDownloadService(string booksDirectory)
    {
        _booksDirectory = booksDirectory;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        
        // Ensure books directory exists
        Directory.CreateDirectory(_booksDirectory);
    }

    public async Task<BookSearchResponse> SearchBooksAsync(string query, EbookSource source = EbookSource.Gutenberg)
    {
        var trimmedQuery = query.Trim();
        if (string.IsNullOrEmpty(trimmedQuery))
        {
            return new BookSearchResponse
            {
                Results = new List<BookSearchResult>(),
                Error = "Please enter a search term",
                Source = source
            };
        }

        try
        {
            List<BookSearchResult> results = source switch
            {
                EbookSource.Gutenberg => await SearchGutenbergAsync(trimmedQuery),
                EbookSource.StandardEbooks => await SearchStandardEbooksAsync(trimmedQuery),
                EbookSource.OpenLibrary => await SearchOpenLibraryAsync(trimmedQuery),
                _ => new List<BookSearchResult>()
            };

            if (results.Count == 0)
            {
                return new BookSearchResponse
                {
                    Results = new List<BookSearchResult>(),
                    Error = $"No books found for \"{trimmedQuery}\". Try different keywords or check another source.",
                    Source = source
                };
            }

            return new BookSearchResponse
            {
                Results = results,
                Source = source
            };
        }
        catch (Exception ex)
        {
            return new BookSearchResponse
            {
                Results = new List<BookSearchResult>(),
                Error = ex.Message,
                Source = source
            };
        }
    }

    public async Task<DownloadResult> DownloadBookAsync(string url, string bookId, IProgress<DownloadProgress>? progress = null)
    {
        try
        {
            var format = DetectFormat(url);
            if (format == null)
            {
                return new DownloadResult
                {
                    Success = false,
                    Error = "Unsupported file format. Only EPUB, FB2, MOBI, and TXT are supported."
                };
            }

            var safeId = Regex.Replace(bookId, @"[^a-zA-Z0-9-_]", "_");
            var filename = $"book.{format.ToString().ToLowerInvariant()}";
            var destPath = Path.Combine(_booksDirectory, safeId, filename);
            var bookDir = Path.GetDirectoryName(destPath)!;
            Directory.CreateDirectory(bookDir);

            var downloadProgress = new DownloadProgress
            {
                BookId = bookId,
                Status = DownloadStatus.Downloading
            };

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            downloadProgress.TotalBytes = totalBytes;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long bytesDownloaded = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                bytesDownloaded += bytesRead;

                downloadProgress.BytesDownloaded = bytesDownloaded;
                progress?.Report(downloadProgress);
            }

            downloadProgress.Status = DownloadStatus.Completed;
            progress?.Report(downloadProgress);

            var fileInfo = new FileInfo(destPath);

            return new DownloadResult
            {
                Success = true,
                FilePath = destPath,
                Format = format.Value,
                FileSize = fileInfo.Length,
                Metadata = new BookMetadata
                {
                    Title = "Downloaded Book",
                    Author = "Unknown"
                }
            };
        }
        catch (Exception ex)
        {
            return new DownloadResult
            {
                Success = false,
                Error = GetErrorMessage(ex)
            };
        }
    }

    public async Task<DownloadResult> DownloadBookFromSearchResultAsync(BookSearchResult searchResult, IProgress<DownloadProgress>? progress = null)
    {
        var result = await DownloadBookAsync(searchResult.DownloadUrl, searchResult.Id, progress);
        
        if (result.Success && result.Metadata != null)
        {
            result.Metadata.Title = searchResult.Title;
            result.Metadata.Author = searchResult.Author;
            result.Metadata.CoverUrl = searchResult.CoverUrl;
            result.Metadata.Description = searchResult.Description;
        }

        return result;
    }

    private async Task<List<BookSearchResult>> SearchGutenbergAsync(string query)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://gutendex.com/books/?search={encodedQuery}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("results", out var resultsArray))
        {
            return new List<BookSearchResult>();
        }

        var results = new List<BookSearchResult>();

        foreach (var book in resultsArray.EnumerateArray().Take(20))
        {
            if (!book.TryGetProperty("formats", out var formats))
                continue;

            string? epubUrl = null;
            string? txtUrl = null;

            if (formats.TryGetProperty("application/epub+zip", out var epub))
                epubUrl = epub.GetString();

            if (formats.TryGetProperty("text/plain; charset=utf-8", out var txt1))
                txtUrl = txt1.GetString();
            else if (formats.TryGetProperty("text/plain", out var txt2))
                txtUrl = txt2.GetString();

            var downloadUrl = epubUrl ?? txtUrl;
            if (string.IsNullOrEmpty(downloadUrl))
                continue;

            var format = epubUrl != null ? BookFormat.Epub : BookFormat.Txt;

            var authors = new List<string>();
            if (book.TryGetProperty("authors", out var authorsArray))
            {
                foreach (var authorItem in authorsArray.EnumerateArray())
                {
                    if (authorItem.TryGetProperty("name", out var name))
                        authors.Add(name.GetString() ?? "");
                }
            }

            var author = authors.Count > 0 ? string.Join(", ", authors) : "Unknown Author";

            string? coverUrl = null;
            if (formats.TryGetProperty("image/jpeg", out var cover))
                coverUrl = cover.GetString();
            else if (book.TryGetProperty("id", out var id))
            {
                var bookId = id.GetInt32();
                coverUrl = $"https://www.gutenberg.org/cache/epub/{bookId}/pg{bookId}.cover.medium.jpg";
            }

            var languages = new List<string>();
            if (book.TryGetProperty("languages", out var langArray))
            {
                foreach (var lang in langArray.EnumerateArray())
                {
                    languages.Add(lang.GetString() ?? "en");
                }
            }

            var subjects = new List<string>();
            if (book.TryGetProperty("subjects", out var subjectsArray))
            {
                foreach (var subject in subjectsArray.EnumerateArray().Take(3))
                {
                    subjects.Add(subject.GetString() ?? "");
                }
            }

            results.Add(new BookSearchResult
            {
                Id = $"gutenberg-{book.GetProperty("id").GetInt32()}",
                Title = book.TryGetProperty("title", out var title) ? title.GetString() ?? "Untitled" : "Untitled",
                Author = author,
                CoverUrl = coverUrl,
                DownloadUrl = downloadUrl,
                Format = format,
                Source = EbookSource.Gutenberg,
                Language = languages.FirstOrDefault() ?? "en",
                Description = subjects.Count > 0 ? string.Join(", ", subjects) : null
            });
        }

        return results;
    }

    private async Task<List<BookSearchResult>> SearchStandardEbooksAsync(string query)
    {
        var url = "https://standardebooks.org/opds/all";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var xmlText = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xmlText);
        var ns = XNamespace.Get("http://www.w3.org/2005/Atom");

        var results = new List<BookSearchResult>();
        var queryLower = query.ToLowerInvariant();

        foreach (var entry in doc.Descendants(ns + "entry").Take(50))
        {
            var title = entry.Element(ns + "title")?.Value ?? "";
            var author = entry.Element(ns + "author")?.Element(ns + "name")?.Value ?? "Unknown Author";

            if (!title.ToLowerInvariant().Contains(queryLower) && 
                !author.ToLowerInvariant().Contains(queryLower))
                continue;

            var id = entry.Element(ns + "id")?.Value ?? "";
            var epubLink = entry.Elements(ns + "link")
                .FirstOrDefault(l => l.Attribute("type")?.Value == "application/epub+zip");

            if (epubLink == null)
                continue;

            var downloadUrl = epubLink.Attribute("href")?.Value ?? "";
            if (!downloadUrl.StartsWith("http"))
                downloadUrl = $"https://standardebooks.org{downloadUrl}";

            var coverLink = entry.Elements(ns + "link")
                .FirstOrDefault(l => l.Attribute("rel")?.Value == "http://opds-spec.org/image");

            var coverUrl = coverLink?.Attribute("href")?.Value;
            if (!string.IsNullOrEmpty(coverUrl) && !coverUrl.StartsWith("http"))
                coverUrl = $"https://standardebooks.org{coverUrl}";

            var summary = entry.Element(ns + "summary")?.Value;

            results.Add(new BookSearchResult
            {
                Id = $"standardebooks-{Regex.Replace(id, @"[^a-zA-Z0-9]", "-")}",
                Title = DecodeXmlEntities(title),
                Author = DecodeXmlEntities(author),
                CoverUrl = coverUrl,
                DownloadUrl = downloadUrl,
                Format = BookFormat.Epub,
                Source = EbookSource.StandardEbooks,
                Language = "en",
                Description = summary != null ? DecodeXmlEntities(summary) : null
            });

            if (results.Count >= 20)
                break;
        }

        return results;
    }

    private async Task<List<BookSearchResult>> SearchOpenLibraryAsync(string query)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://openlibrary.org/search.json?q={encodedQuery}&has_fulltext=true&limit=20";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("docs", out var docsArray))
        {
            return new List<BookSearchResult>();
        }

        var results = new List<BookSearchResult>();

        foreach (var book in docsArray.EnumerateArray())
        {
            if (!book.TryGetProperty("has_fulltext", out var hasFulltext) || !hasFulltext.GetBoolean())
                continue;

            if (!book.TryGetProperty("ia", out var iaArray) || iaArray.GetArrayLength() == 0)
                continue;

            var iaId = iaArray[0].GetString();
            if (string.IsNullOrEmpty(iaId))
                continue;

            var downloadUrl = $"https://archive.org/download/{iaId}/{iaId}.epub";

            var authorNames = new List<string>();
            if (book.TryGetProperty("author_name", out var authorArray))
            {
                foreach (var authorItem in authorArray.EnumerateArray())
                {
                    authorNames.Add(authorItem.GetString() ?? "");
                }
            }

            var author = authorNames.Count > 0 ? string.Join(", ", authorNames) : "Unknown Author";

            string? coverUrl = null;
            if (book.TryGetProperty("cover_i", out var coverI))
            {
                var coverId = coverI.GetInt32();
                coverUrl = $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
            }

            var languages = new List<string>();
            if (book.TryGetProperty("language", out var langArray))
            {
                foreach (var lang in langArray.EnumerateArray())
                {
                    languages.Add(lang.GetString() ?? "en");
                }
            }

            var firstSentence = "";
            if (book.TryGetProperty("first_sentence", out var sentenceArray) && sentenceArray.GetArrayLength() > 0)
            {
                firstSentence = sentenceArray[0].GetString() ?? "";
            }

            int? publishYear = null;
            if (book.TryGetProperty("first_publish_year", out var year))
            {
                publishYear = year.GetInt32();
            }

            var bookKey = book.TryGetProperty("key", out var key) ? key.GetString()?.Replace("/works/", "") ?? iaId : iaId;
            results.Add(new BookSearchResult
            {
                Id = $"openlibrary-{bookKey}",
                Title = book.TryGetProperty("title", out var title) ? title.GetString() ?? "Untitled" : "Untitled",
                Author = author,
                CoverUrl = coverUrl,
                DownloadUrl = downloadUrl,
                Format = BookFormat.Epub,
                Source = EbookSource.OpenLibrary,
                Language = languages.FirstOrDefault() ?? "en",
                Description = firstSentence,
                PublishYear = publishYear
            });
        }

        return results;
    }

    private BookFormat? DetectFormat(string url)
    {
        var lower = url.ToLowerInvariant();
        if (lower.EndsWith(".epub")) return BookFormat.Epub;
        if (lower.EndsWith(".fb2")) return BookFormat.Fb2;
        if (lower.EndsWith(".mobi") || lower.EndsWith(".azw") || lower.EndsWith(".azw3")) return BookFormat.Mobi;
        if (lower.EndsWith(".txt")) return BookFormat.Txt;
        return null;
    }

    private string GetErrorMessage(Exception ex)
    {
        var message = ex.Message;

        if (message.Contains("Network") || message.Contains("connection"))
            return "Network error. Please check your internet connection and try again.";
        if (message.Contains("404"))
            return "Book file not found. This book may no longer be available.";
        if (message.Contains("403"))
            return "Access denied. This book may require special permissions to download.";

        return message;
    }

    private string DecodeXmlEntities(string str)
    {
        return str
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

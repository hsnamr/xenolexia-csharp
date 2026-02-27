using System.Text;
using Fb2.Document.Constants;
using HtmlAgilityPack;
using UglyToad.PdfPig;
using VersOne.Epub;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Book parser service. EPUB is parsed with EpubCore (MPL-2.0, FOSS).
/// PDF = PdfPig, FB2 = Fb2.Document, MOBI = native lib only. TXT = .NET BCL.
/// </summary>
public class BookParserService : IBookParserService
{
    public async Task<ParsedBook> ParseBookAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Book file path is missing.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Book file not found: {filePath}", filePath);

        var format = DetectFormat(filePath);
        if (format != BookFormat.Epub && format != BookFormat.Txt && format != BookFormat.Pdf && format != BookFormat.Fb2 && format != BookFormat.Mobi)
            throw new NotSupportedException($"Format {format} is not supported for parsing.");

        return format switch
        {
            BookFormat.Epub => await ParseEpubAsync(filePath),
            BookFormat.Txt => await ParseTxtAsync(filePath),
            BookFormat.Pdf => await ParsePdfAsync(filePath),
            BookFormat.Fb2 => await ParseFb2Async(filePath),
            BookFormat.Mobi => await ParseMobiAsync(filePath),
            _ => throw new NotSupportedException($"Format {format} is not yet supported")
        };
    }

    public async Task<Chapter> GetChapterAsync(string filePath, int chapterIndex)
    {
        if (chapterIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chapterIndex), chapterIndex, "Chapter index cannot be negative.");
        var parsed = await ParseBookAsync(filePath);
        if (parsed?.Chapters == null)
            throw new InvalidOperationException("Book parsing produced no chapters.");
        if (chapterIndex >= parsed.Chapters.Count)
            throw new ArgumentOutOfRangeException(nameof(chapterIndex), chapterIndex, $"Chapter index must be less than {parsed.Chapters.Count}.");
        return parsed.Chapters[chapterIndex];
    }

    public async Task<List<TableOfContentsItem>> GetTableOfContentsAsync(string filePath)
    {
        var parsed = await ParseBookAsync(filePath);
        if (parsed?.TableOfContents == null)
            return new List<TableOfContentsItem>();
        return parsed.TableOfContents;
    }

    public async Task<BookMetadata> GetMetadataAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Book file path is missing.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Book file not found: {filePath}", filePath);

        var format = DetectFormat(filePath);
        if (format == BookFormat.Epub)
        {
            try
            {
                var book = await EpubReader.ReadBookAsync(filePath);
                if (book == null)
                    return new BookMetadata { Title = "Unknown", Subjects = new List<string>() };
                return new BookMetadata
                {
                    Title = book.Title ?? "Unknown",
                    Author = book.Author ?? (book.AuthorList != null && book.AuthorList.Count > 0 ? string.Join(", ", book.AuthorList) : null),
                    Description = book.Description,
                    Subjects = new List<string>()
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"EPUB file could not be read: {filePath}. The file may be corrupted or invalid.", ex);
            }
        }
        if (format == BookFormat.Pdf)
            return await GetPdfMetadataAsync(filePath);
        if (format == BookFormat.Fb2)
        {
            var fb2Book = Fb2Native.TryParseFb2(filePath);
            if (fb2Book != null)
                return await Task.FromResult(fb2Book.Metadata);
            return await GetFb2MetadataAsync(filePath);
        }
        if (format == BookFormat.Mobi)
        {
            var mobiBook = MobiNative.TryParseMobi(filePath);
            if (mobiBook != null)
                return await Task.FromResult(mobiBook.Metadata);
        }
        var parsed = await ParseBookAsync(filePath);
        return parsed.Metadata;
    }

    private static BookFormat DetectFormat(string filePath)
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

    private static async Task<ParsedBook> ParseEpubAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("EPUB file path is invalid or file does not exist.", filePath);
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext != ".epub")
            throw new ArgumentException($"Expected .epub file, got: {ext}", nameof(filePath));

        EpubBook book;
        try
        {
            book = await EpubReader.ReadBookAsync(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"EPUB file could not be opened: {filePath}. The file may be corrupted or not a valid EPUB.", ex);
        }
        if (book == null)
            throw new InvalidOperationException($"EPUB reader returned no book for: {filePath}");

        var metadata = new BookMetadata
        {
            Title = book.Title ?? "Unknown",
            Author = book.Author ?? (book.AuthorList != null && book.AuthorList.Count > 0 ? string.Join(", ", book.AuthorList) : null),
            Description = book.Description,
            Subjects = new List<string>()
        };

        var chapters = new List<Chapter>();
        var readingOrder = book.ReadingOrder ?? new List<EpubLocalTextContentFile>();

        for (var index = 0; index < readingOrder.Count; index++)
        {
            var file = readingOrder[index];
            var htmlContent = file.Content ?? string.Empty;
            var plainText = ExtractPlainTextFromHtml(htmlContent);
            var wordCount = CountWords(plainText);
            var title = GetChapterTitle(book, file, index);

            chapters.Add(new Chapter
            {
                Id = $"chapter-{index}",
                Title = title,
                Index = index,
                Content = htmlContent,
                WordCount = wordCount,
                Href = file.FilePath ?? string.Empty
            });
        }

        var toc = BuildTableOfContents(book.Navigation);

        return new ParsedBook
        {
            Metadata = metadata,
            Chapters = chapters,
            TableOfContents = toc,
            TotalWordCount = chapters.Sum(c => c.WordCount)
        };
    }

    /// <summary>
    /// Extracts plain text from EPUB HTML using HtmlAgilityPack (per VersOne.Epub recommendations).
    /// </summary>
    private static string ExtractPlainTextFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var sb = new StringBuilder();
        var textNodes = doc.DocumentNode.SelectNodes("//text()");
        if (textNodes == null)
            return doc.DocumentNode.InnerText?.Trim() ?? string.Empty;

        foreach (var node in textNodes)
        {
            var text = node.InnerText?.Trim();
            if (string.IsNullOrEmpty(text))
                continue;
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(text);
        }

        return sb.ToString().Trim();
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string GetChapterTitle(EpubBook book, EpubLocalTextContentFile file, int index)
    {
        if (book.Navigation == null)
            return $"Chapter {index + 1}";

        var fileName = file.FilePath ?? string.Empty;
        var navItem = FindNavigationByFile(book.Navigation, fileName);
        return !string.IsNullOrWhiteSpace(navItem?.Title) ? navItem.Title : $"Chapter {index + 1}";
    }

    private static EpubNavigationItem? FindNavigationByFile(List<EpubNavigationItem> items, string fileName)
    {
        foreach (var item in items)
        {
            var linkPath = item.Link?.ContentFilePath ?? string.Empty;
            if (!string.IsNullOrEmpty(linkPath) && linkPath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                return item;
            if (item.NestedItems != null)
            {
                var found = FindNavigationByFile(item.NestedItems, fileName);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    private static List<TableOfContentsItem> BuildTableOfContents(List<EpubNavigationItem>? navigation)
    {
        var result = new List<TableOfContentsItem>();
        if (navigation == null)
            return result;

        void AddItems(IEnumerable<EpubNavigationItem> items, int level)
        {
            foreach (var item in items)
            {
                result.Add(new TableOfContentsItem
                {
                    Id = item.Title ?? string.Empty,
                    Title = item.Title ?? string.Empty,
                    Href = item.Link?.ContentFilePath ?? string.Empty,
                    Level = level
                });
                if (item.NestedItems != null && item.NestedItems.Count > 0)
                    AddItems(item.NestedItems, level + 1);
            }
        }
        AddItems(navigation, 0);
        return result;
    }

    private static async Task<ParsedBook> ParsePdfAsync(string filePath)
    {
        await Task.Yield();
        using var document = PdfDocument.Open(filePath);
        var info = document.Information;
        var metadata = new BookMetadata
        {
            Title = info.Title ?? Path.GetFileNameWithoutExtension(filePath),
            Author = info.Author,
            Subjects = new List<string>()
        };
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrEmpty(text))
                sb.AppendLine(text);
        }
        var content = sb.ToString().Trim();
        var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var chapters = new List<Chapter>();
        if (wordCount > 0)
        {
            chapters.Add(new Chapter
            {
                Id = "chapter-0",
                Title = "Content",
                Index = 0,
                Content = content,
                WordCount = wordCount
            });
        }
        return new ParsedBook
        {
            Metadata = metadata,
            Chapters = chapters,
            TableOfContents = new List<TableOfContentsItem>(),
            TotalWordCount = wordCount
        };
    }

    private static async Task<BookMetadata> GetPdfMetadataAsync(string filePath)
    {
        await Task.Yield();
        using var document = PdfDocument.Open(filePath);
        var info = document.Information;
        return new BookMetadata
        {
            Title = info.Title ?? Path.GetFileNameWithoutExtension(filePath),
            Author = info.Author,
            Subjects = new List<string>()
        };
    }

    private static async Task<ParsedBook> ParseFb2Async(string filePath)
    {
        var nativeBook = Fb2Native.TryParseFb2(filePath);
        if (nativeBook != null)
            return await Task.FromResult(nativeBook);
        var doc = new Fb2.Document.Fb2Document();
        await using (var stream = File.OpenRead(filePath))
            await doc.LoadAsync(stream);
        var metadata = await GetFb2MetadataFromDocumentAsync(doc, filePath);
        var chapters = new List<Chapter>();
        var body = doc.Book?.GetFirstDescendant(ElementNames.BookBody) as Fb2.Document.Models.Base.Fb2Container;
        if (body != null)
        {
            var sections = body.GetDescendants(ElementNames.BookBodySection).ToList();
            var index = 0;
            foreach (var section in sections)
            {
                if (section is not Fb2.Document.Models.Base.Fb2Container sectionContainer)
                    continue;
                var titleNode = sectionContainer.GetFirstChild(ElementNames.Title);
                var title = titleNode is Fb2.Document.Models.Base.Fb2Element titleEl
                    ? titleEl.Content
                    : $"Section {index + 1}";
                var sb = new StringBuilder();
                foreach (var p in sectionContainer.GetDescendants(ElementNames.Paragraph))
                {
                    if (p is Fb2.Document.Models.Base.Fb2Container pContainer)
                        AppendTextFromNode(pContainer, sb);
                    else if (p is Fb2.Document.Models.Base.Fb2Element pel && !string.IsNullOrWhiteSpace(pel.Content))
                        sb.AppendLine(pel.Content);
                }
                var content = sb.ToString().Trim();
                var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                chapters.Add(new Chapter
                {
                    Id = $"chapter-{index}",
                    Title = title,
                    Index = index,
                    Content = content,
                    WordCount = wordCount
                });
                index++;
            }
            if (chapters.Count == 0)
            {
                var fullText = new StringBuilder();
                foreach (var p in body.GetDescendants(ElementNames.Paragraph))
                {
                    if (p is Fb2.Document.Models.Base.Fb2Container pContainer)
                        AppendTextFromNode(pContainer, fullText);
                    else if (p is Fb2.Document.Models.Base.Fb2Element pel && !string.IsNullOrWhiteSpace(pel.Content))
                        fullText.AppendLine(pel.Content);
                }
                var ct = fullText.ToString().Trim();
                var wc = ct.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                chapters.Add(new Chapter
                {
                    Id = "chapter-0",
                    Title = "Content",
                    Index = 0,
                    Content = ct,
                    WordCount = wc
                });
            }
        }
        return new ParsedBook
        {
            Metadata = metadata,
            Chapters = chapters,
            TableOfContents = new List<TableOfContentsItem>(),
            TotalWordCount = chapters.Sum(c => c.WordCount)
        };
    }

    private static async Task<BookMetadata> GetFb2MetadataAsync(string filePath)
    {
        var doc = new Fb2.Document.Fb2Document();
        await using (var stream = File.OpenRead(filePath))
            await doc.LoadAsync(stream);
        return await GetFb2MetadataFromDocumentAsync(doc);
    }

    private static Task<BookMetadata> GetFb2MetadataFromDocumentAsync(Fb2.Document.Fb2Document doc, string? filePath = null)
    {
        var title = !string.IsNullOrEmpty(filePath) ? Path.GetFileNameWithoutExtension(filePath) : "Unknown";
        var author = (string?)null;
        var titleInfo = doc.Book?.GetFirstDescendant(ElementNames.TitleInfo);
        if (titleInfo is Fb2.Document.Models.Base.Fb2Container titleContainer)
        {
            var titleEl = titleContainer.GetFirstChild(ElementNames.BookTitle) as Fb2.Document.Models.Base.Fb2Element;
            if (titleEl != null)
                title = titleEl.Content;
            var authorEl = titleContainer.GetFirstChild(ElementNames.Author) as Fb2.Document.Models.Base.Fb2Container;
            if (authorEl != null)
            {
                var parts = new List<string>();
                var fn = authorEl.GetFirstChild(ElementNames.FirstName) as Fb2.Document.Models.Base.Fb2Element;
                var ln = authorEl.GetFirstChild(ElementNames.LastName) as Fb2.Document.Models.Base.Fb2Element;
                if (fn != null) parts.Add(fn.Content);
                if (ln != null) parts.Add(ln.Content);
                if (parts.Count > 0)
                    author = string.Join(" ", parts);
            }
        }
        return Task.FromResult(new BookMetadata
        {
            Title = title,
            Author = author,
            Subjects = new List<string>()
        });
    }

    private static void AppendTextFromNode(Fb2.Document.Models.Base.Fb2Container container, StringBuilder sb)
    {
        foreach (var node in container.Content)
        {
            if (node is Fb2.Document.Models.Base.Fb2Element el && !string.IsNullOrWhiteSpace(el.Content))
                sb.Append(el.Content);
            else if (node is Fb2.Document.Models.Base.Fb2Container child)
                AppendTextFromNode(child, sb);
        }
        sb.AppendLine();
    }

    private static async Task<ParsedBook> ParseMobiAsync(string filePath)
    {
        var nativeBook = MobiNative.TryParseMobi(filePath);
        if (nativeBook != null)
            return await Task.FromResult(nativeBook);
        throw new NotSupportedException("MOBI parsing requires libxenolexia_mobi (xenolexia-shared-c with ENABLE_LIBMOBI=1).");
    }

    private static async Task<ParsedBook> ParseTxtAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return new ParsedBook
        {
            Metadata = new BookMetadata { Title = Path.GetFileNameWithoutExtension(filePath) },
            Chapters = new List<Chapter>
            {
                new Chapter
                {
                    Id = "chapter-0",
                    Title = "Content",
                    Index = 0,
                    Content = content,
                    WordCount = wordCount
                }
            },
            TableOfContents = new List<TableOfContentsItem>(),
            TotalWordCount = wordCount
        };
    }
}

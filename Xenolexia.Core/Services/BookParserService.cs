using System.Text;
using Fb2.Document.Constants;
using EpubCore;
using EpubCore.Format;
using Xenolexia.Core.Models;
using UglyToad.PdfPig;

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
            EpubBook book;
            try
            {
                book = await Task.Run(() => EpubReader.Read(filePath));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"EPUB file could not be read: {filePath}. The file may be corrupted or invalid.", ex);
            }
            if (book == null)
                throw new InvalidOperationException($"EPUB reader returned no book for: {filePath}");
            if (book.Format?.Opf?.Metadata == null)
                return new BookMetadata { Title = "Unknown", Subjects = new List<string>() };
            return new BookMetadata
            {
                Title = book.Title ?? "Unknown",
                Author = book.Authors != null && book.Authors.Cast<object>().Any() ? string.Join(", ", book.Authors) : null,
                Description = null,
                Language = null,
                Publisher = null,
                PublishDate = null,
                Isbn = null,
                Subjects = new List<string>()
            };
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
            book = await Task.Run(() => EpubReader.Read(filePath));
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
            Author = book.Authors != null && book.Authors.Cast<object>().Any() ? string.Join(", ", book.Authors) : null,
            Description = null,
            Subjects = new List<string>()
        };

        var chapters = new List<Chapter>();
        // Prefer HTML in reading order (EpubCore) when available
        var htmlInOrder = book.SpecialResources?.HtmlInReadingOrder;
        if (htmlInOrder != null && htmlInOrder.Count > 0)
        {
            var index = 0;
            foreach (var html in htmlInOrder)
            {
                if (html == null) continue;
                var content = html.TextContent ?? "";
                var fileName = html.FileName ?? "";
                var normalizedHref = NormalizeHref(fileName);
                var tocChapter = book.TableOfContents?.FirstOrDefault(c =>
                    string.Equals(NormalizeHref(c.RelativePath ?? c.AbsolutePath ?? ""), normalizedHref, StringComparison.OrdinalIgnoreCase));
                var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t', '<', '>', '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
                chapters.Add(new Chapter
                {
                    Id = $"chapter-{index}",
                    Title = tocChapter?.Title ?? $"Chapter {index + 1}",
                    Index = index,
                    Content = content,
                    WordCount = wordCount,
                    Href = fileName
                });
                index++;
            }
        }
        else
        {
            var opf = book.Format?.Opf;
            var htmlByHref = (book.Resources?.Html ?? Array.Empty<EpubTextFile>())
                .ToDictionary(f => NormalizeHref(f.FileName), f => f.TextContent ?? "", StringComparer.OrdinalIgnoreCase);
            var manifestItems = opf?.Manifest?.Items ?? Array.Empty<OpfManifestItem>();
            var manifestById = manifestItems.ToDictionary(m => m.Id ?? "", m => m.Href ?? "", StringComparer.OrdinalIgnoreCase);

            if (opf?.Spine?.ItemRefs != null && opf.Spine.ItemRefs.Count > 0)
            {
                var index = 0;
                foreach (var itemRef in opf.Spine.ItemRefs)
                {
                    var idRef = itemRef.IdRef ?? "";
                    if (!manifestById.TryGetValue(idRef, out var href))
                        continue;
                    var normalizedHref = NormalizeHref(href);
                    if (!htmlByHref.TryGetValue(normalizedHref, out var content))
                        content = "";
                    var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t', '<', '>', '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    var tocChapter = book.TableOfContents?.FirstOrDefault(c =>
                        string.Equals(NormalizeHref(c.RelativePath ?? c.AbsolutePath ?? ""), normalizedHref, StringComparison.OrdinalIgnoreCase));
                    chapters.Add(new Chapter
                    {
                        Id = $"chapter-{index}",
                        Title = tocChapter?.Title ?? $"Chapter {index + 1}",
                        Index = index,
                        Content = content,
                        WordCount = wordCount,
                        Href = href
                    });
                    index++;
                }
            }

            if (chapters.Count == 0 && book.Resources?.Html != null)
            {
                var idx = 0;
                foreach (var html in book.Resources.Html)
                {
                    if (html == null) continue;
                    var content = html.TextContent ?? "";
                    var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t', '<', '>', '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    chapters.Add(new Chapter
                    {
                        Id = $"chapter-{idx}",
                        Title = $"Chapter {idx + 1}",
                        Index = idx,
                        Content = content,
                        WordCount = wordCount,
                        Href = html.FileName ?? ""
                    });
                    idx++;
                }
            }
        }

        var toc = new List<TableOfContentsItem>();
        if (book.TableOfContents != null)
        {
            foreach (var ch in book.TableOfContents)
            {
                if (ch == null) continue;
                toc.Add(new TableOfContentsItem
                {
                    Id = ch.Title ?? "",
                    Title = ch.Title ?? "",
                    Href = ch.RelativePath ?? ch.AbsolutePath ?? "",
                    Level = 0
                });
            }
        }

        return new ParsedBook
        {
            Metadata = metadata ?? new BookMetadata { Title = "Unknown", Subjects = new List<string>() },
            Chapters = chapters ?? new List<Chapter>(),
            TableOfContents = toc ?? new List<TableOfContentsItem>(),
            TotalWordCount = chapters?.Sum(c => c?.WordCount ?? 0) ?? 0
        };
    }

    private static string NormalizeHref(string href)
    {
        if (string.IsNullOrEmpty(href)) return "";
        var u = href.Replace('\\', '/').TrimStart('/');
        return u;
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

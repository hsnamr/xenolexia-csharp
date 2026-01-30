using Xenolexia.Core.Models;
using VersOne.Epub;

namespace Xenolexia.Core.Services;

/// <summary>
/// Book parser service using VersOne.Epub for EPUB and minimal custom logic for TXT/PDF.
/// </summary>
public class BookParserService : IBookParserService
{
    public async Task<ParsedBook> ParseBookAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Book file not found: {filePath}");

        var format = DetectFormat(filePath);
        return format switch
        {
            BookFormat.Epub => await ParseEpubAsync(filePath),
            BookFormat.Txt => await ParseTxtAsync(filePath),
            BookFormat.Pdf => ParsePdfMetadataOnly(filePath),
            BookFormat.Fb2 or BookFormat.Mobi => throw new NotSupportedException($"Format {format} can be imported but full parsing is not yet supported"),
            _ => throw new NotSupportedException($"Format {format} is not yet supported")
        };
    }

    public async Task<Chapter> GetChapterAsync(string filePath, int chapterIndex)
    {
        var parsed = await ParseBookAsync(filePath);
        if (chapterIndex < 0 || chapterIndex >= parsed.Chapters.Count)
            throw new ArgumentOutOfRangeException(nameof(chapterIndex));
        return parsed.Chapters[chapterIndex];
    }

    public async Task<List<TableOfContentsItem>> GetTableOfContentsAsync(string filePath)
    {
        var parsed = await ParseBookAsync(filePath);
        return parsed.TableOfContents;
    }

    public async Task<BookMetadata> GetMetadataAsync(string filePath)
    {
        var format = DetectFormat(filePath);
        if (format == BookFormat.Epub)
        {
            using var bookRef = await EpubReader.OpenBookAsync(filePath);
            return new BookMetadata
            {
                Title = bookRef.Title ?? "Unknown",
                Author = bookRef.Author,
                Description = bookRef.Description,
                Language = null,
                Publisher = null,
                PublishDate = null,
                Isbn = null,
                Subjects = new List<string>()
            };
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
        var book = await EpubReader.ReadBookAsync(filePath);
        var metadata = new BookMetadata
        {
            Title = book.Title ?? "Unknown",
            Author = book.Author,
            Description = book.Description,
            Subjects = new List<string>()
        };

        var chapters = new List<Chapter>();
        if (book.ReadingOrder != null)
        {
            var index = 0;
            foreach (var contentFile in book.ReadingOrder)
            {
                var content = contentFile.Content ?? "";
                var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t', '<', '>', '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
                chapters.Add(new Chapter
                {
                    Id = $"chapter-{index}",
                    Title = $"Chapter {index + 1}",
                    Index = index,
                    Content = content,
                    WordCount = wordCount,
                    Href = contentFile.Key
                });
                index++;
            }
        }

        var toc = new List<TableOfContentsItem>();
        if (book.Navigation != null)
        {
            void AddNavItems(IEnumerable<VersOne.Epub.EpubNavigationItem> items, int level)
            {
                foreach (var item in items)
                {
                    toc.Add(new TableOfContentsItem
                    {
                        Id = item.Title ?? "",
                        Title = item.Title ?? "",
                        Href = item.Link?.ContentFilePath ?? "",
                        Level = level
                    });
                    if (item.NestedItems?.Count > 0)
                        AddNavItems(item.NestedItems, level + 1);
                }
            }
            AddNavItems(book.Navigation, 0);
        }

        return new ParsedBook
        {
            Metadata = metadata,
            Chapters = chapters,
            TableOfContents = toc,
            TotalWordCount = chapters.Sum(c => c.WordCount)
        };
    }

    private static ParsedBook ParsePdfMetadataOnly(string filePath)
    {
        var title = Path.GetFileNameWithoutExtension(filePath);
        return new ParsedBook
        {
            Metadata = new BookMetadata { Title = title },
            Chapters = new List<Chapter>(),
            TableOfContents = new List<TableOfContentsItem>(),
            TotalWordCount = 0
        };
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

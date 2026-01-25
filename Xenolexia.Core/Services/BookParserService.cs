using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using HtmlAgilityPack;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// EPUB book parser service
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
        var parsed = await ParseBookAsync(filePath);
        return parsed.Metadata;
    }

    private BookFormat DetectFormat(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".epub" => BookFormat.Epub,
            ".fb2" => BookFormat.Fb2,
            ".mobi" => BookFormat.Mobi,
            ".txt" => BookFormat.Txt,
            _ => throw new NotSupportedException($"Unsupported file format: {ext}")
        };
    }

    private async Task<ParsedBook> ParseEpubAsync(string filePath)
    {
        using var zip = ZipFile.OpenRead(filePath);
        
        // Read container.xml to find OPF file
        var containerEntry = zip.GetEntry("META-INF/container.xml");
        if (containerEntry == null)
            throw new InvalidDataException("Invalid EPUB: container.xml not found");

        var containerXml = await ReadZipEntryAsync(containerEntry);
        var containerDoc = XDocument.Parse(containerXml);
        var opfPath = containerDoc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "rootfile")?
            .Attribute("full-path")?.Value;

        if (string.IsNullOrEmpty(opfPath))
            throw new InvalidDataException("Invalid EPUB: OPF path not found");

        // Read OPF file
        var opfEntry = zip.GetEntry(opfPath);
        if (opfEntry == null)
            throw new InvalidDataException($"Invalid EPUB: OPF file not found: {opfPath}");

        var opfXml = await ReadZipEntryAsync(opfEntry);
        var opfDoc = XDocument.Parse(opfXml);
        var ns = opfDoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        // Extract metadata
        var metadata = ExtractMetadata(opfDoc, ns);

        // Extract chapters
        var chapters = await ExtractChaptersAsync(zip, opfDoc, ns, opfPath);

        // Extract TOC
        var toc = ExtractTableOfContents(zip, opfDoc, ns);

        return new ParsedBook
        {
            Metadata = metadata,
            Chapters = chapters,
            TableOfContents = toc,
            TotalWordCount = chapters.Sum(c => c.WordCount)
        };
    }

    private BookMetadata ExtractMetadata(XDocument opfDoc, XNamespace ns)
    {
        var metadata = new BookMetadata();
        var metadataElement = opfDoc.Descendants(ns + "metadata").FirstOrDefault();

        if (metadataElement != null)
        {
            metadata.Title = metadataElement.Descendants(ns + "title").FirstOrDefault()?.Value ?? "Unknown";
            metadata.Author = metadataElement.Descendants(ns + "creator").FirstOrDefault()?.Value;
            metadata.Description = metadataElement.Descendants(ns + "description").FirstOrDefault()?.Value;
            metadata.Language = metadataElement.Descendants(ns + "language").FirstOrDefault()?.Value;
            metadata.Publisher = metadataElement.Descendants(ns + "publisher").FirstOrDefault()?.Value;
            metadata.PublishDate = metadataElement.Descendants(ns + "date").FirstOrDefault()?.Value;
            metadata.Isbn = metadataElement.Descendants().FirstOrDefault(e => e.Attribute("property")?.Value == "dcterms:identifier")?.Value;
        }

        return metadata;
    }

    private async Task<List<Chapter>> ExtractChaptersAsync(ZipArchive zip, XDocument opfDoc, XNamespace ns, string opfPath)
    {
        var chapters = new List<Chapter>();
        var opfDir = Path.GetDirectoryName(opfPath)?.Replace('\\', '/') ?? "";

        var manifest = opfDoc.Descendants(ns + "manifest").FirstOrDefault();
        var spine = opfDoc.Descendants(ns + "spine").FirstOrDefault();

        if (manifest == null || spine == null)
            return chapters;

        var itemRefs = spine.Descendants(ns + "itemref").ToList();
        int index = 0;

        foreach (var itemRef in itemRefs)
        {
            var idref = itemRef.Attribute("idref")?.Value;
            if (string.IsNullOrEmpty(idref)) continue;

            var item = manifest.Descendants(ns + "item")
                .FirstOrDefault(i => i.Attribute("id")?.Value == idref);

            if (item == null) continue;

            var href = item.Attribute("href")?.Value;
            if (string.IsNullOrEmpty(href)) continue;

            var fullPath = string.IsNullOrEmpty(opfDir) ? href : $"{opfDir}/{href}";
            var entry = zip.GetEntry(fullPath);
            if (entry == null) continue;

            var content = await ReadZipEntryAsync(entry);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);

            var title = htmlDoc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? $"Chapter {index + 1}";
            var body = htmlDoc.DocumentNode.SelectSingleNode("//body");
            var text = body?.InnerText ?? "";
            var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            chapters.Add(new Chapter
            {
                Id = $"chapter-{index}",
                Title = title,
                Index = index,
                Content = content,
                WordCount = wordCount,
                Href = href
            });

            index++;
        }

        return chapters;
    }

    private List<TableOfContentsItem> ExtractTableOfContents(ZipArchive zip, XDocument opfDoc, XNamespace ns)
    {
        var toc = new List<TableOfContentsItem>();

        // Try to find NCX file
        var manifest = opfDoc.Descendants(ns + "manifest").FirstOrDefault();
        var tocItem = manifest?.Descendants(ns + "item")
            .FirstOrDefault(i => i.Attribute("properties")?.Value?.Contains("nav") == true ||
                                 i.Attribute("id")?.Value?.ToLower().Contains("toc") == true);

        // For now, create a simple TOC from chapters
        // Full NCX parsing would require more complex logic
        return toc;
    }

    private async Task<ParsedBook> ParseTxtAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var lines = content.Split('\n');
        var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        return new ParsedBook
        {
            Metadata = new BookMetadata
            {
                Title = Path.GetFileNameWithoutExtension(filePath)
            },
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

    private async Task<string> ReadZipEntryAsync(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}

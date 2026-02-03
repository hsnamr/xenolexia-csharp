using System.IO.Compression;
using System.Text;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;
using Xunit;

namespace Xenolexia.Core.Tests;

/// <summary>
/// Unit tests for BookParserService. Confirms EPUB parsing (VersOne.Epub, FOSS) returns chapters with content.
/// </summary>
public class BookParserServiceTests
{
    private readonly BookParserService _parser = new();

    /// <summary>
    /// Creates a minimal valid EPUB 2 file (ZIP) with one chapter containing readable text.
    /// VersOne.Epub expects: mimetype (first, stored), META-INF/container.xml, OEBPS/content.opf, OEBPS/chapter1.xhtml.
    /// </summary>
    private static string CreateMinimalEpubToTemp()
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_epub_{Guid.NewGuid():N}.epub");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            // mimetype must be first and stored (no compression)
            var mimetype = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var w = new StreamWriter(mimetype.Open(), Encoding.ASCII, leaveOpen: false))
                w.Write("application/epub+zip");

            AddEntry(zip, "META-INF/container.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles>
                    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
                  </rootfiles>
                </container>
                """);

            AddEntry(zip, "OEBPS/content.opf", """
                <?xml version="1.0" encoding="UTF-8"?>
                <package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="uid">
                  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:title>Test Book</dc:title>
                    <dc:identifier id="uid">test-id-1</dc:identifier>
                    <dc:language>en</dc:language>
                  </metadata>
                  <manifest>
                    <item id="ncx" href="toc.ncx" media-type="application/x-dtbncx+xml"/>
                    <item id="chapter1" href="chapter1.xhtml" media-type="application/xhtml+xml"/>
                  </manifest>
                  <spine toc="ncx">
                    <itemref idref="chapter1"/>
                  </spine>
                </package>
                """);

            AddEntry(zip, "OEBPS/toc.ncx", """
                <?xml version="1.0" encoding="UTF-8"?>
                <ncx xmlns="http://www.daisy.org/z3986/2005/ncx/" version="2005-1">
                  <head><meta name="dtb:uid" content="test-id-1"/></head>
                  <docTitle><text>Test Book</text></docTitle>
                  <navMap>
                    <navPoint id="nav1" playOrder="1">
                      <navLabel><text>Chapter 1</text></navLabel>
                      <content src="chapter1.xhtml"/>
                    </navPoint>
                  </navMap>
                </ncx>
                """);

            AddEntry(zip, "OEBPS/chapter1.xhtml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE html>
                <html xmlns="http://www.w3.org/1999/xhtml">
                <head><title>Chapter 1</title></head>
                <body>
                  <p>Hello, this is the first chapter.</p>
                  <p>It has readable text for unit tests.</p>
                </body>
                </html>
                """);
        }
        return path;
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name);
        using var w = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
        w.Write(content);
    }

    [Fact]
    public async Task ParseEpubAsync_ReturnsParsedBook_WithNonEmptyChapters()
    {
        var epubPath = CreateMinimalEpubToTemp();
        try
        {
            var parsed = await _parser.ParseBookAsync(epubPath);

            Assert.NotNull(parsed);
            Assert.NotNull(parsed.Metadata);
            Assert.Equal("Test Book", parsed.Metadata.Title);
            Assert.NotNull(parsed.Chapters);
            Assert.NotEmpty(parsed.Chapters);

            var first = parsed.Chapters[0];
            Assert.NotNull(first.Content);
            Assert.True(first.Content.Length > 0, "Chapter Content must not be empty so reader can display text.");
            Assert.Contains("Hello", first.Content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("first chapter", first.Content, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(epubPath))
                File.Delete(epubPath);
        }
    }

    [Fact]
    public async Task ParseEpubAsync_GetChapterAsync_ReturnsSameContent()
    {
        var epubPath = CreateMinimalEpubToTemp();
        try
        {
            var parsed = await _parser.ParseBookAsync(epubPath);
            var chapter0 = await _parser.GetChapterAsync(epubPath, 0);

            Assert.NotNull(parsed.Chapters);
            Assert.NotEmpty(parsed.Chapters);
            Assert.Equal(parsed.Chapters[0].Content, chapter0.Content);
            Assert.True(chapter0.Content!.Length > 0);
        }
        finally
        {
            if (File.Exists(epubPath))
                File.Delete(epubPath);
        }
    }

    [Fact]
    public async Task ParseEpubAsync_MissingFile_Throws()
    {
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(
            () => _parser.ParseBookAsync("/nonexistent/path.epub"));
        Assert.Contains("nonexistent", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMetadataAsync_Epub_ReturnsTitle()
    {
        var epubPath = CreateMinimalEpubToTemp();
        try
        {
            var metadata = await _parser.GetMetadataAsync(epubPath);
            Assert.NotNull(metadata);
            Assert.Equal("Test Book", metadata.Title);
        }
        finally
        {
            if (File.Exists(epubPath))
                File.Delete(epubPath);
        }
    }
}

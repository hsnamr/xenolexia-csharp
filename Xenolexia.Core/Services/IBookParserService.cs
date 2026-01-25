using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Book parser service interface
/// </summary>
public interface IBookParserService
{
    Task<ParsedBook> ParseBookAsync(string filePath);
    Task<Chapter> GetChapterAsync(string filePath, int chapterIndex);
    Task<List<TableOfContentsItem>> GetTableOfContentsAsync(string filePath);
    Task<BookMetadata> GetMetadataAsync(string filePath);
}

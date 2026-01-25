namespace Xenolexia.Core.Models;

/// <summary>
/// Supported book formats
/// </summary>
public enum BookFormat
{
    Epub,
    Fb2,
    Mobi,
    Txt
}

/// <summary>
/// Book entity
/// </summary>
public class Book
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? CoverPath { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public BookFormat Format { get; set; }
    public long FileSize { get; set; } // in bytes
    public DateTime AddedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
    public LanguagePair LanguagePair { get; set; } = new();
    public ProficiencyLevel ProficiencyLevel { get; set; }
    public double WordDensity { get; set; } // 0.0 - 1.0

    // Reading Progress
    public double Progress { get; set; } // 0-100 percentage
    public string? CurrentLocation { get; set; } // CFI for EPUB, chapter index otherwise
    public int CurrentChapter { get; set; }
    public int TotalChapters { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int ReadingTimeMinutes { get; set; }

    // Download/Source info
    public string? SourceUrl { get; set; }
    public bool IsDownloaded { get; set; }
}

/// <summary>
/// Book metadata
/// </summary>
public class BookMetadata
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? CoverUrl { get; set; }
    public string? Language { get; set; }
    public string? Publisher { get; set; }
    public string? PublishDate { get; set; }
    public string? Isbn { get; set; }
    public List<string> Subjects { get; set; } = new();
}

/// <summary>
/// Chapter in a book
/// </summary>
public class Chapter
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Index { get; set; }
    public string Content { get; set; } = string.Empty; // HTML or plain text
    public int WordCount { get; set; }
    public string? Href { get; set; } // Path to the chapter file in EPUB
}

/// <summary>
/// Table of contents item
/// </summary>
public class TableOfContentsItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public int Level { get; set; }
    public List<TableOfContentsItem>? Children { get; set; }
}

/// <summary>
/// Parsed book structure
/// </summary>
public class ParsedBook
{
    public BookMetadata Metadata { get; set; } = new();
    public List<Chapter> Chapters { get; set; } = new();
    public List<TableOfContentsItem> TableOfContents { get; set; } = new();
    public int TotalWordCount { get; set; }
}

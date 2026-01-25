using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Export service interface for vocabulary export
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export vocabulary to specified format
    /// </summary>
    Task<ExportResult> ExportVocabularyAsync(
        List<VocabularyItem> vocabulary,
        ExportFormat format,
        ExportOptions? options = null);
}

/// <summary>
/// Export formats
/// </summary>
public enum ExportFormat
{
    Csv,
    Anki,
    Json
}

/// <summary>
/// Export options
/// </summary>
public class ExportOptions
{
    public bool IncludeContext { get; set; } = true;
    public bool IncludeSRSData { get; set; } = true;
    public bool IncludeBookInfo { get; set; } = true;
    public List<VocabularyStatus>? FilterByStatus { get; set; }
    public Language? FilterBySourceLanguage { get; set; }
    public Language? FilterByTargetLanguage { get; set; }
}

/// <summary>
/// Export result
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public int ItemCount { get; set; }
    public string? Error { get; set; }
}

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Export service implementation
/// Supports CSV, Anki TSV, and JSON formats
/// </summary>
public class ExportService : IExportService
{
    private readonly string _exportDirectory;

    public ExportService(string exportDirectory)
    {
        _exportDirectory = exportDirectory;
        Directory.CreateDirectory(_exportDirectory);
    }

    public async Task<ExportResult> ExportVocabularyAsync(
        List<VocabularyItem> vocabulary,
        ExportFormat format,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        try
        {
            // Apply filters
            var filteredVocabulary = ApplyFilters(vocabulary, options);

            if (filteredVocabulary.Count == 0)
            {
                return new ExportResult
                {
                    Success = false,
                    ItemCount = 0,
                    Error = "No vocabulary items match the export criteria"
                };
            }

            // Generate content
            string content = format switch
            {
                ExportFormat.Csv => GenerateCSV(filteredVocabulary, options),
                ExportFormat.Anki => GenerateAnki(filteredVocabulary, options),
                ExportFormat.Json => GenerateJSON(filteredVocabulary, options),
                _ => throw new NotSupportedException($"Format {format} is not supported")
            };

            // Generate filename
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm", CultureInfo.InvariantCulture);
            var extension = format switch
            {
                ExportFormat.Csv => "csv",
                ExportFormat.Anki => "txt",
                ExportFormat.Json => "json",
                _ => "txt"
            };

            var fileName = $"xenolexia_vocabulary_{timestamp}.{extension}";
            var filePath = Path.Combine(_exportDirectory, fileName);

            // Write file
            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);

            return new ExportResult
            {
                Success = true,
                FilePath = filePath,
                FileName = fileName,
                ItemCount = filteredVocabulary.Count
            };
        }
        catch (Exception ex)
        {
            return new ExportResult
            {
                Success = false,
                ItemCount = 0,
                Error = ex.Message
            };
        }
    }

    private List<VocabularyItem> ApplyFilters(List<VocabularyItem> vocabulary, ExportOptions options)
    {
        var filtered = vocabulary.AsEnumerable();

        if (options.FilterByStatus != null && options.FilterByStatus.Count > 0)
        {
            filtered = filtered.Where(v => options.FilterByStatus.Contains(v.Status));
        }

        if (options.FilterBySourceLanguage.HasValue)
        {
            filtered = filtered.Where(v => v.SourceLanguage == options.FilterBySourceLanguage.Value);
        }

        if (options.FilterByTargetLanguage.HasValue)
        {
            filtered = filtered.Where(v => v.TargetLanguage == options.FilterByTargetLanguage.Value);
        }

        return filtered.ToList();
    }

    private string GenerateCSV(List<VocabularyItem> vocabulary, ExportOptions options)
    {
        var headers = new List<string> { "source_word", "target_word", "source_language", "target_language" };

        if (options.IncludeContext)
            headers.Add("context_sentence");
        if (options.IncludeBookInfo)
            headers.Add("book_title");
        if (options.IncludeSRSData)
            headers.AddRange(new[] { "status", "review_count", "ease_factor", "interval", "added_at" });

        var rows = new List<string> { string.Join(",", headers) };

        foreach (var item in vocabulary)
        {
            var row = new List<string>
            {
                EscapeCSV(item.SourceWord),
                EscapeCSV(item.TargetWord),
                item.SourceLanguage.ToString().ToLowerInvariant(),
                item.TargetLanguage.ToString().ToLowerInvariant()
            };

            if (options.IncludeContext)
                row.Add(EscapeCSV(item.ContextSentence ?? ""));
            if (options.IncludeBookInfo)
                row.Add(EscapeCSV(item.BookTitle ?? ""));
            if (options.IncludeSRSData)
            {
                row.Add(item.Status.ToString().ToLowerInvariant());
                row.Add(item.ReviewCount.ToString());
                row.Add(item.EaseFactor.ToString("F2", CultureInfo.InvariantCulture));
                row.Add(item.Interval.ToString());
                row.Add(item.AddedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            rows.Add(string.Join(",", row));
        }

        return string.Join("\n", rows);
    }

    private string GenerateAnki(List<VocabularyItem> vocabulary, ExportOptions options)
    {
        var rows = new List<string>();

        foreach (var item in vocabulary)
        {
            // Front of card (foreign word)
            var front = item.TargetWord;

            // Back of card (original word + optional context)
            var back = new StringBuilder(item.SourceWord);
            
            if (options.IncludeContext && !string.IsNullOrEmpty(item.ContextSentence))
            {
                back.Append("<br><br><i>\"");
                back.Append(item.ContextSentence);
                back.Append("\"</i>");
            }

            if (options.IncludeBookInfo && !string.IsNullOrEmpty(item.BookTitle))
            {
                back.Append("<br><small>From: ");
                back.Append(item.BookTitle);
                back.Append("</small>");
            }

            // Tags (spec: lowercase codes e.g. en-fr, new)
            var tags = new List<string>
            {
                $"{item.SourceLanguage.ToString().ToLowerInvariant()}-{item.TargetLanguage.ToString().ToLowerInvariant()}",
                item.Status.ToString().ToLowerInvariant()
            };

            rows.Add($"{front}\t{back}\t{string.Join(" ", tags)}");
        }

        var header = "#separator:tab\n#html:true\n#tags column:3\n";
        return header + string.Join("\n", rows);
    }

    private string GenerateJSON(List<VocabularyItem> vocabulary, ExportOptions options)
    {
        var exportData = new
        {
            exportedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            itemCount = vocabulary.Count,
            format = "xenolexia-vocabulary-v1",
            items = vocabulary.Select(item => new
            {
                sourceWord = item.SourceWord,
                targetWord = item.TargetWord,
                sourceLanguage = item.SourceLanguage.ToString().ToLowerInvariant(),
                targetLanguage = item.TargetLanguage.ToString().ToLowerInvariant(),
                contextSentence = options.IncludeContext ? item.ContextSentence : null,
                bookId = options.IncludeBookInfo ? item.BookId : null,
                bookTitle = options.IncludeBookInfo ? item.BookTitle : null,
                status = options.IncludeSRSData ? item.Status.ToString().ToLowerInvariant() : null,
                reviewCount = options.IncludeSRSData ? item.ReviewCount : (int?)null,
                easeFactor = options.IncludeSRSData ? item.EaseFactor : (double?)null,
                interval = options.IncludeSRSData ? item.Interval : (int?)null,
                addedAt = options.IncludeSRSData ? item.AddedAt.ToString("O", CultureInfo.InvariantCulture) : null,
                lastReviewedAt = options.IncludeSRSData && item.LastReviewedAt.HasValue
                    ? item.LastReviewedAt.Value.ToString("O", CultureInfo.InvariantCulture)
                    : null
            })
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(exportData, jsonOptions);
    }

    private string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

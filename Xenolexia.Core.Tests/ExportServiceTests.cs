using Xenolexia.Core.Models;
using Xenolexia.Core.Services;
using Xunit;

namespace Xenolexia.Core.Tests;

public class ExportServiceTests
{
    private static string ExportDir => Path.Combine(Path.GetTempPath(), "xenolexia_export_test");

    [Fact]
    public void ExportVocabulary_CSV_EscapesQuotesAndCommas()
    {
        var service = new ExportService(ExportDir);
        var items = new List<VocabularyItem>
        {
            new VocabularyItem
            {
                Id = "1",
                SourceWord = "He said, \"Hello\"",
                TargetWord = "Dijo \"Hola\"",
                SourceLanguage = Language.En,
                TargetLanguage = Language.Es,
                ContextSentence = "She said, \"Yes.\"",
                AddedAt = new DateTime(2024, 1, 15),
                ReviewCount = 0,
                EaseFactor = 2.5,
                Interval = 0,
                Status = VocabularyStatus.New
            }
        };
        var result = service.ExportVocabularyAsync(items, ExportFormat.Csv, new ExportOptions
        {
            IncludeContext = true,
            IncludeBookInfo = true
        }).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        var content = File.ReadAllText(result.FilePath);
        Assert.Contains("source_word", content);
        Assert.Contains("\"He said, \"\"Hello\"\"\"", content);
    }

    [Fact]
    public void ExportVocabulary_Anki_HasCorrectHeaderAndFrontBack()
    {
        var service = new ExportService(ExportDir);
        var items = new List<VocabularyItem>
        {
            new VocabularyItem
            {
                Id = "1",
                SourceWord = "word",
                TargetWord = "palabra",
                SourceLanguage = Language.En,
                TargetLanguage = Language.Es,
                AddedAt = DateTime.UtcNow,
                ReviewCount = 0,
                EaseFactor = 2.5,
                Interval = 0,
                Status = VocabularyStatus.New
            }
        };
        var result = service.ExportVocabularyAsync(items, ExportFormat.Anki).GetAwaiter().GetResult();

        Assert.True(result.Success);
        var content = File.ReadAllText(result.FilePath!);
        Assert.StartsWith("#separator:tab", content);
        Assert.Contains("#html:true", content);
        Assert.Contains("palabra\tword\t", content);
    }

    [Fact]
    public void ExportVocabulary_JSON_HasWrapperAndFormat()
    {
        var service = new ExportService(ExportDir);
        var items = new List<VocabularyItem>
        {
            new VocabularyItem
            {
                Id = "1",
                SourceWord = "hello",
                TargetWord = "hola",
                SourceLanguage = Language.En,
                TargetLanguage = Language.Es,
                AddedAt = DateTime.UtcNow,
                ReviewCount = 0,
                EaseFactor = 2.5,
                Interval = 0,
                Status = VocabularyStatus.New
            }
        };
        var result = service.ExportVocabularyAsync(items, ExportFormat.Json).GetAwaiter().GetResult();

        Assert.True(result.Success);
        var content = File.ReadAllText(result.FilePath!);
        Assert.Contains("xenolexia-vocabulary-v1", content);
        Assert.Contains("exportedAt", content);
        Assert.Contains("itemCount", content);
        Assert.Contains("items", content);
    }
}

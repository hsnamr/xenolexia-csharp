using Xenolexia.Core.Models;
using Xenolexia.Core.Services;
using Xunit;

namespace Xenolexia.Core.Tests;

public class ReplacementEngineTests
{
    private static IOfflineDictionaryService CreateMockDictionary()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["the"] = "el/la",
            ["and"] = "y",
            ["of"] = "de",
            ["to"] = "a",
            ["in"] = "en",
            ["is"] = "es"
        };
        return new MockOfflineDictionaryService(dict);
    }

    [Fact]
    public async Task ProcessChapterAsync_ReplacesWords_WhenDictionaryHasEntries()
    {
        var dict = CreateMockDictionary();
        await dict.EnsureDictionaryLoadedAsync(Language.En, Language.Es);

        var engine = new ReplacementEngine(dict);
        var chapter = new Chapter
        {
            Id = "ch1",
            Title = "Test",
            Index = 0,
            Content = "The cat and the dog are in the house.",
            WordCount = 8,
            Href = ""
        };
        var pair = new LanguagePair { SourceLanguage = Language.En, TargetLanguage = Language.Es };

        var result = await engine.ProcessChapterAsync(chapter, pair);

        Assert.NotNull(result);
        Assert.True(result.ForeignWords.Count > 0, "Expected at least one word to be replaced");
        Assert.Contains(result.ForeignWords, fw => fw.OriginalWord.Equals("the", StringComparison.OrdinalIgnoreCase));
        Assert.NotEqual(chapter.Content, result.ProcessedContent);
    }

    [Fact]
    public async Task ProcessChapterAsync_ReplacesWords_WithGreekDictionary()
    {
        var dict = new OfflineDictionaryService(Path.Combine(Path.GetTempPath(), "xenolexia-test-" + Guid.NewGuid().ToString("N")));
        await dict.EnsureDictionaryLoadedAsync(Language.En, Language.El);

        var engine = new ReplacementEngine(dict);
        var chapter = new Chapter
        {
            Id = "ch1",
            Title = "Test",
            Index = 0,
            Content = "The cat and the dog are in the house. The book is good.",
            WordCount = 12,
            Href = ""
        };
        var pair = new LanguagePair { SourceLanguage = Language.En, TargetLanguage = Language.El };

        var result = await engine.ProcessChapterAsync(chapter, pair);

        Assert.NotNull(result);
        Assert.True(result.ForeignWords.Count > 0, "Expected Greek replacements (the, cat, and, dog, etc.)");
        Assert.NotEqual(chapter.Content, result.ProcessedContent);
    }

    [Fact]
    public async Task ProcessChapterAsync_ReturnsPlainText_WhenSourceEqualsTarget()
    {
        var dict = CreateMockDictionary();
        var engine = new ReplacementEngine(dict);
        var chapter = new Chapter
        {
            Id = "ch1",
            Title = "Test",
            Index = 0,
            Content = "The cat and the dog.",
            WordCount = 5,
            Href = ""
        };
        var pair = new LanguagePair { SourceLanguage = Language.En, TargetLanguage = Language.En };

        var result = await engine.ProcessChapterAsync(chapter, pair);

        Assert.NotNull(result);
        Assert.Empty(result.ForeignWords);
        Assert.NotNull(result.ProcessedContent);
    }

    private class MockOfflineDictionaryService : IOfflineDictionaryService
    {
        private readonly Dictionary<string, string> _dict;

        public MockOfflineDictionaryService(Dictionary<string, string> dict)
        {
            _dict = dict;
        }

        public Task EnsureDictionaryLoadedAsync(Language sourceLanguage, Language targetLanguage) => Task.CompletedTask;

        public WordEntry? Lookup(string word, Language sourceLanguage, Language targetLanguage)
        {
            if (!_dict.TryGetValue(word.ToLowerInvariant(), out var translation))
                return null;
            return new WordEntry
            {
                Id = $"{word}_{translation}",
                SourceWord = word.ToLowerInvariant(),
                TargetWord = translation,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                ProficiencyLevel = ProficiencyLevel.Beginner,
                FrequencyRank = 0,
                PartOfSpeech = PartOfSpeech.Other
            };
        }

        public Dictionary<string, WordEntry?> LookupBatch(IEnumerable<string> words, Language sourceLanguage, Language targetLanguage)
        {
            var result = new Dictionary<string, WordEntry?>(StringComparer.OrdinalIgnoreCase);
            foreach (var w in words.Distinct(StringComparer.OrdinalIgnoreCase))
                result[w] = Lookup(w, sourceLanguage, targetLanguage);
            return result;
        }

        public bool IsDictionaryAvailable(Language sourceLanguage, Language targetLanguage) => true;
    }
}

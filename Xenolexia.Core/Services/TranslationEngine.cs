using System.Text.RegularExpressions;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Translation engine that processes text and replaces words with translations.
/// Selects words by density (random sample); translates via ITranslationService; records positions in processed content.
/// </summary>
public class TranslationEngine
{
    private readonly ITranslationService _translationService;
    private readonly Dictionary<string, WordEntry> _wordCache;
    private readonly Random _random = new();

    public TranslationEngine(ITranslationService translationService)
    {
        _translationService = translationService;
        _wordCache = new Dictionary<string, WordEntry>();
    }

    /// <summary>
    /// Process chapter content and replace words based on proficiency level and density.
    /// ForeignWordData.StartIndex/EndIndex refer to positions in the returned ProcessedContent.
    /// </summary>
    public async Task<ProcessedChapter> ProcessChapterAsync(
        Chapter chapter,
        LanguagePair languagePair,
        ProficiencyLevel proficiencyLevel,
        double wordDensity)
    {
        var tokens = TokenizeWithPositions(chapter.Content);
        if (tokens.Count == 0)
        {
            return new ProcessedChapter
            {
                Id = chapter.Id,
                Title = chapter.Title,
                Index = chapter.Index,
                Content = chapter.Content,
                WordCount = chapter.WordCount,
                Href = chapter.Href,
                ForeignWords = new List<ForeignWordData>(),
                ProcessedContent = chapter.Content
            };
        }

        // Select tokens by density (random sample); clamp density to avoid too many API calls
        var takeCount = Math.Max(1, (int)(tokens.Count * Math.Clamp(wordDensity, 0.05, 0.5)));
        var selectedIndices = Enumerable.Range(0, tokens.Count)
            .OrderBy(_ => _random.Next())
            .Take(takeCount)
            .OrderBy(i => tokens[i].Start)
            .ToList();

        var foreignWords = new List<ForeignWordData>();
        var processedContent = new System.Text.StringBuilder();
        var lastEnd = 0;

        foreach (var idx in selectedIndices)
        {
            var (wordLower, original, start, end) = tokens[idx];
            // Copy plain text from lastEnd to start of this token
            processedContent.Append(chapter.Content, lastEnd, start - lastEnd);

            var translation = await GetTranslationAsync(wordLower, languagePair);
            if (translation != null)
            {
                var processedStart = processedContent.Length;
                processedContent.Append(translation.TargetWord);
                var processedEnd = processedContent.Length;
                foreignWords.Add(new ForeignWordData
                {
                    OriginalWord = original,
                    ForeignWord = translation.TargetWord,
                    StartIndex = processedStart,
                    EndIndex = processedEnd,
                    WordEntry = translation
                });
            }
            else
            {
                processedContent.Append(chapter.Content, start, end - start);
            }
            lastEnd = end;
        }

        processedContent.Append(chapter.Content, lastEnd, chapter.Content.Length - lastEnd);

        return new ProcessedChapter
        {
            Id = chapter.Id,
            Title = chapter.Title,
            Index = chapter.Index,
            Content = chapter.Content,
            WordCount = chapter.WordCount,
            Href = chapter.Href,
            ForeignWords = foreignWords,
            ProcessedContent = processedContent.ToString()
        };
    }

    /// <summary>
    /// Returns (wordLower, originalSubstring, start, end) for each word token.
    /// </summary>
    private static List<(string WordLower, string Original, int Start, int End)> TokenizeWithPositions(string text)
    {
        var list = new List<(string, string, int, int)>();
        var pattern = @"\b\w+\b";
        var matches = Regex.Matches(text, pattern);
        foreach (Match m in matches)
        {
            var original = m.Value;
            var lower = original.ToLowerInvariant();
            list.Add((lower, original, m.Index, m.Index + m.Length));
        }
        return list;
    }

    private async Task<WordEntry?> GetTranslationAsync(string word, LanguagePair languagePair)
    {
        var cacheKey = $"{word}_{languagePair.SourceLanguage}_{languagePair.TargetLanguage}";
        if (_wordCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var translation = await _translationService.TranslateAsync(word, languagePair.SourceLanguage, languagePair.TargetLanguage);
            var entry = new WordEntry
            {
                Id = Guid.NewGuid().ToString(),
                SourceWord = word,
                TargetWord = translation,
                SourceLanguage = languagePair.SourceLanguage,
                TargetLanguage = languagePair.TargetLanguage,
                ProficiencyLevel = ProficiencyLevel.Beginner,
                FrequencyRank = 0,
                PartOfSpeech = PartOfSpeech.Other
            };
            _wordCache[cacheKey] = entry;
            return entry;
        }
        catch
        {
            return null;
        }
    }
}

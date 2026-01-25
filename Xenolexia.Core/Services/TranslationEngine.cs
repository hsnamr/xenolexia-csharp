using System.Text.RegularExpressions;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Translation engine that processes text and replaces words with translations
/// </summary>
public class TranslationEngine
{
    private readonly ITranslationService _translationService;
    private readonly Dictionary<string, WordEntry> _wordCache;

    public TranslationEngine(ITranslationService translationService)
    {
        _translationService = translationService;
        _wordCache = new Dictionary<string, WordEntry>();
    }

    /// <summary>
    /// Process chapter content and replace words based on proficiency level
    /// </summary>
    public async Task<ProcessedChapter> ProcessChapterAsync(
        Chapter chapter,
        LanguagePair languagePair,
        ProficiencyLevel proficiencyLevel,
        double wordDensity)
    {
        var words = TokenizeText(chapter.Content);
        var wordsToReplace = SelectWordsToReplace(words, proficiencyLevel, wordDensity);
        
        var foreignWords = new List<ForeignWordData>();
        var processedContent = chapter.Content;
        int offset = 0;

        foreach (var word in wordsToReplace)
        {
            var translation = await GetTranslationAsync(word, languagePair);
            if (translation != null)
            {
                var startIndex = processedContent.IndexOf(word, offset);
                if (startIndex >= 0)
                {
                    var endIndex = startIndex + word.Length;
                    processedContent = processedContent.Substring(0, startIndex) + 
                                     translation.TargetWord + 
                                     processedContent.Substring(endIndex);
                    
                    offset = startIndex + translation.TargetWord.Length;
                    
                    foreignWords.Add(new ForeignWordData
                    {
                        OriginalWord = word,
                        ForeignWord = translation.TargetWord,
                        StartIndex = startIndex,
                        EndIndex = endIndex,
                        WordEntry = translation
                    });
                }
            }
        }

        return new ProcessedChapter
        {
            Id = chapter.Id,
            Title = chapter.Title,
            Index = chapter.Index,
            Content = chapter.Content,
            WordCount = chapter.WordCount,
            Href = chapter.Href,
            ForeignWords = foreignWords,
            ProcessedContent = processedContent
        };
    }

    private List<string> TokenizeText(string text)
    {
        // Simple tokenization - split by whitespace and punctuation
        var pattern = @"\b\w+\b";
        var matches = Regex.Matches(text, pattern);
        return matches.Select(m => m.Value.ToLowerInvariant()).ToList();
    }

    private List<string> SelectWordsToReplace(List<string> words, ProficiencyLevel level, double density)
    {
        // Filter by frequency rank based on proficiency level
        var frequencyRanges = level switch
        {
            ProficiencyLevel.Beginner => (1, 500),
            ProficiencyLevel.Intermediate => (501, 2000),
            ProficiencyLevel.Advanced => (2001, 5000),
            _ => (1, 500)
        };

        // Select words based on density
        var wordsToReplace = words
            .Where(w => _wordCache.ContainsKey(w) && 
                       _wordCache[w].FrequencyRank >= frequencyRanges.Item1 &&
                       _wordCache[w].FrequencyRank <= frequencyRanges.Item2)
            .Take((int)(words.Count * density))
            .ToList();

        return wordsToReplace;
    }

    private async Task<WordEntry?> GetTranslationAsync(string word, LanguagePair languagePair)
    {
        if (_wordCache.TryGetValue($"{word}_{languagePair.SourceLanguage}_{languagePair.TargetLanguage}", out var cached))
        {
            return cached;
        }

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
                ProficiencyLevel = ProficiencyLevel.Beginner, // Default
                FrequencyRank = 0, // Would need frequency data
                PartOfSpeech = PartOfSpeech.Other
            };

            _wordCache[$"{word}_{languagePair.SourceLanguage}_{languagePair.TargetLanguage}"] = entry;
            return entry;
        }
        catch
        {
            return null;
        }
    }
}

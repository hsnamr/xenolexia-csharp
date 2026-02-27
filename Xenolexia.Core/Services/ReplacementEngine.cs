using System.Text;
using System.Text.RegularExpressions;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Word replacement engine for language learning.
/// 1. Parses words in the ebook
/// 2. Selects 5-10 words per paragraph, max 25-35% of paragraph words
/// 3. Replaces with target language from offline dictionary
/// 4. Produces segments for hover popup (original word on hover)
/// </summary>
public class ReplacementEngine
{
    private readonly IOfflineDictionaryService _dictionary;
    private readonly Random _random = new();

    private const int WordsPerParagraphMin = 5;
    private const int WordsPerParagraphMax = 10;
    private const double MaxFractionPerParagraph = 0.35;

    public ReplacementEngine(IOfflineDictionaryService dictionary)
    {
        _dictionary = dictionary;
    }

    /// <summary>
    /// Process chapter: replace words per paragraph using offline dictionary.
    /// Returns processed plain text and foreign word metadata for hover popups.
    /// </summary>
    public async Task<ProcessedChapter> ProcessChapterAsync(Chapter chapter, LanguagePair languagePair)
    {
        if (languagePair.SourceLanguage == languagePair.TargetLanguage)
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
                ProcessedContent = ToPlainText(chapter.Content)
            };
        }

        await _dictionary.EnsureDictionaryLoadedAsync(languagePair.SourceLanguage, languagePair.TargetLanguage);

        var content = chapter.Content;
        var paragraphs = ExtractParagraphs(content);

        var foreignWords = new List<ForeignWordData>();
        var sb = new StringBuilder();

        for (var i = 0; i < paragraphs.Count; i++)
        {
            var offset = sb.Length;
            var (processed, fw) = ProcessParagraph(paragraphs[i], languagePair);
            sb.Append(processed);
            foreach (var w in fw)
            {
                w.StartIndex += offset;
                w.EndIndex += offset;
            }
            foreignWords.AddRange(fw);
            if (i < paragraphs.Count - 1)
                sb.Append("\n\n");
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
            ProcessedContent = sb.ToString()
        };
    }

    private static string ToPlainText(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        if (!content.TrimStart().StartsWith("<", StringComparison.Ordinal))
            return content;
        return HtmlToPlainText.ToPlainText(content);
    }

    private static List<string> ExtractParagraphs(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return new List<string>();

        if (content.TrimStart().StartsWith("<", StringComparison.Ordinal))
            return ExtractParagraphsFromHtml(content);

        return ExtractParagraphsFromPlainText(content);
    }

    private static List<string> ExtractParagraphsFromHtml(string html)
    {
        var blockRegex = new Regex(@"<\s*\/?(?:p|div|h[1-6])(?:\s[^>]*)?\s*>", RegexOptions.IgnoreCase);
        var boundaries = new List<int> { 0 };
        foreach (Match m in blockRegex.Matches(html))
            boundaries.Add(m.Index + m.Length);
        boundaries.Add(html.Length);

        var result = new List<string>();
        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var chunk = html.Substring(boundaries[i], boundaries[i + 1] - boundaries[i]);
            var text = Regex.Replace(Regex.Replace(chunk, @"<[^>]+>", " "), @"\s+", " ").Trim();
            if (!string.IsNullOrWhiteSpace(text))
                result.Add(text);
        }
        return result.Count > 0 ? result : new List<string> { Regex.Replace(Regex.Replace(html, @"<[^>]+>", " "), @"\s+", " ").Trim() };
    }

    private static List<string> ExtractParagraphsFromPlainText(string text)
    {
        var parts = Regex.Split(text, @"(\r?\n\s*\r?\n)");
        var current = new StringBuilder();
        var result = new List<string>();
        foreach (var part in parts)
        {
            if (Regex.IsMatch(part, @"^\r?\n\s*\r?\n$"))
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
            }
            else
            {
                current.Append(part);
            }
        }
        if (current.Length > 0)
            result.Add(current.ToString().Trim());
        return result;
    }

    private (string Processed, List<ForeignWordData> ForeignWords) ProcessParagraph(string paragraph, LanguagePair languagePair)
    {
        var tokens = Tokenize(paragraph);
        if (tokens.Count == 0)
            return (paragraph, new List<ForeignWordData>());

        var unique = tokens.Select(t => t.WordLower).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var lookup = _dictionary.LookupBatch(unique, languagePair.SourceLanguage, languagePair.TargetLanguage);

        var candidates = new List<(string Original, string WordLower, int Start, int End, WordEntry Entry)>();
        foreach (var (wordLower, original, start, end) in tokens)
        {
            if (lookup.TryGetValue(wordLower, out var entry) && entry != null)
                candidates.Add((original, wordLower, start, end, entry));
        }

        if (candidates.Count == 0)
            return (paragraph, new List<ForeignWordData>());

        var wordCount = tokens.Count;
        var maxByFraction = (int)Math.Floor(wordCount * MaxFractionPerParagraph);
        var targetCount = _random.Next(WordsPerParagraphMin, WordsPerParagraphMax + 1);
        var takeCount = Math.Min(Math.Min(targetCount, maxByFraction), candidates.Count);

        if (takeCount <= 0)
            return (paragraph, new List<ForeignWordData>());

        var selected = SelectDistributed(candidates, takeCount);
        var toReplace = selected.OrderByDescending(c => c.Start).ToList();

        var sb = new StringBuilder(paragraph);
        var foreignWords = new List<ForeignWordData>();

        foreach (var (original, _, start, end, entry) in toReplace)
        {
            var translation = PreserveCase(original, entry.TargetWord);
            sb.Remove(start, end - start);
            sb.Insert(start, translation);
            foreignWords.Add(new ForeignWordData
            {
                OriginalWord = original,
                ForeignWord = translation,
                StartIndex = start,
                EndIndex = start + translation.Length,
                WordEntry = entry
            });
        }

        return (sb.ToString(), foreignWords.OrderBy(f => f.StartIndex).ToList());
    }

    private static List<(string WordLower, string Original, int Start, int End)> Tokenize(string text)
    {
        var list = new List<(string, string, int, int)>();
        foreach (Match m in Regex.Matches(text, @"\b[a-zA-Z]{2,25}\b"))
        {
            var original = m.Value;
            list.Add((original.ToLowerInvariant(), original, m.Index, m.Index + m.Length));
        }
        return list;
    }

    private List<(string Original, string WordLower, int Start, int End, WordEntry Entry)> SelectDistributed(
        List<(string Original, string WordLower, int Start, int End, WordEntry Entry)> candidates,
        int count)
    {
        if (candidates.Count <= count)
            return candidates;

        var sorted = candidates.OrderBy(c => c.Start).ToList();
        var step = (double)sorted.Count / count;
        var indices = new HashSet<int>();
        for (var i = 0; i < count; i++)
        {
            var idx = Math.Min((int)(i * step + step * 0.3), sorted.Count - 1);
            indices.Add(idx);
        }
        return indices.Select(i => sorted[i]).ToList();
    }

    private static string PreserveCase(string original, string replacement)
    {
        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(replacement))
            return replacement;
        if (original == original.ToUpperInvariant() && original.Length > 1)
            return replacement.ToUpperInvariant();
        if (char.IsUpper(original[0]) && original[1..] == original[1..].ToLowerInvariant())
            return char.ToUpperInvariant(replacement[0]) + replacement[1..].ToLowerInvariant();
        return replacement.ToLowerInvariant();
    }
}

namespace Xenolexia.Core.Models;

/// <summary>
/// Parts of speech
/// </summary>
public enum PartOfSpeech
{
    Noun,
    Verb,
    Adjective,
    Adverb,
    Pronoun,
    Preposition,
    Conjunction,
    Interjection,
    Article,
    Other
}

/// <summary>
/// Word entry in the database
/// </summary>
public class WordEntry
{
    public string Id { get; set; } = string.Empty;
    public string SourceWord { get; set; } = string.Empty;
    public string TargetWord { get; set; } = string.Empty;
    public Language SourceLanguage { get; set; }
    public Language TargetLanguage { get; set; }
    public ProficiencyLevel ProficiencyLevel { get; set; }
    public int FrequencyRank { get; set; }
    public PartOfSpeech PartOfSpeech { get; set; }
    public List<string> Variants { get; set; } = new();
    public string? Pronunciation { get; set; }
}

/// <summary>
/// Vocabulary item saved by user
/// </summary>
public class VocabularyItem
{
    public string Id { get; set; } = string.Empty;
    public string SourceWord { get; set; } = string.Empty;
    public string TargetWord { get; set; } = string.Empty;
    public Language SourceLanguage { get; set; }
    public Language TargetLanguage { get; set; }
    public string? ContextSentence { get; set; }
    public string? BookId { get; set; }
    public string? BookTitle { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public int ReviewCount { get; set; }
    public double EaseFactor { get; set; } // SM-2 algorithm
    public int Interval { get; set; } // Days until next review
    public VocabularyStatus Status { get; set; }
}

/// <summary>
/// Vocabulary status
/// </summary>
public enum VocabularyStatus
{
    New,
    Learning,
    Review,
    Learned
}

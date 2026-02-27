using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Offline dictionary service for word replacement in the reader.
/// Dictionaries are stored in-app and automatically installed when the user selects a target language.
/// </summary>
public interface IOfflineDictionaryService
{
    /// <summary>
    /// Ensures the dictionary for the language pair is loaded.
    /// If not present, installs it (from bundled resources or download).
    /// </summary>
    Task EnsureDictionaryLoadedAsync(Language sourceLanguage, Language targetLanguage);

    /// <summary>
    /// Looks up a word in the offline dictionary. Returns null if not found.
    /// </summary>
    WordEntry? Lookup(string word, Language sourceLanguage, Language targetLanguage);

    /// <summary>
    /// Batch lookup for efficiency. Returns a map of word (lowercase) -> WordEntry or null.
    /// </summary>
    Dictionary<string, WordEntry?> LookupBatch(IEnumerable<string> words, Language sourceLanguage, Language targetLanguage);

    /// <summary>
    /// Returns true if the dictionary for this language pair is available.
    /// </summary>
    bool IsDictionaryAvailable(Language sourceLanguage, Language targetLanguage);
}

using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Storage service interface for database operations.
/// Aligned with Xenolexia Core Spec (docs/core-spec); uses same SQL schema and SM-2 as Obj-C.
/// </summary>
public interface IStorageService
{
    Task InitializeAsync();
    Task<List<Book>> GetAllBooksAsync();
    Task<Book?> GetBookByIdAsync(string bookId);
    Task AddBookAsync(Book book);
    Task UpdateBookAsync(Book book);
    Task DeleteBookAsync(string bookId);

    Task<List<VocabularyItem>> GetVocabularyItemsAsync();
    Task<VocabularyItem?> GetVocabularyItemByIdAsync(string wordId);
    Task AddVocabularyItemAsync(VocabularyItem item);
    Task UpdateVocabularyItemAsync(VocabularyItem item);
    Task DeleteVocabularyItemAsync(string wordId);
    /// <summary>Items due for review (SM-2: status != learned and last_reviewed_at + interval*86400000 &lt;= now).</summary>
    Task<List<VocabularyItem>> GetVocabularyDueForReviewAsync(int limit = 20);
    /// <summary>Record one SM-2 review step (quality 0-5). Uses shared SM-2 formula.</summary>
    Task RecordReviewAsync(string itemId, int quality);

    /// <summary>Load user preferences from the preferences table. Returns defaults for any missing keys.</summary>
    Task<UserPreferences> GetPreferencesAsync();

    /// <summary>Save user preferences to the preferences table (key/value pairs).</summary>
    Task SavePreferencesAsync(UserPreferences prefs);

    /// <summary>Start a reading session for the given book. Returns the new session id.</summary>
    Task<string> StartReadingSessionAsync(string bookId);

    /// <summary>End a reading session and record words revealed/saved.</summary>
    Task EndReadingSessionAsync(string sessionId, int wordsRevealed, int wordsSaved);

    /// <summary>Get the active (not yet ended) reading session for the book, if any.</summary>
    Task<ReadingSession?> GetActiveSessionForBookAsync(string bookId);

    /// <summary>Aggregate reading and vocabulary stats for the Statistics screen.</summary>
    Task<ReadingStats> GetReadingStatsAsync();
}

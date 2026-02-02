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
}

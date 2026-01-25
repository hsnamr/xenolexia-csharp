using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Storage service interface for database operations
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
}

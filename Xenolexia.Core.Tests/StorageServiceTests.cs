using Xenolexia.Core.Models;
using Xenolexia.Core.Services;
using Xunit;

namespace Xenolexia.Core.Tests;

public class StorageServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly StorageService _storage;

    public StorageServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"xenolexia_test_{Guid.NewGuid():N}.db");
        _storage = new StorageService(_dbPath);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task InitializeAsync_CreatesTables()
    {
        await _storage.InitializeAsync();
        var books = await _storage.GetAllBooksAsync();
        var vocab = await _storage.GetVocabularyItemsAsync();
        Assert.NotNull(books);
        Assert.NotNull(vocab);
        Assert.Empty(books);
        Assert.Empty(vocab);
    }

    [Fact]
    public async Task AddVocabularyItem_GetVocabularyDueForReview_RecordReview_SM2()
    {
        await _storage.InitializeAsync();
        var item = new VocabularyItem
        {
            Id = Guid.NewGuid().ToString(),
            SourceWord = "hello",
            TargetWord = "γεια",
            SourceLanguage = Language.En,
            TargetLanguage = Language.El,
            ContextSentence = null,
            BookId = null,
            BookTitle = null,
            AddedAt = DateTime.UtcNow,
            LastReviewedAt = null,
            ReviewCount = 0,
            EaseFactor = 2.5,
            Interval = 0,
            Status = VocabularyStatus.New
        };
        await _storage.AddVocabularyItemAsync(item);

        var due = await _storage.GetVocabularyDueForReviewAsync(20);
        Assert.Single(due);
        Assert.Equal(item.Id, due[0].Id);

        await _storage.RecordReviewAsync(item.Id, 4);
        var after = await _storage.GetVocabularyItemByIdAsync(item.Id);
        Assert.NotNull(after);
        Assert.Equal(1, after.ReviewCount);
        Assert.Equal(1, after.Interval);
        Assert.Equal(VocabularyStatus.Learning, after.Status);
    }

    [Fact]
    public async Task RecordReview_QualityFail_ResetsInterval()
    {
        await _storage.InitializeAsync();
        var item = new VocabularyItem
        {
            Id = Guid.NewGuid().ToString(),
            SourceWord = "test",
            TargetWord = "δοκιμή",
            SourceLanguage = Language.En,
            TargetLanguage = Language.El,
            AddedAt = DateTime.UtcNow,
            ReviewCount = 1,
            EaseFactor = 2.5,
            Interval = 6,
            Status = VocabularyStatus.Review
        };
        await _storage.AddVocabularyItemAsync(item);
        await _storage.RecordReviewAsync(item.Id, 2);

        var after = await _storage.GetVocabularyItemByIdAsync(item.Id);
        Assert.NotNull(after);
        Assert.Equal(0, after.Interval);
        Assert.Equal(VocabularyStatus.Learning, after.Status);
    }
}

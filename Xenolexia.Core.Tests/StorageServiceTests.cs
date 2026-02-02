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

    [Fact]
    public async Task GetPreferencesAsync_ReturnsDefaults_WhenEmpty()
    {
        await _storage.InitializeAsync();
        var prefs = await _storage.GetPreferencesAsync();
        Assert.NotNull(prefs);
        Assert.Equal(Language.En, prefs.DefaultSourceLanguage);
        Assert.Equal(Language.Es, prefs.DefaultTargetLanguage);
        Assert.Equal(ProficiencyLevel.Beginner, prefs.DefaultProficiencyLevel);
        Assert.Equal(0.3, prefs.DefaultWordDensity);
        Assert.False(prefs.HasCompletedOnboarding);
        Assert.Equal(30, prefs.DailyGoal);
        Assert.Equal(ReaderTheme.Light, prefs.ReaderSettings.Theme);
    }

    [Fact]
    public async Task SavePreferencesAsync_GetPreferencesAsync_RoundTrip()
    {
        await _storage.InitializeAsync();
        var prefs = new UserPreferences
        {
            DefaultSourceLanguage = Language.Fr,
            DefaultTargetLanguage = Language.De,
            DefaultProficiencyLevel = ProficiencyLevel.Advanced,
            DefaultWordDensity = 0.2,
            HasCompletedOnboarding = true,
            NotificationsEnabled = true,
            DailyGoal = 45,
            ReaderSettings = new ReaderSettings
            {
                Theme = ReaderTheme.Dark,
                FontSize = 18,
                LineHeight = 1.8
            }
        };
        await _storage.SavePreferencesAsync(prefs);

        var loaded = await _storage.GetPreferencesAsync();
        Assert.Equal(Language.Fr, loaded.DefaultSourceLanguage);
        Assert.Equal(Language.De, loaded.DefaultTargetLanguage);
        Assert.Equal(ProficiencyLevel.Advanced, loaded.DefaultProficiencyLevel);
        Assert.Equal(0.2, loaded.DefaultWordDensity);
        Assert.True(loaded.HasCompletedOnboarding);
        Assert.True(loaded.NotificationsEnabled);
        Assert.Equal(45, loaded.DailyGoal);
        Assert.Equal(ReaderTheme.Dark, loaded.ReaderSettings.Theme);
        Assert.Equal(18, loaded.ReaderSettings.FontSize);
        Assert.Equal(1.8, loaded.ReaderSettings.LineHeight);
    }

    [Fact]
    public async Task StartReadingSessionAsync_EndReadingSessionAsync_GetActiveSession()
    {
        await _storage.InitializeAsync();
        var bookId = Guid.NewGuid().ToString();
        await _storage.AddBookAsync(new Book
        {
            Id = bookId,
            Title = "Test",
            Author = "Author",
            FilePath = "/tmp/test.epub",
            Format = BookFormat.Epub,
            AddedAt = DateTime.UtcNow,
            LanguagePair = new LanguagePair { SourceLanguage = Language.En, TargetLanguage = Language.Es },
            ProficiencyLevel = ProficiencyLevel.Beginner,
            WordDensity = 0.3
        });

        var sessionId = await _storage.StartReadingSessionAsync(bookId);
        Assert.NotNull(sessionId);
        Assert.NotEmpty(sessionId);

        var active = await _storage.GetActiveSessionForBookAsync(bookId);
        Assert.NotNull(active);
        Assert.Equal(sessionId, active.Id);
        Assert.Equal(bookId, active.BookId);
        Assert.Null(active.EndedAt);
        Assert.Equal(0, active.WordsRevealed);
        Assert.Equal(0, active.WordsSaved);

        await _storage.EndReadingSessionAsync(sessionId, 5, 2);

        var activeAfter = await _storage.GetActiveSessionForBookAsync(bookId);
        Assert.Null(activeAfter);
    }
}

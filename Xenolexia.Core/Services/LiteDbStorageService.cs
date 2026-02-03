using LiteDB;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// Storage service backed by LiteDB (MIT, embedded NoSQL). Single file, document collections.
/// Replaces SQLite to avoid schema/constraint issues; uses off-the-shelf FOSS library.
/// </summary>
public class LiteDbStorageService : IStorageService
{
    private readonly string _databasePath;
    private readonly object _lock = new();
    private LiteDatabase? _db;

    public LiteDbStorageService(string databasePath)
    {
        _databasePath = databasePath;
    }

    public Task InitializeAsync()
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                _db = new LiteDatabase(new ConnectionString
                {
                    Filename = _databasePath,
                    Connection = ConnectionType.Direct
                });
                var books = _db.GetCollection<Book>("books");
                books.EnsureIndex(x => x.Id, true);
                books.EnsureIndex(x => x.FilePath);
                var vocab = _db.GetCollection<VocabularyItem>("vocabulary");
                vocab.EnsureIndex(x => x.Id, true);
                vocab.EnsureIndex(x => x.Status);
                vocab.EnsureIndex(x => x.LastReviewedAt);
                var sessions = _db.GetCollection<ReadingSession>("reading_sessions");
                sessions.EnsureIndex(x => x.Id, true);
                sessions.EnsureIndex(x => x.BookId);
                var prefs = _db.GetCollection<PreferencesDoc>("preferences");
                prefs.EnsureIndex(x => x.Id, true);
            }
        });
    }

    private ILiteDatabase Db()
    {
        if (_db == null) throw new InvalidOperationException("Database not initialized");
        return _db;
    }

    public Task<List<Book>> GetAllBooksAsync()
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                return Db().GetCollection<Book>("books")
                    .FindAll()
                    .OrderByDescending(b => b.LastReadAt ?? DateTime.MinValue)
                    .ThenByDescending(b => b.AddedAt)
                    .ToList();
            }
        });
    }

    public Task<Book?> GetBookByIdAsync(string bookId)
    {
        return Task.Run(() =>
        {
            lock (_lock)
                return (Book?)Db().GetCollection<Book>("books").FindById(new BsonValue(bookId));
        });
    }

    public Task<Book?> GetBookByFilePathAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return Task.FromResult<Book?>(null);
        return Task.Run<Book?>(() =>
        {
            lock (_lock)
                return Db().GetCollection<Book>("books").FindOne(x => x.FilePath == filePath);
        });
    }

    public Task AddBookAsync(Book book)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(book.Id))
                book.Id = Guid.NewGuid().ToString("N")[..16];
            if (book.AddedAt == default)
                book.AddedAt = DateTime.UtcNow;
            book.WordDensity = Math.Max(0.1, Math.Min(1.0, book.WordDensity));
            lock (_lock)
                Db().GetCollection<Book>("books").Insert(book);
        });
    }

    public Task UpdateBookAsync(Book book)
    {
        return Task.Run(() =>
        {
            lock (_lock)
                Db().GetCollection<Book>("books").Update(book);
        });
    }

    public Task DeleteBookAsync(string bookId)
    {
        return Task.Run(() =>
        {
            lock (_lock)
                Db().GetCollection<Book>("books").Delete(bookId);
        });
    }

    public Task<List<VocabularyItem>> GetVocabularyItemsAsync()
    {
        return Task.Run(() =>
        {
            lock (_lock)
                return Db().GetCollection<VocabularyItem>("vocabulary")
                    .FindAll()
                    .OrderByDescending(v => v.AddedAt)
                    .ToList();
            });
    }

    public Task<VocabularyItem?> GetVocabularyItemByIdAsync(string wordId)
    {
        return Task.Run(() =>
        {
            lock (_lock)
                return (VocabularyItem?)Db().GetCollection<VocabularyItem>("vocabulary").FindById(new BsonValue(wordId));
        });
    }

    public Task AddVocabularyItemAsync(VocabularyItem item)
    {
        return Task.Run(() =>
        {
            if (item.AddedAt == default)
                item.AddedAt = DateTime.UtcNow;
            lock (_lock)
                Db().GetCollection<VocabularyItem>("vocabulary").Insert(item);
        });
    }

    public Task UpdateVocabularyItemAsync(VocabularyItem item)
    {
        return Task.Run(() =>
        {
            lock (_lock)
                Db().GetCollection<VocabularyItem>("vocabulary").Update(item);
        });
    }

    public Task DeleteVocabularyItemAsync(string wordId)
    {
        return Task.Run(() =>
        {
            lock (_lock)
                Db().GetCollection<VocabularyItem>("vocabulary").Delete(wordId);
        });
    }

    public Task<List<VocabularyItem>> GetVocabularyDueForReviewAsync(int limit = 20)
    {
        return Task.Run(() =>
        {
            var now = DateTime.UtcNow;
            lock (_lock)
            {
                return Db().GetCollection<VocabularyItem>("vocabulary")
                    .Find(v => v.Status != VocabularyStatus.Learned &&
                        (v.LastReviewedAt == null || v.LastReviewedAt.Value.AddDays(v.Interval) <= now))
                    .OrderBy(v => v.LastReviewedAt ?? DateTime.MinValue)
                    .Take(limit)
                    .ToList();
            }
        });
    }

    /// <summary>SM-2 implemented in C# (same formula as xenolexia-shared-c/sm2.c). xenolexia-shared-c remains for xenolexia-objc.</summary>
    public async Task RecordReviewAsync(string itemId, int quality)
    {
        var item = await GetVocabularyItemByIdAsync(itemId);
        if (item == null) return;
        double ef = item.EaseFactor;
        int iv = item.Interval;
        int rc = item.ReviewCount + 1;
        var newStatus = item.Status;
        if (quality >= 3)
        {
            if (iv == 0) iv = 1;
            else if (iv == 1) iv = 6;
            else iv = (int)Math.Round(iv * ef);
            ef = Math.Max(1.3, ef + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
            if (rc >= 5 && quality >= 4) newStatus = VocabularyStatus.Learned;
            else if (rc >= 2) newStatus = VocabularyStatus.Review;
            else newStatus = VocabularyStatus.Learning;
        }
        else { iv = 0; newStatus = VocabularyStatus.Learning; }
        item.Interval = iv;
        item.EaseFactor = ef;
        item.ReviewCount = rc;
        item.Status = newStatus;
        item.LastReviewedAt = DateTime.UtcNow;
        await UpdateVocabularyItemAsync(item);
    }

    public Task<UserPreferences> GetPreferencesAsync()
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var doc = Db().GetCollection<PreferencesDoc>("preferences").FindById("default");
                return doc?.Prefs ?? new UserPreferences();
            }
        });
    }

    public Task SavePreferencesAsync(UserPreferences prefs)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var col = Db().GetCollection<PreferencesDoc>("preferences");
                col.Upsert(new PreferencesDoc { Id = "default", Prefs = prefs });
            }
        });
    }

    public Task<string> StartReadingSessionAsync(string bookId)
    {
        return Task.Run(() =>
        {
            var session = new ReadingSession
            {
                Id = Guid.NewGuid().ToString("N"),
                BookId = bookId,
                StartedAt = DateTime.UtcNow,
                EndedAt = null,
                PagesRead = 0,
                WordsRevealed = 0,
                WordsSaved = 0
            };
            lock (_lock)
                Db().GetCollection<ReadingSession>("reading_sessions").Insert(session);
            return session.Id;
        });
    }

    public Task EndReadingSessionAsync(string sessionId, int wordsRevealed, int wordsSaved)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var col = Db().GetCollection<ReadingSession>("reading_sessions");
                var session = col.FindById(sessionId);
                if (session == null) return;
                session.EndedAt = DateTime.UtcNow;
                session.WordsRevealed = wordsRevealed;
                session.WordsSaved = wordsSaved;
                session.Duration = session.EndedAt.HasValue
                    ? (int)(session.EndedAt.Value - session.StartedAt).TotalSeconds
                    : 0;
                col.Update(session);
            }
        });
    }

    public Task<ReadingSession?> GetActiveSessionForBookAsync(string bookId)
    {
        return Task.Run<ReadingSession?>(() =>
        {
            lock (_lock)
                return Db().GetCollection<ReadingSession>("reading_sessions")
                    .FindOne(x => x.BookId == bookId && x.EndedAt == null);
        });
    }

    public Task<ReadingStats> GetReadingStatsAsync()
    {
        return Task.Run(() =>
        {
            var todayUtc = DateTime.UtcNow.Date;
            var bookIds = new HashSet<string>();
            var totalSeconds = 0;
            var sessionCount = 0;
            var wordsRevealedToday = 0;
            var wordsSavedToday = 0;
            var sessionDates = new List<DateTime>();

            lock (_lock)
            {
                var sessions = Db().GetCollection<ReadingSession>("reading_sessions")
                    .Find(x => x.EndedAt != null);
                foreach (var s in sessions)
                {
                    bookIds.Add(s.BookId);
                    totalSeconds += s.Duration;
                    sessionCount++;
                    var endedDate = s.EndedAt?.Date ?? todayUtc;
                    sessionDates.Add(endedDate);
                    if (endedDate == todayUtc)
                    {
                        wordsRevealedToday += s.WordsRevealed;
                        wordsSavedToday += s.WordsSaved;
                    }
                }

                var totalWordsLearned = Db().GetCollection<VocabularyItem>("vocabulary")
                    .Count(x => x.Status == VocabularyStatus.Learned);

                var distinctDates = sessionDates.Distinct().OrderByDescending(d => d).ToList();
                var currentStreak = ComputeCurrentStreak(distinctDates, todayUtc);
                var longestStreak = ComputeLongestStreak(distinctDates);
                var avgSessionDuration = sessionCount > 0 ? (double)totalSeconds / sessionCount : 0;

                return new ReadingStats
                {
                    TotalBooksRead = bookIds.Count,
                    TotalReadingTime = totalSeconds,
                    TotalWordsLearned = totalWordsLearned,
                    CurrentStreak = currentStreak,
                    LongestStreak = longestStreak,
                    AverageSessionDuration = avgSessionDuration,
                    WordsRevealedToday = wordsRevealedToday,
                    WordsSavedToday = wordsSavedToday
                };
            }
        });
    }

    private static int ComputeCurrentStreak(List<DateTime> distinctDatesDesc, DateTime todayUtc)
    {
        if (distinctDatesDesc.Count == 0) return 0;
        var set = distinctDatesDesc.ToHashSet();
        var mostRecent = distinctDatesDesc[0];
        var streak = 0;
        var d = mostRecent;
        while (set.Contains(d))
        {
            streak++;
            d = d.AddDays(-1);
        }
        return streak;
    }

    private static int ComputeLongestStreak(List<DateTime> distinctDates)
    {
        if (distinctDates.Count == 0) return 0;
        var sorted = distinctDates.Distinct().OrderBy(d => d).ToList();
        var maxStreak = 1;
        var current = 1;
        for (var i = 1; i < sorted.Count; i++)
        {
            var diff = (sorted[i] - sorted[i - 1]).Days;
            if (diff == 1) current++;
            else current = 1;
            if (current > maxStreak) maxStreak = current;
        }
        return maxStreak;
    }

    /// <summary>Wrapper so preferences collection has a document Id for LiteDB.</summary>
    private class PreferencesDoc
    {
        public string Id { get; set; } = "default";
        public UserPreferences Prefs { get; set; } = new();
    }
}

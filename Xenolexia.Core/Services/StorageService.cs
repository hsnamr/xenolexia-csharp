using System.Data.SQLite;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// SQLite-based storage service aligned with Xenolexia Core Spec (docs/core-spec).
/// Uses snake_case table/column names and INTEGER timestamps (ms since epoch).
/// SM-2 formula matches shared C library (xenolexia-shared-c/sm2.c) and spec 04-algorithms.
/// </summary>
public class StorageService : IStorageService
{
    private readonly string _databasePath;
    private SQLiteConnection? _connection;

    public StorageService(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task InitializeAsync()
    {
        _connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;");
        await _connection.OpenAsync();
        await CreateTablesAsync();
    }

    private async Task CreateTablesAsync()
    {
        if (_connection == null) return;

        // Spec 02-sql-schema: books, vocabulary, reading_sessions, preferences, word_list
        var createBooks = @"
            CREATE TABLE IF NOT EXISTS books (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                author TEXT,
                cover_path TEXT,
                file_path TEXT NOT NULL,
                format TEXT NOT NULL,
                file_size INTEGER,
                added_at INTEGER NOT NULL,
                last_read_at INTEGER,
                progress REAL DEFAULT 0,
                current_location TEXT,
                source_lang TEXT NOT NULL,
                target_lang TEXT NOT NULL,
                proficiency TEXT NOT NULL,
                density REAL DEFAULT 0.3,
                current_chapter INTEGER,
                total_chapters INTEGER,
                current_page INTEGER,
                total_pages INTEGER,
                reading_time_minutes INTEGER,
                source_url TEXT,
                is_downloaded INTEGER
            )";
        var createVocabulary = @"
            CREATE TABLE IF NOT EXISTS vocabulary (
                id TEXT PRIMARY KEY,
                source_word TEXT NOT NULL,
                target_word TEXT NOT NULL,
                source_lang TEXT NOT NULL,
                target_lang TEXT NOT NULL,
                context_sentence TEXT,
                book_id TEXT,
                book_title TEXT,
                added_at INTEGER NOT NULL,
                last_reviewed_at INTEGER,
                review_count INTEGER DEFAULT 0,
                ease_factor REAL DEFAULT 2.5,
                interval INTEGER DEFAULT 0,
                status TEXT DEFAULT 'new',
                FOREIGN KEY (book_id) REFERENCES books(id) ON DELETE SET NULL
            )";
        var createSessions = @"
            CREATE TABLE IF NOT EXISTS reading_sessions (
                id TEXT PRIMARY KEY,
                book_id TEXT NOT NULL,
                started_at INTEGER NOT NULL,
                ended_at INTEGER,
                pages_read INTEGER DEFAULT 0,
                words_revealed INTEGER DEFAULT 0,
                words_saved INTEGER DEFAULT 0,
                FOREIGN KEY (book_id) REFERENCES books(id) ON DELETE CASCADE
            )";
        var createPreferences = @"
            CREATE TABLE IF NOT EXISTS preferences (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            )";
        var createWordList = @"
            CREATE TABLE IF NOT EXISTS word_list (
                id TEXT PRIMARY KEY,
                source_word TEXT NOT NULL,
                target_word TEXT NOT NULL,
                source_lang TEXT NOT NULL,
                target_lang TEXT NOT NULL,
                proficiency TEXT NOT NULL,
                frequency_rank INTEGER,
                part_of_speech TEXT,
                variants TEXT,
                pronunciation TEXT
            )";
        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_vocabulary_book ON vocabulary(book_id);
            CREATE INDEX IF NOT EXISTS idx_vocabulary_status ON vocabulary(status);
            CREATE INDEX IF NOT EXISTS idx_vocabulary_source ON vocabulary(source_word);
            CREATE INDEX IF NOT EXISTS idx_reading_sessions_book ON reading_sessions(book_id);
        ";

        foreach (var sql in new[] { createBooks, createVocabulary, createSessions, createPreferences, createWordList, createIndexes })
        {
            using var cmd = new SQLiteCommand(sql, _connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static long ToEpochMs(DateTime d) => new DateTimeOffset(d).ToUnixTimeMilliseconds();
    private static DateTime FromEpochMs(long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
    private static string LangToSpec(Language l) => l.ToString().ToLowerInvariant();
    private static string ProfToSpec(ProficiencyLevel p) => p.ToString().ToLowerInvariant();
    private static string StatusToSpec(VocabularyStatus s) => s.ToString().ToLowerInvariant();
    private static Language SpecToLang(string s) => Enum.Parse<Language>(s, true);
    private static ProficiencyLevel SpecToProf(string s) => Enum.Parse<ProficiencyLevel>(s, true);
    private static VocabularyStatus SpecToStatus(string s) => Enum.Parse<VocabularyStatus>(s, true);
    private static string FormatToSpec(BookFormat f) => f.ToString().ToLowerInvariant();
    private static BookFormat SpecToFormat(string s) => Enum.Parse<BookFormat>(s, true);

    public async Task<List<Book>> GetAllBooksAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var books = new List<Book>();
        var query = "SELECT * FROM books ORDER BY last_read_at DESC, added_at DESC";
        using var cmd = new SQLiteCommand(query, _connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            books.Add(MapBookFromReader((SQLiteDataReader)reader));
        return books;
    }

    public async Task<Book?> GetBookByIdAsync(string bookId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var query = "SELECT * FROM books WHERE id = @id";
        using var cmd = new SQLiteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@id", bookId);
        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapBookFromReader((SQLiteDataReader)reader) : null;
    }

    public async Task AddBookAsync(Book book)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var query = @"INSERT INTO books (id, title, author, cover_path, file_path, format, file_size, added_at, last_read_at,
            source_lang, target_lang, proficiency, density, progress, current_location,
            current_chapter, total_chapters, current_page, total_pages, reading_time_minutes, source_url, is_downloaded)
            VALUES (@id, @title, @author, @cover_path, @file_path, @format, @file_size, @added_at, @last_read_at,
            @source_lang, @target_lang, @proficiency, @density, @progress, @current_location,
            @current_chapter, @total_chapters, @current_page, @total_pages, @reading_time_minutes, @source_url, @is_downloaded)";
        using var cmd = new SQLiteCommand(query, _connection);
        AddBookParams(cmd, book);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateBookAsync(Book book)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var query = @"UPDATE books SET title=@title, author=@author, cover_path=@cover_path, file_path=@file_path,
            format=@format, file_size=@file_size, added_at=@added_at, last_read_at=@last_read_at, source_lang=@source_lang, target_lang=@target_lang,
            proficiency=@proficiency, density=@density, progress=@progress, current_location=@current_location,
            current_chapter=@current_chapter, total_chapters=@total_chapters, current_page=@current_page,
            total_pages=@total_pages, reading_time_minutes=@reading_time_minutes, source_url=@source_url, is_downloaded=@is_downloaded
            WHERE id=@id";
        using var cmd = new SQLiteCommand(query, _connection);
        AddBookParams(cmd, book);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteBookAsync(string bookId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        using var cmd = new SQLiteCommand("DELETE FROM books WHERE id = @id", _connection);
        cmd.Parameters.AddWithValue("@id", bookId);
        await cmd.ExecuteNonQueryAsync();
    }

    private void AddBookParams(SQLiteCommand cmd, Book book)
    {
        cmd.Parameters.AddWithValue("@id", book.Id);
        cmd.Parameters.AddWithValue("@title", book.Title);
        cmd.Parameters.AddWithValue("@author", book.Author ?? "");
        cmd.Parameters.AddWithValue("@cover_path", book.CoverPath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@file_path", book.FilePath);
        cmd.Parameters.AddWithValue("@format", FormatToSpec(book.Format));
        cmd.Parameters.AddWithValue("@file_size", book.FileSize);
        cmd.Parameters.AddWithValue("@added_at", ToEpochMs(book.AddedAt));
        cmd.Parameters.AddWithValue("@last_read_at", book.LastReadAt.HasValue ? ToEpochMs(book.LastReadAt.Value) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@source_lang", LangToSpec(book.LanguagePair.SourceLanguage));
        cmd.Parameters.AddWithValue("@target_lang", LangToSpec(book.LanguagePair.TargetLanguage));
        cmd.Parameters.AddWithValue("@proficiency", ProfToSpec(book.ProficiencyLevel));
        cmd.Parameters.AddWithValue("@density", book.WordDensity);
        cmd.Parameters.AddWithValue("@progress", book.Progress);
        cmd.Parameters.AddWithValue("@current_location", book.CurrentLocation ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@current_chapter", book.CurrentChapter);
        cmd.Parameters.AddWithValue("@total_chapters", book.TotalChapters);
        cmd.Parameters.AddWithValue("@current_page", book.CurrentPage);
        cmd.Parameters.AddWithValue("@total_pages", book.TotalPages);
        cmd.Parameters.AddWithValue("@reading_time_minutes", book.ReadingTimeMinutes);
        cmd.Parameters.AddWithValue("@source_url", book.SourceUrl ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@is_downloaded", book.IsDownloaded ? 1 : 0);
    }

    private Book MapBookFromReader(SQLiteDataReader r)
    {
        return new Book
        {
            Id = r.GetString(0),
            Title = r.GetString(1),
            Author = r.IsDBNull(2) ? "" : r.GetString(2),
            CoverPath = r.IsDBNull(3) ? null : r.GetString(3),
            FilePath = r.GetString(4),
            Format = SpecToFormat(r.GetString(5)),
            FileSize = r.IsDBNull(6) ? 0 : r.GetInt64(6),
            AddedAt = FromEpochMs(r.GetInt64(7)),
            LastReadAt = r.IsDBNull(8) ? null : FromEpochMs(r.GetInt64(8)),
            LanguagePair = new LanguagePair { SourceLanguage = SpecToLang(r.GetString(10)), TargetLanguage = SpecToLang(r.GetString(11)) },
            ProficiencyLevel = SpecToProf(r.GetString(12)),
            WordDensity = r.GetDouble(13),
            Progress = r.GetDouble(14),
            CurrentLocation = r.IsDBNull(15) ? null : r.GetString(15),
            CurrentChapter = r.IsDBNull(16) ? 0 : r.GetInt32(16),
            TotalChapters = r.IsDBNull(17) ? 0 : r.GetInt32(17),
            CurrentPage = r.IsDBNull(18) ? 0 : r.GetInt32(18),
            TotalPages = r.IsDBNull(19) ? 0 : r.GetInt32(19),
            ReadingTimeMinutes = r.IsDBNull(20) ? 0 : r.GetInt32(20),
            SourceUrl = r.IsDBNull(21) ? null : r.GetString(21),
            IsDownloaded = r.IsDBNull(22) ? false : r.GetInt32(22) != 0
        };
    }

    public async Task<List<VocabularyItem>> GetVocabularyItemsAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var list = new List<VocabularyItem>();
        using var cmd = new SQLiteCommand("SELECT * FROM vocabulary ORDER BY added_at DESC", _connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapVocabularyFromReader((SQLiteDataReader)reader));
        return list;
    }

    public async Task<VocabularyItem?> GetVocabularyItemByIdAsync(string wordId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        using var cmd = new SQLiteCommand("SELECT * FROM vocabulary WHERE id = @id", _connection);
        cmd.Parameters.AddWithValue("@id", wordId);
        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapVocabularyFromReader((SQLiteDataReader)reader) : null;
    }

    public async Task AddVocabularyItemAsync(VocabularyItem item)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var query = @"INSERT INTO vocabulary (id, source_word, target_word, source_lang, target_lang,
            context_sentence, book_id, book_title, added_at, status)
            VALUES (@id, @source_word, @target_word, @source_lang, @target_lang,
            @context_sentence, @book_id, @book_title, @added_at, 'new')";
        using var cmd = new SQLiteCommand(query, _connection);
        AddVocabularyParams(cmd, item);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateVocabularyItemAsync(VocabularyItem item)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var query = @"UPDATE vocabulary SET source_word=@source_word, target_word=@target_word,
            source_lang=@source_lang, target_lang=@target_lang, context_sentence=@context_sentence,
            book_id=@book_id, book_title=@book_title, added_at=@added_at, last_reviewed_at=@last_reviewed_at,
            review_count=@review_count, ease_factor=@ease_factor, interval=@interval, status=@status WHERE id=@id";
        using var cmd = new SQLiteCommand(query, _connection);
        AddVocabularyParams(cmd, item);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteVocabularyItemAsync(string wordId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        using var cmd = new SQLiteCommand("DELETE FROM vocabulary WHERE id = @id", _connection);
        cmd.Parameters.AddWithValue("@id", wordId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<VocabularyItem>> GetVocabularyDueForReviewAsync(int limit = 20)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var now = ToEpochMs(DateTime.UtcNow);
        var query = @"SELECT * FROM vocabulary
            WHERE status != 'learned'
            AND (last_reviewed_at IS NULL OR (last_reviewed_at + interval * 86400000) <= @now)
            ORDER BY last_reviewed_at ASC LIMIT @limit";
        using var cmd = new SQLiteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@now", now);
        cmd.Parameters.AddWithValue("@limit", limit);
        var list = new List<VocabularyItem>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(MapVocabularyFromReader((SQLiteDataReader)reader));
        return list;
    }

    public async Task RecordReviewAsync(string itemId, int quality)
    {
        var item = await GetVocabularyItemByIdAsync(itemId);
        if (item == null) return;
        double ef = item.EaseFactor;
        int iv = item.Interval;
        int rc = item.ReviewCount; // native increments and returns new value
        int statusInt = (int)item.Status;
        if (Sm2Native.TryStep(quality, ref ef, ref iv, ref rc, ref statusInt))
        {
            item.EaseFactor = ef;
            item.Interval = iv;
            item.ReviewCount = rc;
            item.Status = (VocabularyStatus)statusInt;
        }
        else
        {
            // Fallback: in-C# SM-2 (same formula as xenolexia-shared-c/sm2.c)
            rc = item.ReviewCount + 1;
            VocabularyStatus newStatus = item.Status;
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
        }
        item.LastReviewedAt = DateTime.UtcNow;
        await UpdateVocabularyItemAsync(item);
    }

    private void AddVocabularyParams(SQLiteCommand cmd, VocabularyItem item)
    {
        cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.Parameters.AddWithValue("@source_word", item.SourceWord);
        cmd.Parameters.AddWithValue("@target_word", item.TargetWord);
        cmd.Parameters.AddWithValue("@source_lang", LangToSpec(item.SourceLanguage));
        cmd.Parameters.AddWithValue("@target_lang", LangToSpec(item.TargetLanguage));
        cmd.Parameters.AddWithValue("@context_sentence", item.ContextSentence ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@book_id", item.BookId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@book_title", item.BookTitle ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@added_at", ToEpochMs(item.AddedAt));
        cmd.Parameters.AddWithValue("@last_reviewed_at", item.LastReviewedAt.HasValue ? ToEpochMs(item.LastReviewedAt.Value) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@review_count", item.ReviewCount);
        cmd.Parameters.AddWithValue("@ease_factor", item.EaseFactor);
        cmd.Parameters.AddWithValue("@interval", item.Interval);
        cmd.Parameters.AddWithValue("@status", StatusToSpec(item.Status));
    }

    private VocabularyItem MapVocabularyFromReader(SQLiteDataReader r)
    {
        return new VocabularyItem
        {
            Id = r.GetString(0),
            SourceWord = r.GetString(1),
            TargetWord = r.GetString(2),
            SourceLanguage = SpecToLang(r.GetString(3)),
            TargetLanguage = SpecToLang(r.GetString(4)),
            ContextSentence = r.IsDBNull(5) ? null : r.GetString(5),
            BookId = r.IsDBNull(6) ? null : r.GetString(6),
            BookTitle = r.IsDBNull(7) ? null : r.GetString(7),
            AddedAt = FromEpochMs(r.GetInt64(8)),
            LastReviewedAt = r.IsDBNull(9) ? null : FromEpochMs(r.GetInt64(9)),
            ReviewCount = r.GetInt32(10),
            EaseFactor = r.GetDouble(11),
            Interval = r.GetInt32(12),
            Status = SpecToStatus(r.GetString(13))
        };
    }
}

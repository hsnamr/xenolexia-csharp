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
        foreach (var sql in new[] { createBooks, createVocabulary, createSessions, createPreferences, createWordList })
        {
            using var cmd = new SQLiteCommand(sql, _connection);
            await cmd.ExecuteNonQueryAsync();
        }

        // Migration: add columns to existing tables (e.g. old DBs without book_id)
        await MigrateSchemaAsync();

        // Create indexes only for columns that exist (old DBs may have different schema)
        var vocabCols = await GetColumnNamesAsync("vocabulary");
        var sessionCols = await GetColumnNamesAsync("reading_sessions");
        var indexStatements = new List<string>();
        if (vocabCols.Contains("book_id")) indexStatements.Add("CREATE INDEX IF NOT EXISTS idx_vocabulary_book ON vocabulary(book_id)");
        if (vocabCols.Contains("status")) indexStatements.Add("CREATE INDEX IF NOT EXISTS idx_vocabulary_status ON vocabulary(status)");
        if (vocabCols.Contains("source_word")) indexStatements.Add("CREATE INDEX IF NOT EXISTS idx_vocabulary_source ON vocabulary(source_word)");
        if (sessionCols.Contains("book_id")) indexStatements.Add("CREATE INDEX IF NOT EXISTS idx_reading_sessions_book ON reading_sessions(book_id)");
        foreach (var sql in indexStatements)
        {
            using var cmdIndex = new SQLiteCommand(sql, _connection);
            await cmdIndex.ExecuteNonQueryAsync();
        }
    }

    private async Task MigrateSchemaAsync()
    {
        if (_connection == null) return;
        var vocabCols = await GetColumnNamesAsync("vocabulary");
        // Ensure vocabulary has book_id, book_title (older schema may not)
        if (!vocabCols.Contains("book_id"))
        {
            using var a1 = new SQLiteCommand("ALTER TABLE vocabulary ADD COLUMN book_id TEXT", _connection);
            await a1.ExecuteNonQueryAsync();
            vocabCols.Add("book_id");
        }
        if (!vocabCols.Contains("book_title"))
        {
            using var a2 = new SQLiteCommand("ALTER TABLE vocabulary ADD COLUMN book_title TEXT", _connection);
            await a2.ExecuteNonQueryAsync();
            vocabCols.Add("book_title");
        }
        // Migrate PascalCase columns to snake_case (SQLite 3.25+ RENAME COLUMN)
        await RenameColumnIfExistsAsync("vocabulary", "SourceWord", "source_word");
        await RenameColumnIfExistsAsync("vocabulary", "TargetWord", "target_word");
        await RenameColumnIfExistsAsync("vocabulary", "SourceLang", "source_lang");
        await RenameColumnIfExistsAsync("vocabulary", "TargetLang", "target_lang");
        await RenameColumnIfExistsAsync("vocabulary", "ContextSentence", "context_sentence");
        await RenameColumnIfExistsAsync("vocabulary", "BookId", "book_id");
        await RenameColumnIfExistsAsync("vocabulary", "BookTitle", "book_title");
        await RenameColumnIfExistsAsync("vocabulary", "AddedAt", "added_at");
        await RenameColumnIfExistsAsync("vocabulary", "LastReviewedAt", "last_reviewed_at");
        await RenameColumnIfExistsAsync("vocabulary", "ReviewCount", "review_count");
        await RenameColumnIfExistsAsync("vocabulary", "EaseFactor", "ease_factor");
        await RenameColumnIfExistsAsync("vocabulary", "Interval", "interval");
        await RenameColumnIfExistsAsync("vocabulary", "Status", "status");
        // Ensure reading_sessions has book_id
        var sessionCols = await GetColumnNamesAsync("reading_sessions");
        if (sessionCols.Count > 0 && !sessionCols.Contains("book_id"))
        {
            using var a3 = new SQLiteCommand("ALTER TABLE reading_sessions ADD COLUMN book_id TEXT", _connection);
            await a3.ExecuteNonQueryAsync();
        }
        // Ensure books table has all columns (old DBs may lack cover_path, last_read_at, etc.)
        await MigrateBooksTableAsync();
        // Fix books table if id was INTEGER (causes constraint failed when inserting UUID)
        await EnsureBooksTableIdIsTextAsync();
    }

    private async Task EnsureBooksTableIdIsTextAsync()
    {
        if (_connection == null) return;
        var idType = await GetColumnTypeAsync("books", "id");
        if (idType == null || idType.IndexOf("INT", StringComparison.OrdinalIgnoreCase) < 0)
            return;
        Console.WriteLine($"[Storage] books.id type is '{idType}', recreating table with id TEXT");
        const string createNew = @"
            CREATE TABLE books_new (
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
        using (var cmd = new SQLiteCommand(createNew, _connection))
            await cmd.ExecuteNonQueryAsync();
        try
        {
            using var copyCmd = new SQLiteCommand(@"
                INSERT INTO books_new (id, title, author, cover_path, file_path, format, file_size, added_at, last_read_at,
                    source_lang, target_lang, proficiency, density, progress, current_location,
                    current_chapter, total_chapters, current_page, total_pages, reading_time_minutes, source_url, is_downloaded)
                SELECT CAST(id AS TEXT), title, author, cover_path, file_path, format, file_size, added_at, last_read_at,
                    source_lang, target_lang, proficiency, density, progress, current_location,
                    current_chapter, total_chapters, current_page, total_pages, reading_time_minutes, source_url, is_downloaded
                FROM books", _connection);
            await copyCmd.ExecuteNonQueryAsync();
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine($"[Storage] Copy to books_new failed ({ex.Message}), dropping old table and keeping empty.");
        }
        using (var cmd = new SQLiteCommand("DROP TABLE books", _connection))
            await cmd.ExecuteNonQueryAsync();
        using (var cmd = new SQLiteCommand("ALTER TABLE books_new RENAME TO books", _connection))
            await cmd.ExecuteNonQueryAsync();
    }

    private async Task<string?> GetColumnTypeAsync(string tableName, string columnName)
    {
        if (_connection == null) return null;
        using var cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", _connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return reader.IsDBNull(2) ? null : reader.GetString(2);
        }
        return null;
    }

    private async Task MigrateBooksTableAsync()
    {
        if (_connection == null) return;
        var cols = await GetColumnNamesAsync("books");
        if (cols.Count == 0) return; // table doesn't exist yet
        var additions = new[] {
            ("cover_path", "TEXT"), ("file_path", "TEXT"), ("format", "TEXT"), ("file_size", "INTEGER"),
            ("added_at", "INTEGER"), ("last_read_at", "INTEGER"), ("progress", "REAL"), ("current_location", "TEXT"),
            ("source_lang", "TEXT"), ("target_lang", "TEXT"), ("proficiency", "TEXT"), ("density", "REAL"),
            ("current_chapter", "INTEGER"), ("total_chapters", "INTEGER"), ("current_page", "INTEGER"), ("total_pages", "INTEGER"),
            ("reading_time_minutes", "INTEGER"), ("source_url", "TEXT"), ("is_downloaded", "INTEGER"),
        };
        foreach (var (name, sqlType) in additions)
        {
            if (cols.Contains(name)) continue;
            try
            {
                using var cmd = new SQLiteCommand($"ALTER TABLE books ADD COLUMN {name} {sqlType}", _connection);
                await cmd.ExecuteNonQueryAsync();
                cols.Add(name);
            }
            catch (SQLiteException) { /* duplicate column from race; ignore */ }
        }
    }

    private async Task RenameColumnIfExistsAsync(string table, string oldName, string newName)
    {
        if (_connection == null) return;
        var cols = await GetColumnNamesAsync(table);
        if (!cols.Contains(oldName) || cols.Contains(newName)) return;
        try
        {
            using var cmd = new SQLiteCommand($"ALTER TABLE {table} RENAME COLUMN \"{oldName}\" TO \"{newName}\"", _connection);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SQLiteException)
        {
            // SQLite < 3.25 has no RENAME COLUMN; ignore
        }
    }

    private async Task<HashSet<string>> GetColumnNamesAsync(string tableName)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_connection == null) return set;
        using var cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", _connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(1);
            set.Add(name);
        }
        return set;
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
    private static string ThemeToSpec(ReaderTheme t) => t.ToString().ToLowerInvariant();
    private static ReaderTheme SpecToTheme(string s) => Enum.Parse<ReaderTheme>(s, true);
    private static string TextAlignToSpec(TextAlign a) => a.ToString().ToLowerInvariant();
    private static TextAlign SpecToTextAlign(string s) => Enum.Parse<TextAlign>(s, true);

    private static DateTime SafeFromEpochMs(long ms)
    {
        if (ms <= 0 || ms > 8640000000000000L) return DateTime.UnixEpoch;
        try { return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime; } catch { return DateTime.UnixEpoch; }
    }
    private static BookFormat SafeSpecToFormat(string? s) => string.IsNullOrWhiteSpace(s) || !Enum.TryParse<BookFormat>(s, true, out var f) ? BookFormat.Epub : f;
    private static Language SafeSpecToLang(string? s) => string.IsNullOrWhiteSpace(s) || !Enum.TryParse<Language>(s, true, out var l) ? Language.En : l;
    private static ProficiencyLevel SafeSpecToProf(string? s) => string.IsNullOrWhiteSpace(s) || !Enum.TryParse<ProficiencyLevel>(s, true, out var p) ? ProficiencyLevel.Beginner : p;

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

    public async Task<Book?> GetBookByFilePathAsync(string filePath)
    {
        if (_connection == null || string.IsNullOrWhiteSpace(filePath)) return null;
        var query = "SELECT * FROM books WHERE file_path = @file_path LIMIT 1";
        using var cmd = new SQLiteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@file_path", filePath);
        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapBookFromReader((SQLiteDataReader)reader) : null;
    }

    public async Task AddBookAsync(Book book)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        if (string.IsNullOrWhiteSpace(book.Id))
            book.Id = Guid.NewGuid().ToString("N")[..16];
        Console.WriteLine($"[Storage] AddBook: id={book.Id?.Length ?? 0} chars, file_path={book.FilePath?.Length ?? 0} chars, title={book.Title?.Length ?? 0} chars");
        await LogBooksTableSchemaAsync();
        var query = @"INSERT INTO books (id, title, author, cover_path, file_path, format, file_size, added_at, last_read_at,
            source_lang, target_lang, proficiency, density, progress, current_location,
            current_chapter, total_chapters, current_page, total_pages, reading_time_minutes, source_url, is_downloaded)
            VALUES (@id, @title, @author, @cover_path, @file_path, @format, @file_size, @added_at, @last_read_at,
            @source_lang, @target_lang, @proficiency, @density, @progress, @current_location,
            @current_chapter, @total_chapters, @current_page, @total_pages, @reading_time_minutes, @source_url, @is_downloaded)";
        try
        {
            using var cmd = new SQLiteCommand(query, _connection);
            AddBookParams(cmd, book);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine($"[Storage] SQLiteException: ErrorCode={ex.ResultCode}, Message={ex.Message}");
            throw new InvalidOperationException($"Insert failed: {ex.ResultCode} - {ex.Message}", ex);
        }
    }

    private async Task LogBooksTableSchemaAsync()
    {
        if (_connection == null) return;
        try
        {
            using var cmd = new SQLiteCommand("PRAGMA table_info(books)", _connection);
            using var reader = await cmd.ExecuteReaderAsync();
            var columns = new List<string>();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(1);
                var notnull = reader.GetInt32(3);
                columns.Add(notnull != 0 ? $"{name}(NOT NULL)" : name);
            }
            Console.WriteLine($"[Storage] books table columns: {string.Join(", ", columns)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Storage] Could not read table_info: {ex.Message}");
        }
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
        cmd.Parameters.AddWithValue("@id", book.Id ?? "");
        cmd.Parameters.AddWithValue("@title", book.Title ?? "");
        cmd.Parameters.AddWithValue("@author", book.Author ?? "");
        cmd.Parameters.AddWithValue("@cover_path", string.IsNullOrEmpty(book.CoverPath) ? (object)DBNull.Value : book.CoverPath);
        cmd.Parameters.AddWithValue("@file_path", book.FilePath ?? "");
        cmd.Parameters.AddWithValue("@format", FormatToSpec(book.Format));
        cmd.Parameters.AddWithValue("@file_size", book.FileSize);
        var addedAt = book.AddedAt;
        if (addedAt == default) addedAt = DateTime.UtcNow;
        cmd.Parameters.AddWithValue("@added_at", ToEpochMs(addedAt));
        cmd.Parameters.AddWithValue("@last_read_at", book.LastReadAt.HasValue ? ToEpochMs(book.LastReadAt.Value) : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@source_lang", book.LanguagePair != null ? LangToSpec(book.LanguagePair.SourceLanguage) : "en");
        cmd.Parameters.AddWithValue("@target_lang", book.LanguagePair != null ? LangToSpec(book.LanguagePair.TargetLanguage) : "es");
        cmd.Parameters.AddWithValue("@proficiency", ProfToSpec(book.ProficiencyLevel));
        cmd.Parameters.AddWithValue("@density", Math.Max(0.1, Math.Min(1.0, book.WordDensity)));
        cmd.Parameters.AddWithValue("@progress", book.Progress);
        cmd.Parameters.AddWithValue("@current_location", string.IsNullOrEmpty(book.CurrentLocation) ? (object)DBNull.Value : book.CurrentLocation);
        cmd.Parameters.AddWithValue("@current_chapter", book.CurrentChapter);
        cmd.Parameters.AddWithValue("@total_chapters", book.TotalChapters);
        cmd.Parameters.AddWithValue("@current_page", book.CurrentPage);
        cmd.Parameters.AddWithValue("@total_pages", book.TotalPages);
        cmd.Parameters.AddWithValue("@reading_time_minutes", book.ReadingTimeMinutes);
        cmd.Parameters.AddWithValue("@source_url", string.IsNullOrEmpty(book.SourceUrl) ? (object)DBNull.Value : book.SourceUrl);
        cmd.Parameters.AddWithValue("@is_downloaded", book.IsDownloaded ? 1 : 0);
    }

    private Book MapBookFromReader(SQLiteDataReader r)
    {
        return new Book
        {
            Id = GetString(r, "id"),
            Title = GetString(r, "title"),
            Author = GetString(r, "author") ?? "",
            CoverPath = GetStringNull(r, "cover_path"),
            FilePath = GetString(r, "file_path"),
            Format = SafeSpecToFormat(GetStringNull(r, "format")),
            FileSize = GetInt64(r, "file_size"),
            AddedAt = SafeFromEpochMs(GetInt64(r, "added_at")),
            LastReadAt = GetInt64Null(r, "last_read_at") is { } t ? SafeFromEpochMs(t) : null,
            Progress = GetDouble(r, "progress"),
            CurrentLocation = GetStringNull(r, "current_location"),
            LanguagePair = new LanguagePair { SourceLanguage = SafeSpecToLang(GetStringNull(r, "source_lang")), TargetLanguage = SafeSpecToLang(GetStringNull(r, "target_lang")) },
            ProficiencyLevel = SafeSpecToProf(GetStringNull(r, "proficiency")),
            WordDensity = GetDouble(r, "density"),
            CurrentChapter = GetInt32(r, "current_chapter"),
            TotalChapters = GetInt32(r, "total_chapters"),
            CurrentPage = GetInt32(r, "current_page"),
            TotalPages = GetInt32(r, "total_pages"),
            ReadingTimeMinutes = GetInt32(r, "reading_time_minutes"),
            SourceUrl = GetStringNull(r, "source_url"),
            IsDownloaded = GetInt32(r, "is_downloaded") != 0
        };
    }

    private static string GetString(SQLiteDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? "" : r.GetString(r.GetOrdinal(col));
    private static string? GetStringNull(SQLiteDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(r.GetOrdinal(col));
    private static long GetInt64(SQLiteDataReader r, string col) { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? 0 : Convert.ToInt64(r.GetValue(i)); }
    private static long? GetInt64Null(SQLiteDataReader r, string col) { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? null : Convert.ToInt64(r.GetValue(i)); }
    private static double GetDouble(SQLiteDataReader r, string col) { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? 0 : Convert.ToDouble(r.GetValue(i)); }
    private static int GetInt32(SQLiteDataReader r, string col) { var i = r.GetOrdinal(col); return r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i)); }

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

    public async Task<UserPreferences> GetPreferencesAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = new SQLiteCommand("SELECT key, value FROM preferences", _connection))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                dict[reader.GetString(0)] = reader.GetString(1);
        }

        string Get(string key, string defaultValue = "")
        {
            return dict.TryGetValue(key, out var v) ? v : defaultValue;
        }

        return new UserPreferences
        {
            DefaultSourceLanguage = ParseLang(Get("source_lang", "en")),
            DefaultTargetLanguage = ParseLang(Get("target_lang", "es")),
            DefaultProficiencyLevel = ParseProf(Get("proficiency", "beginner")),
            DefaultWordDensity = ParseDouble(Get("word_density", "0.3"), 0.3),
            ReaderSettings = new ReaderSettings
            {
                Theme = ParseTheme(Get("reader_theme", "light")),
                FontFamily = Get("reader_font_family", "System"),
                FontSize = ParseDouble(Get("reader_font_size", "16"), 16),
                LineHeight = ParseDouble(Get("reader_line_height", "1.6"), 1.6),
                MarginHorizontal = ParseDouble(Get("reader_margin_horizontal", "24"), 24),
                MarginVertical = ParseDouble(Get("reader_margin_vertical", "16"), 16),
                TextAlign = ParseTextAlign(Get("reader_text_align", "left")),
                Brightness = ParseDouble(Get("reader_brightness", "1"), 1.0)
            },
            HasCompletedOnboarding = Get("onboarding_done", "false").Equals("true", StringComparison.OrdinalIgnoreCase),
            NotificationsEnabled = Get("notifications_enabled", "false").Equals("true", StringComparison.OrdinalIgnoreCase),
            DailyGoal = (int)ParseDouble(Get("daily_goal", "30"), 30)
        };
    }

    public async Task SavePreferencesAsync(UserPreferences prefs)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var pairs = new[]
        {
            ("source_lang", LangToSpec(prefs.DefaultSourceLanguage)),
            ("target_lang", LangToSpec(prefs.DefaultTargetLanguage)),
            ("proficiency", ProfToSpec(prefs.DefaultProficiencyLevel)),
            ("word_density", prefs.DefaultWordDensity.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)),
            ("reader_theme", ThemeToSpec(prefs.ReaderSettings.Theme)),
            ("reader_font_family", prefs.ReaderSettings.FontFamily ?? "System"),
            ("reader_font_size", prefs.ReaderSettings.FontSize.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)),
            ("reader_line_height", prefs.ReaderSettings.LineHeight.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)),
            ("reader_margin_horizontal", prefs.ReaderSettings.MarginHorizontal.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)),
            ("reader_margin_vertical", prefs.ReaderSettings.MarginVertical.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)),
            ("reader_text_align", TextAlignToSpec(prefs.ReaderSettings.TextAlign)),
            ("reader_brightness", prefs.ReaderSettings.Brightness.ToString("G17", System.Globalization.CultureInfo.InvariantCulture)),
            ("onboarding_done", prefs.HasCompletedOnboarding ? "true" : "false"),
            ("notifications_enabled", prefs.NotificationsEnabled ? "true" : "false"),
            ("daily_goal", prefs.DailyGoal.ToString())
        };
        foreach (var (key, value) in pairs)
        {
            using var cmd = new SQLiteCommand("INSERT OR REPLACE INTO preferences (key, value) VALUES (@key, @value)", _connection);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", value);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<string> StartReadingSessionAsync(string bookId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var sessionId = Guid.NewGuid().ToString("N");
        var nowMs = ToEpochMs(DateTime.UtcNow);
        var sql = @"INSERT INTO reading_sessions (id, book_id, started_at, ended_at, pages_read, words_revealed, words_saved)
                    VALUES (@id, @book_id, @started_at, NULL, 0, 0, 0)";
        using var cmd = new SQLiteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@id", sessionId);
        cmd.Parameters.AddWithValue("@book_id", bookId);
        cmd.Parameters.AddWithValue("@started_at", nowMs);
        await cmd.ExecuteNonQueryAsync();
        return sessionId;
    }

    public async Task EndReadingSessionAsync(string sessionId, int wordsRevealed, int wordsSaved)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var nowMs = ToEpochMs(DateTime.UtcNow);
        var sql = @"UPDATE reading_sessions SET ended_at = @ended_at, words_revealed = @words_revealed, words_saved = @words_saved WHERE id = @id";
        using var cmd = new SQLiteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@id", sessionId);
        cmd.Parameters.AddWithValue("@ended_at", nowMs);
        cmd.Parameters.AddWithValue("@words_revealed", wordsRevealed);
        cmd.Parameters.AddWithValue("@words_saved", wordsSaved);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<ReadingSession?> GetActiveSessionForBookAsync(string bookId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");
        var sql = @"SELECT id, book_id, started_at, ended_at, pages_read, words_revealed, words_saved
                    FROM reading_sessions WHERE book_id = @book_id AND ended_at IS NULL ORDER BY started_at DESC LIMIT 1";
        using var cmd = new SQLiteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@book_id", bookId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return MapReadingSessionFromReader((SQLiteDataReader)reader);
    }

    private static ReadingSession MapReadingSessionFromReader(SQLiteDataReader r)
    {
        var startedMs = r.GetInt64(2);
        var endedMs = r.IsDBNull(3) ? (long?)null : r.GetInt64(3);
        var durationSec = endedMs.HasValue ? (int)((endedMs.Value - startedMs) / 1000) : 0;
        return new ReadingSession
        {
            Id = r.GetString(0),
            BookId = r.GetString(1),
            StartedAt = FromEpochMs(startedMs),
            EndedAt = endedMs.HasValue ? FromEpochMs(endedMs.Value) : null,
            PagesRead = r.GetInt32(4),
            WordsRevealed = r.GetInt32(5),
            WordsSaved = r.GetInt32(6),
            Duration = durationSec
        };
    }

    private static Language ParseLang(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return Language.En;
        try { return SpecToLang(s); } catch { return Language.En; }
    }

    private static ProficiencyLevel ParseProf(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return ProficiencyLevel.Beginner;
        try { return SpecToProf(s); } catch { return ProficiencyLevel.Beginner; }
    }

    private static ReaderTheme ParseTheme(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return ReaderTheme.Light;
        try { return SpecToTheme(s); } catch { return ReaderTheme.Light; }
    }

    private static TextAlign ParseTextAlign(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return TextAlign.Left;
        try { return SpecToTextAlign(s); } catch { return TextAlign.Left; }
    }

    private static double ParseDouble(string s, double defaultValue)
    {
        if (string.IsNullOrWhiteSpace(s)) return defaultValue;
        return double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : defaultValue;
    }

    public async Task<ReadingStats> GetReadingStatsAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var todayUtc = DateTime.UtcNow.Date;
        var bookIds = new HashSet<string>();
        var totalSeconds = 0;
        var sessionCount = 0;
        var wordsRevealedToday = 0;
        var wordsSavedToday = 0;
        var sessionDates = new List<DateTime>();

        var sql = @"SELECT book_id, started_at, ended_at, words_revealed, words_saved
                    FROM reading_sessions WHERE ended_at IS NOT NULL";
        using (var cmd = new SQLiteCommand(sql, _connection))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var bookId = reader.GetString(0);
                var startedMs = reader.GetInt64(1);
                var endedMs = reader.GetInt64(2);
                var wordsRevealed = reader.GetInt32(3);
                var wordsSaved = reader.GetInt32(4);

                bookIds.Add(bookId);
                var durationSec = (int)((endedMs - startedMs) / 1000);
                totalSeconds += durationSec;
                sessionCount++;

                var endedDate = FromEpochMs(endedMs).Date;
                sessionDates.Add(endedDate);
                if (endedDate == todayUtc)
                {
                    wordsRevealedToday += wordsRevealed;
                    wordsSavedToday += wordsSaved;
                }
            }
        }

        var totalWordsLearned = 0;
        using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM vocabulary WHERE status = 'learned'", _connection))
        {
            totalWordsLearned = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

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
            if (diff == 1)
                current++;
            else
                current = 1;
            if (current > maxStreak) maxStreak = current;
        }
        return maxStreak;
    }
}

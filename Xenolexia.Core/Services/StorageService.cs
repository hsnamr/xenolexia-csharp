using System.Data.SQLite;
using Xenolexia.Core.Models;

namespace Xenolexia.Core.Services;

/// <summary>
/// SQLite-based storage service
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

        var createBooksTable = @"
            CREATE TABLE IF NOT EXISTS Books (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Author TEXT NOT NULL,
                CoverPath TEXT,
                FilePath TEXT NOT NULL,
                Format TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                AddedAt TEXT NOT NULL,
                LastReadAt TEXT,
                SourceLanguage TEXT NOT NULL,
                TargetLanguage TEXT NOT NULL,
                ProficiencyLevel TEXT NOT NULL,
                WordDensity REAL NOT NULL,
                Progress REAL NOT NULL,
                CurrentLocation TEXT,
                CurrentChapter INTEGER NOT NULL,
                TotalChapters INTEGER NOT NULL,
                CurrentPage INTEGER NOT NULL,
                TotalPages INTEGER NOT NULL,
                ReadingTimeMinutes INTEGER NOT NULL,
                SourceUrl TEXT,
                IsDownloaded INTEGER NOT NULL
            )";

        var createVocabularyTable = @"
            CREATE TABLE IF NOT EXISTS Vocabulary (
                Id TEXT PRIMARY KEY,
                SourceWord TEXT NOT NULL,
                TargetWord TEXT NOT NULL,
                SourceLanguage TEXT NOT NULL,
                TargetLanguage TEXT NOT NULL,
                ContextSentence TEXT,
                BookId TEXT,
                BookTitle TEXT,
                AddedAt TEXT NOT NULL,
                LastReviewedAt TEXT,
                ReviewCount INTEGER NOT NULL,
                EaseFactor REAL NOT NULL,
                Interval INTEGER NOT NULL,
                Status TEXT NOT NULL
            )";

        using var cmd1 = new SQLiteCommand(createBooksTable, _connection);
        await cmd1.ExecuteNonQueryAsync();

        using var cmd2 = new SQLiteCommand(createVocabularyTable, _connection);
        await cmd2.ExecuteNonQueryAsync();
    }

    public async Task<List<Book>> GetAllBooksAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var books = new List<Book>();
        var query = "SELECT * FROM Books ORDER BY AddedAt DESC";

        using var cmd = new SQLiteCommand(query, _connection);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            books.Add(MapBookFromReader((SQLiteDataReader)reader));
        }

        return books;
    }

    public async Task<Book?> GetBookByIdAsync(string bookId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var query = "SELECT * FROM Books WHERE Id = @Id";
        using var cmd = new SQLiteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@Id", bookId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapBookFromReader((SQLiteDataReader)reader);
        }

        return null;
    }

    public async Task AddBookAsync(Book book)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var query = @"
            INSERT INTO Books (Id, Title, Author, CoverPath, FilePath, Format, FileSize, AddedAt, LastReadAt,
                SourceLanguage, TargetLanguage, ProficiencyLevel, WordDensity, Progress, CurrentLocation,
                CurrentChapter, TotalChapters, CurrentPage, TotalPages, ReadingTimeMinutes, SourceUrl, IsDownloaded)
            VALUES (@Id, @Title, @Author, @CoverPath, @FilePath, @Format, @FileSize, @AddedAt, @LastReadAt,
                @SourceLanguage, @TargetLanguage, @ProficiencyLevel, @WordDensity, @Progress, @CurrentLocation,
                @CurrentChapter, @TotalChapters, @CurrentPage, @TotalPages, @ReadingTimeMinutes, @SourceUrl, @IsDownloaded)";

        using var cmd = new SQLiteCommand(query, _connection);
        AddBookParameters(cmd, book);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateBookAsync(Book book)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var query = @"
            UPDATE Books SET Title = @Title, Author = @Author, CoverPath = @CoverPath, FilePath = @FilePath,
                Format = @Format, FileSize = @FileSize, AddedAt = @AddedAt, LastReadAt = @LastReadAt,
                SourceLanguage = @SourceLanguage, TargetLanguage = @TargetLanguage, ProficiencyLevel = @ProficiencyLevel,
                WordDensity = @WordDensity, Progress = @Progress, CurrentLocation = @CurrentLocation,
                CurrentChapter = @CurrentChapter, TotalChapters = @TotalChapters, CurrentPage = @CurrentPage,
                TotalPages = @TotalPages, ReadingTimeMinutes = @ReadingTimeMinutes, SourceUrl = @SourceUrl,
                IsDownloaded = @IsDownloaded
            WHERE Id = @Id";

        using var cmd = new SQLiteCommand(query, _connection);
        AddBookParameters(cmd, book);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteBookAsync(string bookId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var query = "DELETE FROM Books WHERE Id = @Id";
        using var cmd = new SQLiteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@Id", bookId);
        await cmd.ExecuteNonQueryAsync();
    }

    private void AddBookParameters(SQLiteCommand cmd, Book book)
    {
        cmd.Parameters.AddWithValue("@Id", book.Id);
        cmd.Parameters.AddWithValue("@Title", book.Title);
        cmd.Parameters.AddWithValue("@Author", book.Author);
        cmd.Parameters.AddWithValue("@CoverPath", book.CoverPath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@FilePath", book.FilePath);
        cmd.Parameters.AddWithValue("@Format", book.Format.ToString());
        cmd.Parameters.AddWithValue("@FileSize", book.FileSize);
        cmd.Parameters.AddWithValue("@AddedAt", book.AddedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@LastReadAt", book.LastReadAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@SourceLanguage", book.LanguagePair.SourceLanguage.ToString());
        cmd.Parameters.AddWithValue("@TargetLanguage", book.LanguagePair.TargetLanguage.ToString());
        cmd.Parameters.AddWithValue("@ProficiencyLevel", book.ProficiencyLevel.ToString());
        cmd.Parameters.AddWithValue("@WordDensity", book.WordDensity);
        cmd.Parameters.AddWithValue("@Progress", book.Progress);
        cmd.Parameters.AddWithValue("@CurrentLocation", book.CurrentLocation ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@CurrentChapter", book.CurrentChapter);
        cmd.Parameters.AddWithValue("@TotalChapters", book.TotalChapters);
        cmd.Parameters.AddWithValue("@CurrentPage", book.CurrentPage);
        cmd.Parameters.AddWithValue("@TotalPages", book.TotalPages);
        cmd.Parameters.AddWithValue("@ReadingTimeMinutes", book.ReadingTimeMinutes);
        cmd.Parameters.AddWithValue("@SourceUrl", book.SourceUrl ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IsDownloaded", book.IsDownloaded ? 1 : 0);
    }

    private Book MapBookFromReader(SQLiteDataReader reader)
    {
        return new Book
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Author = reader.GetString(2),
            CoverPath = reader.IsDBNull(3) ? null : reader.GetString(3),
            FilePath = reader.GetString(4),
            Format = Enum.Parse<BookFormat>(reader.GetString(5)),
            FileSize = reader.GetInt64(6),
            AddedAt = DateTime.Parse(reader.GetString(7)),
            LastReadAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
            LanguagePair = new LanguagePair
            {
                SourceLanguage = Enum.Parse<Language>(reader.GetString(9)),
                TargetLanguage = Enum.Parse<Language>(reader.GetString(10))
            },
            ProficiencyLevel = Enum.Parse<ProficiencyLevel>(reader.GetString(11)),
            WordDensity = reader.GetDouble(12),
            Progress = reader.GetDouble(13),
            CurrentLocation = reader.IsDBNull(14) ? null : reader.GetString(14),
            CurrentChapter = reader.GetInt32(15),
            TotalChapters = reader.GetInt32(16),
            CurrentPage = reader.GetInt32(17),
            TotalPages = reader.GetInt32(18),
            ReadingTimeMinutes = reader.GetInt32(19),
            SourceUrl = reader.IsDBNull(20) ? null : reader.GetString(20),
            IsDownloaded = reader.GetInt32(21) == 1
        };
    }

    public async Task<List<VocabularyItem>> GetVocabularyItemsAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var items = new List<VocabularyItem>();
        var query = "SELECT * FROM Vocabulary ORDER BY AddedAt DESC";

        using var cmd = new SQLiteCommand(query, _connection);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(MapVocabularyFromReader((SQLiteDataReader)reader));
        }

        return items;
    }

    public async Task<VocabularyItem?> GetVocabularyItemByIdAsync(string wordId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var query = "SELECT * FROM Vocabulary WHERE Id = @Id";
        using var cmd = new SQLiteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@Id", wordId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapVocabularyFromReader((SQLiteDataReader)reader);
        }

        return null;
    }

    public async Task AddVocabularyItemAsync(VocabularyItem item)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var query = @"
            INSERT INTO Vocabulary (Id, SourceWord, TargetWord, SourceLanguage, TargetLanguage,
                ContextSentence, BookId, BookTitle, AddedAt, LastReviewedAt, ReviewCount,
                EaseFactor, Interval, Status)
            VALUES (@Id, @SourceWord, @TargetWord, @SourceLanguage, @TargetLanguage,
                @ContextSentence, @BookId, @BookTitle, @AddedAt, @LastReviewedAt, @ReviewCount,
                @EaseFactor, @Interval, @Status)";

        using var cmd = new SQLiteCommand(query, _connection);
        AddVocabularyParameters(cmd, item);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateVocabularyItemAsync(VocabularyItem item)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var query = @"
            UPDATE Vocabulary SET SourceWord = @SourceWord, TargetWord = @TargetWord,
                SourceLanguage = @SourceLanguage, TargetLanguage = @TargetLanguage,
                ContextSentence = @ContextSentence, BookId = @BookId, BookTitle = @BookTitle,
                AddedAt = @AddedAt, LastReviewedAt = @LastReviewedAt, ReviewCount = @ReviewCount,
                EaseFactor = @EaseFactor, Interval = @Interval, Status = @Status
            WHERE Id = @Id";

        using var cmd = new SQLiteCommand(query, _connection);
        AddVocabularyParameters(cmd, item);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteVocabularyItemAsync(string wordId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var query = "DELETE FROM Vocabulary WHERE Id = @Id";
        using var cmd = new SQLiteCommand(query, _connection);
        cmd.Parameters.AddWithValue("@Id", wordId);
        await cmd.ExecuteNonQueryAsync();
    }

    private void AddVocabularyParameters(SQLiteCommand cmd, VocabularyItem item)
    {
        cmd.Parameters.AddWithValue("@Id", item.Id);
        cmd.Parameters.AddWithValue("@SourceWord", item.SourceWord);
        cmd.Parameters.AddWithValue("@TargetWord", item.TargetWord);
        cmd.Parameters.AddWithValue("@SourceLanguage", item.SourceLanguage.ToString());
        cmd.Parameters.AddWithValue("@TargetLanguage", item.TargetLanguage.ToString());
        cmd.Parameters.AddWithValue("@ContextSentence", item.ContextSentence ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BookId", item.BookId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BookTitle", item.BookTitle ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@AddedAt", item.AddedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@LastReviewedAt", item.LastReviewedAt?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ReviewCount", item.ReviewCount);
        cmd.Parameters.AddWithValue("@EaseFactor", item.EaseFactor);
        cmd.Parameters.AddWithValue("@Interval", item.Interval);
        cmd.Parameters.AddWithValue("@Status", item.Status.ToString());
    }

    private VocabularyItem MapVocabularyFromReader(SQLiteDataReader reader)
    {
        return new VocabularyItem
        {
            Id = reader.GetString(0),
            SourceWord = reader.GetString(1),
            TargetWord = reader.GetString(2),
            SourceLanguage = Enum.Parse<Language>(reader.GetString(3)),
            TargetLanguage = Enum.Parse<Language>(reader.GetString(4)),
            ContextSentence = reader.IsDBNull(5) ? null : reader.GetString(5),
            BookId = reader.IsDBNull(6) ? null : reader.GetString(6),
            BookTitle = reader.IsDBNull(7) ? null : reader.GetString(7),
            AddedAt = DateTime.Parse(reader.GetString(8)),
            LastReviewedAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9)),
            ReviewCount = reader.GetInt32(10),
            EaseFactor = reader.GetDouble(11),
            Interval = reader.GetInt32(12),
            Status = Enum.Parse<VocabularyStatus>(reader.GetString(13))
        };
    }
}

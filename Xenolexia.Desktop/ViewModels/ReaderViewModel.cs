using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Desktop.ViewModels;

public partial class ReaderViewModel : ViewModelBase
{
    private readonly IBookParserService _parser;
    private readonly TranslationEngine _translationEngine;
    private readonly IStorageService _storageService;
    private readonly string _filePath;
    private ParsedBook? _parsedBook;
    private string? _sessionId;
    private int _wordsRevealed;
    private int _wordsSaved;

    [ObservableProperty]
    private string _bookTitle = string.Empty;

    [ObservableProperty]
    private string _currentChapterTitle = string.Empty;

    [ObservableProperty]
    private string _currentChapterContent = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ReaderContentSegment> _contentSegments = new();

    [ObservableProperty]
    private int _currentChapterIndex;

    [ObservableProperty]
    private ObservableCollection<TableOfContentsItem> _tableOfContents = new();

    [ObservableProperty]
    private ObservableCollection<Chapter> _chapters = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotLoading))]
    private bool _isLoading = true;

    public bool NotLoading => !IsLoading;

    [ObservableProperty]
    private string? _error;

    [ObservableProperty]
    private bool _hasChapters = true;

    /// <summary>True when ContentSegments is empty; show CurrentChapterContent as fallback in view.</summary>
    public bool ShowFallbackContent => ContentSegments.Count == 0;
    /// <summary>True when ContentSegments has items; show segment-based content.</summary>
    public bool ShowSegments => ContentSegments.Count > 0;

    /// <summary>Reader appearance from preferences; applied in ReaderView.</summary>
    [ObservableProperty]
    private ReaderSettings _readerSettings = new();

    public Book Book { get; }

    public ReaderViewModel(Book book, IBookParserService parser, ITranslationService translationService, IStorageService storageService)
    {
        Book = book;
        _parser = parser;
        _translationEngine = new TranslationEngine(translationService);
        _storageService = storageService;
        _filePath = book.FilePath;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        ContentSegments.Clear();
        OnPropertyChanged(nameof(ShowFallbackContent));
        OnPropertyChanged(nameof(ShowSegments));
        _sessionId = null;
        _wordsRevealed = 0;
        _wordsSaved = 0;
        try
        {
            if (string.IsNullOrWhiteSpace(_filePath))
            {
                Error = "This book's file is missing. Remove it from the library and import it again from file.";
                CurrentChapterContent = Error;
                return;
            }

            var prefs = await _storageService.GetPreferencesAsync();
            ReaderSettings = prefs.ReaderSettings;

            _parsedBook = await _parser.ParseBookAsync(_filePath);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BookTitle = _parsedBook.Metadata.Title;
                if (_parsedBook.Chapters.Count == 0)
                {
                    HasChapters = false;
                    CurrentChapterContent = "This format is not yet supported for reading (e.g. MOBI).";
                    return;
                }
                Book.TotalChapters = _parsedBook.Chapters.Count;
                foreach (var item in _parsedBook.TableOfContents)
                    TableOfContents.Add(item);
                foreach (var ch in _parsedBook.Chapters)
                    Chapters.Add(ch);
            });

            if (_parsedBook.Chapters.Count == 0)
                return;

            _sessionId = await _storageService.StartReadingSessionAsync(Book.Id);

            var startIndex = Math.Clamp(Book.CurrentChapter, 0, _parsedBook.Chapters.Count - 1);
            await GoToChapterAsync(startIndex);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Error = msg;
                CurrentChapterContent = "Failed to load the book.";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToChapter))]
    private async Task GoToChapterAsync(int index)
    {
        if (_parsedBook == null || index < 0 || index >= _parsedBook.Chapters.Count)
            return;

        var chapter = _parsedBook.Chapters[index];
        var raw = chapter.Content;
        var contentToProcess = LooksLikeHtml(raw) ? HtmlToPlainText.ToPlainText(raw) : raw;
        var chapterForEngine = new Chapter
        {
            Id = chapter.Id,
            Title = chapter.Title,
            Index = chapter.Index,
            Content = contentToProcess,
            WordCount = chapter.WordCount,
            Href = chapter.Href
        };

        var languagePair = Book.LanguagePair ?? new LanguagePair { SourceLanguage = Language.En, TargetLanguage = Language.Es };
        var density = Math.Clamp(Book.WordDensity, 0.05, 0.5);
        if (density <= 0) density = 0.2;

        ProcessedChapter? processed = null;
        try
        {
            processed = await _translationEngine.ProcessChapterAsync(chapterForEngine, languagePair, Book.ProficiencyLevel, density);
        }
        catch
        {
            // Fallback to raw content if processing fails
        }

        var processedCopy = processed;
        var contentCopy = contentToProcess;
        var chapterTitle = chapter.Title;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentChapterIndex = index;
            CurrentChapterTitle = chapterTitle;
            if (processedCopy != null && processedCopy.ForeignWords.Count > 0)
            {
                CurrentChapterContent = processedCopy.ProcessedContent;
                BuildContentSegments(processedCopy);
            }
            else
            {
                CurrentChapterContent = contentCopy;
                ContentSegments.Clear();
                // Do not add a single segment: keep ShowFallbackContent true so the view shows CurrentChapterContent in the TextBlock
                OnPropertyChanged(nameof(ShowFallbackContent));
                OnPropertyChanged(nameof(ShowSegments));
            }
        });
    }

    private void BuildContentSegments(ProcessedChapter processed)
    {
        ContentSegments.Clear();
        OnPropertyChanged(nameof(ShowFallbackContent));
        OnPropertyChanged(nameof(ShowSegments));
        var sorted = processed.ForeignWords.OrderBy(f => f.StartIndex).ToList();
        var pos = 0;
        foreach (var fw in sorted)
        {
            if (fw.StartIndex > pos)
            {
                var plain = processed.ProcessedContent.Substring(pos, fw.StartIndex - pos);
                if (plain.Length > 0)
                    ContentSegments.Add(new ReaderContentSegment { Text = plain, IsForeign = false });
            }
            var foreignText = processed.ProcessedContent.Substring(fw.StartIndex, fw.EndIndex - fw.StartIndex);
            ContentSegments.Add(new ReaderContentSegment
            {
                Text = foreignText,
                IsForeign = true,
                WordData = fw,
                SaveWordCommand = SaveWordCommand,
                NotifyRevealedCommand = NotifyWordRevealedCommand
            });
            pos = fw.EndIndex;
        }
        if (pos < processed.ProcessedContent.Length)
        {
            var tail = processed.ProcessedContent.Substring(pos);
            if (tail.Length > 0)
                ContentSegments.Add(new ReaderContentSegment { Text = tail, IsForeign = false });
        }
        OnPropertyChanged(nameof(ShowFallbackContent));
        OnPropertyChanged(nameof(ShowSegments));
    }

    private bool CanGoToChapter(int index) => _parsedBook != null && index >= 0 && index < _parsedBook.Chapters.Count;

    [RelayCommand]
    private void Close()
    {
        BookNavigation.CloseReader();
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextChapter))]
    private async Task NextChapterAsync()
    {
        await GoToChapterAsync(CurrentChapterIndex + 1);
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousChapter))]
    private async Task PreviousChapterAsync()
    {
        await GoToChapterAsync(CurrentChapterIndex - 1);
    }

    private bool CanGoToNextChapter => _parsedBook != null && CurrentChapterIndex < _parsedBook.Chapters.Count - 1;
    private bool CanGoToPreviousChapter => _parsedBook != null && CurrentChapterIndex > 0;

    [RelayCommand]
    private async Task SaveWordAsync(ForeignWordData? wordData)
    {
        if (wordData == null) return;
        try
        {
            var item = new VocabularyItem
            {
                Id = Guid.NewGuid().ToString(),
                SourceWord = wordData.OriginalWord,
                TargetWord = wordData.ForeignWord,
                SourceLanguage = wordData.WordEntry.SourceLanguage,
                TargetLanguage = wordData.WordEntry.TargetLanguage,
                ContextSentence = null,
                BookId = Book.Id,
                BookTitle = Book.Title,
                AddedAt = DateTime.UtcNow,
                Status = VocabularyStatus.New,
                EaseFactor = 2.5,
                Interval = 0,
                ReviewCount = 0
            };
            await _storageService.AddVocabularyItemAsync(item);
            _wordsSaved++;
        }
        catch
        {
            // Ignore; could show toast later
        }
    }

    [RelayCommand]
    private void NotifyWordRevealed()
    {
        _wordsRevealed++;
    }

    partial void OnCurrentChapterIndexChanged(int value)
    {
        GoToChapterCommand.NotifyCanExecuteChanged();
        NextChapterCommand.NotifyCanExecuteChanged();
        PreviousChapterCommand.NotifyCanExecuteChanged();
        if (_parsedBook != null && value >= 0 && value < _parsedBook.Chapters.Count)
            _ = GoToChapterAsync(value);
    }

    private static bool LooksLikeHtml(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var trimmed = s.TrimStart();
        return trimmed.StartsWith("<", StringComparison.Ordinal);
    }
}

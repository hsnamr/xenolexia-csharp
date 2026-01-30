using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Desktop.ViewModels;

public partial class ReaderViewModel : ViewModelBase
{
    private readonly IBookParserService _parser;
    private readonly string _filePath;
    private ParsedBook? _parsedBook;

    [ObservableProperty]
    private string _bookTitle = string.Empty;

    [ObservableProperty]
    private string _currentChapterTitle = string.Empty;

    [ObservableProperty]
    private string _currentChapterContent = string.Empty;

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

    public Book Book { get; }

    public ReaderViewModel(Book book, IBookParserService parser)
    {
        Book = book;
        _parser = parser;
        _filePath = book.FilePath;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        try
        {
            _parsedBook = await _parser.ParseBookAsync(_filePath);
            BookTitle = _parsedBook.Metadata.Title;

            if (_parsedBook.Chapters.Count == 0)
            {
                HasChapters = false;
                CurrentChapterContent = "This format is not yet supported for reading (e.g. MOBI).";
                return;
            }

            foreach (var item in _parsedBook.TableOfContents)
                TableOfContents.Add(item);
            foreach (var ch in _parsedBook.Chapters)
                Chapters.Add(ch);

            var startIndex = Math.Clamp(Book.CurrentChapter, 0, _parsedBook.Chapters.Count - 1);
            await GoToChapterAsync(startIndex);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            CurrentChapterContent = "Failed to load the book.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToChapter))]
    private async Task GoToChapterAsync(int index)
    {
        if (_parsedBook == null || index < 0 || index >= _parsedBook.Chapters.Count)
            return;

        CurrentChapterIndex = index;
        var chapter = _parsedBook.Chapters[index];
        CurrentChapterTitle = chapter.Title;
        var raw = chapter.Content;
        CurrentChapterContent = LooksLikeHtml(raw) ? HtmlToPlainText.ToPlainText(raw) : raw;
        await Task.CompletedTask;
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

    private bool CanGoToNextChapter() => _parsedBook != null && CurrentChapterIndex < _parsedBook.Chapters.Count - 1;
    private bool CanGoToPreviousChapter() => _parsedBook != null && CurrentChapterIndex > 0;

    partial void OnCurrentChapterIndexChanged(int value)
    {
        GoToChapterCommand.NotifyCanExecuteChanged();
        NextChapterCommand.NotifyCanExecuteChanged();
        PreviousChapterCommand.NotifyCanExecuteChanged();
        if (_parsedBook != null && value >= 0 && value < _parsedBook.Chapters.Count)
        {
            var chapter = _parsedBook.Chapters[value];
            CurrentChapterTitle = chapter.Title;
            var raw = chapter.Content;
            CurrentChapterContent = LooksLikeHtml(raw) ? HtmlToPlainText.ToPlainText(raw) : raw;
        }
    }

    private static bool LooksLikeHtml(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var trimmed = s.TrimStart();
        return trimmed.StartsWith("<", StringComparison.Ordinal);
    }
}

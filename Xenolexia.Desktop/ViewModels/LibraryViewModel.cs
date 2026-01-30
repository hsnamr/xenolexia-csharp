using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;
using Xenolexia.Desktop.Services;

namespace Xenolexia.Desktop.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;
    private readonly IBookDownloadService _bookDownloadService;
    private readonly IBookImportService _bookImportService;
    private readonly IFilePickerService _filePickerService;

    [ObservableProperty]
    private ObservableCollection<Book> _books = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _importError;

    [ObservableProperty]
    private bool _isOnlineSearchVisible;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private EbookSource _selectedSource = EbookSource.Gutenberg;

    [ObservableProperty]
    private ObservableCollection<BookSearchResult> _searchResults = new();

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string? _searchError;

    [ObservableProperty]
    private BookSearchResult? _selectedSearchResult;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private string? _downloadError;

    [ObservableProperty]
    private int _downloadProgressPercent;

    /// <summary>Online library sources for the ComboBox.</summary>
    public static EbookSource[] OnlineSourceList { get; } =
        { EbookSource.Gutenberg, EbookSource.StandardEbooks, EbookSource.OpenLibrary };

    /// <summary>Instance access for XAML binding.</summary>
    public EbookSource[] OnlineSources => OnlineSourceList;

    public LibraryViewModel()
    {
        var sp = Program.ServiceProvider ?? throw new InvalidOperationException("Services not initialized");
        _storageService = (IStorageService)sp.GetService(typeof(IStorageService))!;
        _bookDownloadService = (IBookDownloadService)sp.GetService(typeof(IBookDownloadService))!;
        _bookImportService = (IBookImportService)sp.GetService(typeof(IBookImportService))!;
        _filePickerService = (IFilePickerService)sp.GetService(typeof(IFilePickerService))!;
    }

    [RelayCommand]
    private async Task LoadBooksAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            ImportError = null;
            Books.Clear();

            var allBooks = await _storageService.GetAllBooksAsync();
            foreach (var book in allBooks)
            {
                Books.Add(book);
            }
        }
        catch (Exception ex)
        {
            ImportError = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Error loading books: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        ImportError = null;
        var path = await _filePickerService.PickFileAsync("Select an ebook", new[]
        {
            new FilePickerFilter
            {
                Name = "Ebooks",
                Extensions = new[] { "epub", "pdf", "txt", "fb2", "mobi", "azw", "azw3" }
            }
        });
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            if (!BookImportService.IsSupportedFormat(path))
            {
                ImportError = "Unsupported format. Use EPUB, PDF, TXT, FB2, or MOBI.";
                return;
            }

            IsLoading = true;
            var book = await _bookImportService.ImportFromFileAsync(path);
            Books.Insert(0, book);
        }
        catch (Exception ex)
        {
            ImportError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowOnlineSearch()
    {
        IsOnlineSearchVisible = true;
        SearchError = null;
        SearchResults.Clear();
    }

    [RelayCommand]
    private void HideOnlineSearch()
    {
        IsOnlineSearchVisible = false;
        SearchError = null;
    }

    [RelayCommand]
    private async Task SearchOnlineAsync()
    {
        if (IsSearching || string.IsNullOrWhiteSpace(SearchQuery))
            return;

        SearchError = null;
        try
        {
            IsSearching = true;
            SearchResults.Clear();
            var response = await _bookDownloadService.SearchBooksAsync(SearchQuery.Trim(), SelectedSource);
            if (!string.IsNullOrEmpty(response.Error))
            {
                SearchError = response.Error;
                return;
            }
            foreach (var r in response.Results)
                SearchResults.Add(r);
        }
        catch (Exception ex)
        {
            SearchError = ex.Message;
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task DownloadFromSearchAsync(BookSearchResult? result)
    {
        if (result == null || IsDownloading)
            return;

        DownloadError = null;
        try
        {
            IsDownloading = true;
            var progress = new Progress<DownloadProgress>(p =>
            {
                DownloadProgressPercent = p.Percentage;
            });
            var downloadResult = await _bookDownloadService.DownloadBookFromSearchResultAsync(result, progress);
            if (!downloadResult.Success)
            {
                DownloadError = downloadResult.Error ?? "Download failed.";
                return;
            }

            if (string.IsNullOrEmpty(downloadResult.FilePath))
            {
                DownloadError = "Download path missing.";
                return;
            }

            var book = await _bookImportService.AddDownloadedBookAsync(downloadResult.FilePath, downloadResult.Metadata);
            Books.Insert(0, book);
            SearchResults.Remove(result);
        }
        catch (Exception ex)
        {
            DownloadError = ex.Message;
        }
        finally
        {
            IsDownloading = false;
            DownloadProgressPercent = 0;
        }
    }

    [RelayCommand]
    private async Task DeleteBookAsync(Book book)
    {
        try
        {
            await _storageService.DeleteBookAsync(book.Id);
            Books.Remove(book);
        }
        catch (Exception ex)
        {
            ImportError = ex.Message;
        }
    }
}

using System.Collections.ObjectModel;
using System.Windows.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Android.ViewModels;

public class LibraryViewModel : BaseViewModel
{
    private readonly IStorageService _storageService;
    private readonly IBookDownloadService _bookDownloadService;
    private readonly IImageProcessingService _imageService;

    public LibraryViewModel(
        IStorageService storageService,
        IBookDownloadService bookDownloadService,
        IImageProcessingService imageService)
    {
        _storageService = storageService;
        _bookDownloadService = bookDownloadService;
        _imageService = imageService;
        
        Books = new ObservableCollection<Book>();
        RefreshCommand = new Command(async () => await LoadBooksAsync());
        AddBookCommand = new Command(async () => await AddBookAsync());
        DeleteBookCommand = new Command<Book>(async (book) => await DeleteBookAsync(book));
        
        Title = "Library";
    }

    public ObservableCollection<Book> Books { get; }

    public ICommand RefreshCommand { get; }
    public ICommand AddBookCommand { get; }
    public ICommand DeleteBookCommand { get; }

    public async Task LoadBooksAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            Books.Clear();
            
            var books = await _storageService.GetAllBooksAsync();
            foreach (var book in books)
            {
                Books.Add(book);
            }
        }
        catch (Exception ex)
        {
            // Handle error
            System.Diagnostics.Debug.WriteLine($"Error loading books: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddBookAsync()
    {
        // TODO: Implement file picker
        await Task.CompletedTask;
    }

    private async Task DeleteBookAsync(Book book)
    {
        try
        {
            await _storageService.DeleteBookAsync(book.Id);
            Books.Remove(book);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting book: {ex.Message}");
        }
    }
}

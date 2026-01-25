using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Linux.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;
    private readonly IBookDownloadService _bookDownloadService;

    [ObservableProperty]
    private ObservableCollection<Book> _books = new();

    [ObservableProperty]
    private bool _isLoading;

    public LibraryViewModel()
    {
        var serviceProvider = Program.ServiceProvider ?? throw new InvalidOperationException("Services not initialized");
        _storageService = (IStorageService)serviceProvider.GetService(typeof(IStorageService))!;
        _bookDownloadService = (IBookDownloadService)serviceProvider.GetService(typeof(IBookDownloadService))!;
    }

    [RelayCommand]
    private async Task LoadBooksAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            Books.Clear();

            var allBooks = await _storageService.GetAllBooksAsync();
            foreach (var book in allBooks)
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
            IsLoading = false;
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
            System.Diagnostics.Debug.WriteLine($"Error deleting book: {ex.Message}");
        }
    }
}

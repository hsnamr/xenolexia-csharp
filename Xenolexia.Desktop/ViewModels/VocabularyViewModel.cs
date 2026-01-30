using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Desktop.ViewModels;

public partial class VocabularyViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;
    private readonly IExportService _exportService;

    [ObservableProperty]
    private ObservableCollection<VocabularyItem> _vocabulary = new();

    [ObservableProperty]
    private bool _isLoading;

    public VocabularyViewModel()
    {
        var serviceProvider = Program.ServiceProvider ?? throw new InvalidOperationException("Services not initialized");
        _storageService = (IStorageService)serviceProvider.GetService(typeof(IStorageService))!;
        _exportService = (IExportService)serviceProvider.GetService(typeof(IExportService))!;
    }

    [RelayCommand]
    private async Task LoadVocabularyAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            Vocabulary.Clear();

            var items = await _storageService.GetVocabularyItemsAsync();
            foreach (var item in items)
            {
                Vocabulary.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vocabulary: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportVocabularyAsync()
    {
        try
        {
            var result = await _exportService.ExportVocabularyAsync(
                Vocabulary.ToList(),
                Core.Services.ExportFormat.Csv);

            if (result.Success)
            {
                // Show success message
                System.Diagnostics.Debug.WriteLine($"Exported {result.ItemCount} items to {result.FilePath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting vocabulary: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteWordAsync(VocabularyItem item)
    {
        try
        {
            await _storageService.DeleteVocabularyItemAsync(item.Id);
            Vocabulary.Remove(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting word: {ex.Message}");
        }
    }
}

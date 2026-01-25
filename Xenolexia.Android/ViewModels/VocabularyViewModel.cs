using System.Collections.ObjectModel;
using System.Windows.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Android.ViewModels;

public class VocabularyViewModel : BaseViewModel
{
    private readonly IStorageService _storageService;
    private readonly IExportService _exportService;

    public VocabularyViewModel(IStorageService storageService, IExportService exportService)
    {
        _storageService = storageService;
        _exportService = exportService;
        
        Vocabulary = new ObservableCollection<VocabularyItem>();
        RefreshCommand = new Command(async () => await LoadVocabularyAsync());
        ExportCommand = new Command(async () => await ExportVocabularyAsync());
        DeleteWordCommand = new Command<VocabularyItem>(async (item) => await DeleteWordAsync(item));
        
        Title = "Vocabulary";
    }

    public ObservableCollection<VocabularyItem> Vocabulary { get; }

    public ICommand RefreshCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand DeleteWordCommand { get; }

    public async Task LoadVocabularyAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
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
            IsBusy = false;
        }
    }

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
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting vocabulary: {ex.Message}");
        }
    }

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

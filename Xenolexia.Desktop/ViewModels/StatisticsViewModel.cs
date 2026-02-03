using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Desktop.ViewModels;

public partial class StatisticsViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private ReadingStats? _stats;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotLoading))]
    private bool _isLoading = true;

    public bool NotLoading => !IsLoading;

    public StatisticsViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Stats = await _storageService.GetReadingStatsAsync();
        }
        catch
        {
            Stats = new ReadingStats();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    public int TotalBooksRead => Stats?.TotalBooksRead ?? 0;
    public int TotalReadingTimeMinutes => (Stats?.TotalReadingTime ?? 0) / 60;
    public int TotalWordsLearned => Stats?.TotalWordsLearned ?? 0;
    public int CurrentStreak => Stats?.CurrentStreak ?? 0;
    public int LongestStreak => Stats?.LongestStreak ?? 0;
    public int WordsRevealedToday => Stats?.WordsRevealedToday ?? 0;
    public int WordsSavedToday => Stats?.WordsSavedToday ?? 0;
    public double AverageSessionDurationMinutes => (Stats?.AverageSessionDuration ?? 0) / 60.0;

    partial void OnStatsChanged(ReadingStats? value)
    {
        OnPropertyChanged(nameof(TotalBooksRead));
        OnPropertyChanged(nameof(TotalReadingTimeMinutes));
        OnPropertyChanged(nameof(TotalWordsLearned));
        OnPropertyChanged(nameof(CurrentStreak));
        OnPropertyChanged(nameof(LongestStreak));
        OnPropertyChanged(nameof(WordsRevealedToday));
        OnPropertyChanged(nameof(WordsSavedToday));
        OnPropertyChanged(nameof(AverageSessionDurationMinutes));
    }
}

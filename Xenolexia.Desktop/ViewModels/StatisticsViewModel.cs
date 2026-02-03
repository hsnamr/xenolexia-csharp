using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Desktop.ViewModels;

/// <summary>One bar in the "reading over time" chart.</summary>
public record WordsRevealedByDayItem(string DayLabel, DateTime Date, int WordsRevealed, double HeightPercent)
{
    /// <summary>Bar height in pixels (max 80).</summary>
    public double HeightPixels => Math.Max(2, 80 * HeightPercent / 100.0);
}

public partial class StatisticsViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;
    private const int ChartDays = 7;

    [ObservableProperty]
    private ReadingStats? _stats;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotLoading))]
    private bool _isLoading = true;

    /// <summary>Last 7 days for "reading over time" chart (oldest first).</summary>
    public ObservableCollection<WordsRevealedByDayItem> WordsRevealedByDay { get; } = new();

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
            var byDay = await _storageService.GetWordsRevealedByDayAsync(ChartDays);
            WordsRevealedByDay.Clear();
            var maxVal = byDay.Count > 0 ? Math.Max(1, byDay.Max(x => x.WordsRevealed)) : 1;
            foreach (var (date, count) in byDay)
            {
                var dayLabel = date.ToString("ddd", System.Globalization.CultureInfo.CurrentUICulture);
                var heightPct = maxVal > 0 ? (count * 100.0 / maxVal) : 0;
                WordsRevealedByDay.Add(new WordsRevealedByDayItem(dayLabel, date, count, heightPct));
            }
        }
        catch
        {
            Stats = new ReadingStats();
            WordsRevealedByDay.Clear();
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

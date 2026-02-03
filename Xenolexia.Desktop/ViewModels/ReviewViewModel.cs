using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Desktop.ViewModels;

public partial class ReviewViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;
    private const int DueLimit = 50;

    [ObservableProperty]
    private VocabularyItem? _currentItem;

    [ObservableProperty]
    private int _dueCount;

    [ObservableProperty]
    private bool _isFlipped;

    [ObservableProperty]
    private int _reviewedCount;

    [ObservableProperty]
    private bool _hasNoDue;

    private List<VocabularyItem> _dueList = new();
    private int _reviewedThisSession;

    public ReviewViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task LoadDueAsync()
    {
        HasNoDue = false;
        CurrentItem = null;
        IsFlipped = false;
        _reviewedThisSession = 0;
        ReviewedCount = 0;
        try
        {
            _dueList = (await _storageService.GetVocabularyDueForReviewAsync(DueLimit)).ToList();
            if (_dueList.Count > 0)
            {
                CurrentItem = _dueList[0];
                _dueList.RemoveAt(0);
                DueCount = _dueList.Count + 1;
            }
            else
            {
                DueCount = 0;
                HasNoDue = true;
            }
        }
        catch
        {
            DueCount = 0;
            HasNoDue = true;
        }
    }

    [RelayCommand]
    private void Flip()
    {
        IsFlipped = true;
    }

    [RelayCommand]
    private async Task GradeAsync(object? parameter)
    {
        if (CurrentItem == null) return;
        var quality = parameter switch
        {
            int i => i,
            string s when int.TryParse(s, out var n) => n,
            _ => 3
        };
        quality = Math.Clamp(quality, 0, 5);
        var itemId = CurrentItem.Id;
        try
        {
            await _storageService.RecordReviewAsync(itemId, quality);
        }
        catch
        {
            // Continue to advance
        }

        _reviewedThisSession++;
        ReviewedCount = _reviewedThisSession;

        // Advance to next card
        if (_dueList.Count > 0)
        {
            CurrentItem = _dueList[0];
            _dueList.RemoveAt(0);
            DueCount = _dueList.Count + 1; // +1 for current
            IsFlipped = false;
        }
        else
        {
            CurrentItem = null;
            DueCount = 0;
            IsFlipped = false;
            // Try to load more due
            var more = await _storageService.GetVocabularyDueForReviewAsync(DueLimit);
            if (more.Count > 0)
            {
                _dueList = more.ToList();
                CurrentItem = _dueList[0];
                _dueList.RemoveAt(0);
                DueCount = _dueList.Count + 1;
            }
            else
            {
                HasNoDue = true;
            }
        }
    }

    public bool HasCurrentCard => CurrentItem != null;
    public bool ShowFront => HasCurrentCard && !IsFlipped;
    public bool ShowGradeButtons => HasCurrentCard && IsFlipped;

    partial void OnCurrentItemChanged(VocabularyItem? value)
    {
        OnPropertyChanged(nameof(HasCurrentCard));
        OnPropertyChanged(nameof(ShowFront));
        OnPropertyChanged(nameof(ShowGradeButtons));
    }

    partial void OnIsFlippedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowFront));
        OnPropertyChanged(nameof(ShowGradeButtons));
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private Language _defaultSourceLanguage = Language.En;

    [ObservableProperty]
    private Language _defaultTargetLanguage = Language.Es;

    [ObservableProperty]
    private ProficiencyLevel _defaultProficiencyLevel = ProficiencyLevel.Beginner;

    [ObservableProperty]
    private double _defaultWordDensity = 0.3;

    [ObservableProperty]
    private ReaderTheme _readerTheme = ReaderTheme.Light;

    [ObservableProperty]
    private string _readerFontFamily = "System";

    [ObservableProperty]
    private double _readerFontSize = 16;

    [ObservableProperty]
    private double _readerLineHeight = 1.6;

    [ObservableProperty]
    private int _dailyGoal = 30;

    [ObservableProperty]
    private bool _notificationsEnabled;

    [ObservableProperty]
    private string? _saveMessage;

    [ObservableProperty]
    private bool _hasSaveMessage;

    public ObservableCollection<Language> Languages { get; } = new(
        Enum.GetValues<Language>().Cast<Language>().ToList());

    public ObservableCollection<ProficiencyLevel> ProficiencyLevels { get; } = new(
        Enum.GetValues<ProficiencyLevel>().Cast<ProficiencyLevel>().ToList());

    public ObservableCollection<ReaderTheme> ReaderThemes { get; } = new(
        Enum.GetValues<ReaderTheme>().Cast<ReaderTheme>().ToList());

    public SettingsViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task LoadAsync()
    {
        try
        {
            var prefs = await _storageService.GetPreferencesAsync();
            DefaultSourceLanguage = prefs.DefaultSourceLanguage;
            DefaultTargetLanguage = prefs.DefaultTargetLanguage;
            DefaultProficiencyLevel = prefs.DefaultProficiencyLevel;
            DefaultWordDensity = prefs.DefaultWordDensity;
            ReaderTheme = prefs.ReaderSettings.Theme;
            ReaderFontFamily = prefs.ReaderSettings.FontFamily ?? "System";
            ReaderFontSize = prefs.ReaderSettings.FontSize;
            ReaderLineHeight = prefs.ReaderSettings.LineHeight;
            DailyGoal = prefs.DailyGoal;
            NotificationsEnabled = prefs.NotificationsEnabled;
        }
        catch
        {
            // Keep defaults
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        HasSaveMessage = false;
        try
        {
            var existing = await _storageService.GetPreferencesAsync();
            var prefs = new UserPreferences
            {
                DefaultSourceLanguage = DefaultSourceLanguage,
                DefaultTargetLanguage = DefaultTargetLanguage,
                DefaultProficiencyLevel = DefaultProficiencyLevel,
                DefaultWordDensity = DefaultWordDensity,
                ReaderSettings = new ReaderSettings
                {
                    Theme = ReaderTheme,
                    FontFamily = ReaderFontFamily,
                    FontSize = ReaderFontSize,
                    LineHeight = ReaderLineHeight,
                    MarginHorizontal = existing.ReaderSettings.MarginHorizontal,
                    MarginVertical = existing.ReaderSettings.MarginVertical,
                    TextAlign = existing.ReaderSettings.TextAlign,
                    Brightness = existing.ReaderSettings.Brightness
                },
                DailyGoal = DailyGoal,
                NotificationsEnabled = NotificationsEnabled,
                HasCompletedOnboarding = existing.HasCompletedOnboarding
            };
            await _storageService.SavePreferencesAsync(prefs);
            SaveMessage = "Settings saved.";
            HasSaveMessage = true;
        }
        catch (Exception ex)
        {
            SaveMessage = "Failed to save: " + ex.Message;
            HasSaveMessage = true;
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        DefaultSourceLanguage = Language.En;
        DefaultTargetLanguage = Language.Es;
        DefaultProficiencyLevel = ProficiencyLevel.Beginner;
        DefaultWordDensity = 0.3;
        ReaderTheme = ReaderTheme.Light;
        ReaderFontFamily = "System";
        ReaderFontSize = 16;
        ReaderLineHeight = 1.6;
        DailyGoal = 30;
        NotificationsEnabled = false;
        HasSaveMessage = false;
    }
}

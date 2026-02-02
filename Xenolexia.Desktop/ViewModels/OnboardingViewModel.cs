using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;

namespace Xenolexia.Desktop.ViewModels;

public partial class OnboardingViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;
    private readonly Action _onComplete;

    public const int StepWelcome = 0;
    public const int StepSourceLang = 1;
    public const int StepTargetLang = 2;
    public const int StepProficiency = 3;
    public const int StepWordDensity = 4;
    public const int StepGetStarted = 5;
    public const int TotalSteps = 6;

    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private Language _defaultSourceLanguage = Language.En;

    [ObservableProperty]
    private Language _defaultTargetLanguage = Language.Es;

    [ObservableProperty]
    private ProficiencyLevel _defaultProficiencyLevel = ProficiencyLevel.Beginner;

    [ObservableProperty]
    private double _defaultWordDensity = 0.3;

    public ObservableCollection<Language> Languages { get; } = new(
        Enum.GetValues<Language>().Cast<Language>().ToList());

    public ObservableCollection<ProficiencyLevel> ProficiencyLevels { get; } = new(
        Enum.GetValues<ProficiencyLevel>().Cast<ProficiencyLevel>().ToList());

    public bool IsFirstStep => CurrentStep == 0;
    public bool ShowBackButton => !IsFirstStep;
    public bool IsLastStep => CurrentStep == StepGetStarted;
    public string NextButtonText => IsGetStartedStep ? "Get started" : "Next";
    public bool IsWelcomeStep => CurrentStep == StepWelcome;
    public bool IsGetStartedStep => CurrentStep == StepGetStarted;
    public bool IsStepSourceLang => CurrentStep == StepSourceLang;
    public bool IsStepTargetLang => CurrentStep == StepTargetLang;
    public bool IsStepProficiency => CurrentStep == StepProficiency;
    public bool IsStepWordDensity => CurrentStep == StepWordDensity;
    public string StepTitle => CurrentStep switch
    {
        StepWelcome => "Welcome",
        StepSourceLang => "Source language",
        StepTargetLang => "Target language",
        StepProficiency => "Proficiency",
        StepWordDensity => "Word density",
        StepGetStarted => "Get started",
        _ => ""
    };

    public OnboardingViewModel(IStorageService storageService, Action onComplete)
    {
        _storageService = storageService;
        _onComplete = onComplete;
    }

    [RelayCommand]
    private void Next()
    {
        if (CurrentStep < StepGetStarted)
            CurrentStep++;
        else
            _ = CompleteAsync();
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        await SaveAndCompleteAsync(new UserPreferences());
    }

    [RelayCommand]
    private async Task GetStartedAsync()
    {
        await CompleteAsync();
    }

    private async Task CompleteAsync()
    {
        var prefs = new UserPreferences
        {
            DefaultSourceLanguage = DefaultSourceLanguage,
            DefaultTargetLanguage = DefaultTargetLanguage,
            DefaultProficiencyLevel = DefaultProficiencyLevel,
            DefaultWordDensity = DefaultWordDensity,
            HasCompletedOnboarding = true,
            ReaderSettings = new ReaderSettings(),
            DailyGoal = 30,
            NotificationsEnabled = false
        };
        await SaveAndCompleteAsync(prefs);
    }

    private async Task SaveAndCompleteAsync(UserPreferences prefs)
    {
        prefs.HasCompletedOnboarding = true;
        try
        {
            await _storageService.SavePreferencesAsync(prefs);
        }
        catch
        {
            // Continue to close onboarding
        }
        _onComplete();
    }

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsFirstStep));
        OnPropertyChanged(nameof(ShowBackButton));
        OnPropertyChanged(nameof(IsLastStep));
        OnPropertyChanged(nameof(NextButtonText));
        OnPropertyChanged(nameof(IsWelcomeStep));
        OnPropertyChanged(nameof(IsGetStartedStep));
        OnPropertyChanged(nameof(IsStepSourceLang));
        OnPropertyChanged(nameof(IsStepTargetLang));
        OnPropertyChanged(nameof(IsStepProficiency));
        OnPropertyChanged(nameof(IsStepWordDensity));
        OnPropertyChanged(nameof(StepTitle));
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;
using Xenolexia.Desktop.Views;

namespace Xenolexia.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;
    private readonly IExportService _exportService;
    private readonly IBookParserService _bookParserService;
    private readonly ITranslationService _translationService;

    public MainWindowViewModel()
    {
        var serviceProvider = Program.ServiceProvider ?? throw new InvalidOperationException("Services not initialized");
        _storageService = (IStorageService)serviceProvider.GetService(typeof(IStorageService))!;
        _exportService = (IExportService)serviceProvider.GetService(typeof(IExportService))!;
        _bookParserService = (IBookParserService)serviceProvider.GetService(typeof(IBookParserService))!;
        _translationService = (ITranslationService)serviceProvider.GetService(typeof(ITranslationService))!;

        LibraryView = new LibraryView();
        VocabularyView = new VocabularyView();
        ReviewView = new ReviewView { DataContext = new ReviewViewModel(_storageService) };
        AboutView = new AboutView();
        SettingsView = new SettingsView { DataContext = new SettingsViewModel(_storageService) };
        StatisticsView = new StatisticsView { DataContext = new StatisticsViewModel(_storageService) };
        var onboardingVm = new OnboardingViewModel(_storageService, CompleteOnboarding);
        OnboardingView = new OnboardingView { DataContext = onboardingVm };

        BookNavigation.OpenBookRequested += OnOpenBookRequested;
        BookNavigation.RequestCloseReader += OnCloseReaderRequested;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTabs))]
    private bool _isReaderVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTabs))]
    private bool _showOnboarding = true;

    [ObservableProperty]
    private ReaderView? _readerView;

    public bool ShowTabs => !IsReaderVisible && !ShowOnboarding;

    public LibraryView LibraryView { get; }
    public VocabularyView VocabularyView { get; }
    public ReviewView ReviewView { get; }
    public SettingsView SettingsView { get; }
    public StatisticsView StatisticsView { get; }
    public AboutView AboutView { get; }
    public OnboardingView OnboardingView { get; }

    public async Task LoadAsync()
    {
        try
        {
            var prefs = await _storageService.GetPreferencesAsync();
            ShowOnboarding = !prefs.HasCompletedOnboarding;
        }
        catch
        {
            ShowOnboarding = true;
        }
    }

    private void CompleteOnboarding()
    {
        ShowOnboarding = false;
    }

    private async void OnOpenBookRequested(Book book)
    {
        var vm = new ReaderViewModel(book, _bookParserService, _translationService, _storageService);
        var view = new ReaderView { DataContext = vm };
        await vm.LoadAsync();
        ReaderView = view;
        IsReaderVisible = true;
    }

    private void OnCloseReaderRequested()
    {
        IsReaderVisible = false;
    }
}

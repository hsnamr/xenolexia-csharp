using System;
using System.Collections.ObjectModel;
using System.Linq;
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

    public MainWindowViewModel()
    {
        var serviceProvider = Program.ServiceProvider ?? throw new InvalidOperationException("Services not initialized");
        _storageService = (IStorageService)serviceProvider.GetService(typeof(IStorageService))!;
        _exportService = (IExportService)serviceProvider.GetService(typeof(IExportService))!;
        _bookParserService = (IBookParserService)serviceProvider.GetService(typeof(IBookParserService))!;

        LibraryView = new LibraryView();
        VocabularyView = new VocabularyView();
        AboutView = new AboutView();

        BookNavigation.OpenBookRequested += OnOpenBookRequested;
        BookNavigation.RequestCloseReader += OnCloseReaderRequested;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTabs))]
    private bool _isReaderVisible;

    [ObservableProperty]
    private ReaderView? _readerView;

    public bool ShowTabs => !IsReaderVisible;

    public LibraryView LibraryView { get; }
    public VocabularyView VocabularyView { get; }
    public AboutView AboutView { get; }

    private async void OnOpenBookRequested(Book book)
    {
        var vm = new ReaderViewModel(book, _bookParserService);
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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Xenolexia.Core.Models;
using Xenolexia.Core.Services;
using Xenolexia.Linux.Views;

namespace Xenolexia.Linux.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IStorageService _storageService;
    private readonly IExportService _exportService;

    public MainWindowViewModel()
    {
        var serviceProvider = Program.ServiceProvider ?? throw new InvalidOperationException("Services not initialized");
        _storageService = (IStorageService)serviceProvider.GetService(typeof(IStorageService))!;
        _exportService = (IExportService)serviceProvider.GetService(typeof(IExportService))!;

        LibraryView = new LibraryView();
        VocabularyView = new VocabularyView();
        AboutView = new AboutView();
    }

    public LibraryView LibraryView { get; }
    public VocabularyView VocabularyView { get; }
    public AboutView AboutView { get; }
}

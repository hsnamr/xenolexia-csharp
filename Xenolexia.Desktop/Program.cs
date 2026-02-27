using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Xenolexia.Core.Services;
using Xenolexia.Desktop.Services;
using Xenolexia.Desktop.ViewModels;
using Xenolexia.Desktop.Views;

namespace Xenolexia.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        // Initialize services before starting the app
        await InitializeServicesAsync();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    // Initialize services
    public static IServiceProvider? ServiceProvider { get; private set; }

    public static async Task InitializeServicesAsync()
    {
        var xenolexiaDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".xenolexia");
        var databasePath = Path.Combine(xenolexiaDir, "xenolexia.litedb");
        var dictionariesDir = Path.Combine(xenolexiaDir, "dictionaries");
        
        Directory.CreateDirectory(xenolexiaDir);
        Directory.CreateDirectory(dictionariesDir);

        var storageService = new LiteDbStorageService(databasePath);
        await storageService.InitializeAsync();

        var booksDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".xenolexia",
            "books");
        
        var coversDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".xenolexia",
            "covers");
        
        var exportDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".xenolexia",
            "exports");

        Directory.CreateDirectory(booksDir);
        Directory.CreateDirectory(coversDir);
        Directory.CreateDirectory(exportDir);

        var bookImportService = new BookImportService(
            booksDir,
            coversDir,
            storageService,
            new BookParserService(),
            new ImageProcessingService());

        TopLevel? GetMainWindow() =>
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        var filePickerService = new FilePickerService(GetMainWindow);

        var offlineDictionary = new OfflineDictionaryService(dictionariesDir);
        var replacementEngine = new ReplacementEngine(offlineDictionary);

        // Create service provider (simplified - in production use DI container)
        ServiceProvider = new XenolexiaServiceProvider(
            storageService,
            new TranslationService(),
            offlineDictionary,
            replacementEngine,
            new BookParserService(),
            new BookDownloadService(booksDir),
            new ImageProcessingService(),
            new ExportService(exportDir),
            bookImportService,
            filePickerService);
    }
}

// Simple service provider for dependency injection
public class XenolexiaServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public XenolexiaServiceProvider(
        IStorageService storageService,
        ITranslationService translationService,
        IOfflineDictionaryService offlineDictionaryService,
        ReplacementEngine replacementEngine,
        IBookParserService bookParserService,
        IBookDownloadService bookDownloadService,
        IImageProcessingService imageProcessingService,
        IExportService exportService,
        IBookImportService bookImportService,
        IFilePickerService filePickerService)
    {
        _services[typeof(IStorageService)] = storageService;
        _services[typeof(ITranslationService)] = translationService;
        _services[typeof(IOfflineDictionaryService)] = offlineDictionaryService;
        _services[typeof(ReplacementEngine)] = replacementEngine;
        _services[typeof(IBookParserService)] = bookParserService;
        _services[typeof(IBookDownloadService)] = bookDownloadService;
        _services[typeof(IImageProcessingService)] = imageProcessingService;
        _services[typeof(IExportService)] = exportService;
        _services[typeof(IBookImportService)] = bookImportService;
        _services[typeof(IFilePickerService)] = filePickerService;
    }

    public object? GetService(Type serviceType)
    {
        return _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}

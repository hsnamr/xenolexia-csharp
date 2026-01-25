using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Xenolexia.Core.Services;
using Xenolexia.Linux.ViewModels;
using Xenolexia.Linux.Views;

namespace Xenolexia.Linux;

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
        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".xenolexia",
            "xenolexia.db");
        
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var storageService = new StorageService(databasePath);
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

        // Create service provider (simplified - in production use DI container)
        ServiceProvider = new ServiceProvider(
            storageService,
            new TranslationService(),
            new BookParserService(),
            new BookDownloadService(booksDir),
            new ImageProcessingService(),
            new ExportService(exportDir));
    }
}

// Simple service provider for dependency injection
public class ServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public ServiceProvider(
        IStorageService storageService,
        ITranslationService translationService,
        IBookParserService bookParserService,
        IBookDownloadService bookDownloadService,
        IImageProcessingService imageProcessingService,
        IExportService exportService)
    {
        _services[typeof(IStorageService)] = storageService;
        _services[typeof(ITranslationService)] = translationService;
        _services[typeof(IBookParserService)] = bookParserService;
        _services[typeof(IBookDownloadService)] = bookDownloadService;
        _services[typeof(IImageProcessingService)] = imageProcessingService;
        _services[typeof(IExportService)] = exportService;
    }

    public object? GetService(Type serviceType)
    {
        return _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}

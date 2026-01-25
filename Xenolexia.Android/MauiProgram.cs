using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Xenolexia.Core.Services;

namespace Xenolexia.Android;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Register services
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "xenolexia.db");
        builder.Services.AddSingleton<IStorageService>(_ => new StorageService(databasePath));
        builder.Services.AddSingleton<ITranslationService, TranslationService>();
        builder.Services.AddSingleton<IBookParserService, BookParserService>();
        
        var booksDir = Path.Combine(FileSystem.AppDataDirectory, "books");
        var coversDir = Path.Combine(FileSystem.AppDataDirectory, "covers");
        var exportDir = Path.Combine(FileSystem.AppDataDirectory, "exports");
        
        builder.Services.AddSingleton<IBookDownloadService>(_ => new BookDownloadService(booksDir));
        builder.Services.AddSingleton<IImageProcessingService, ImageProcessingService>();
        builder.Services.AddSingleton<IExportService>(_ => new ExportService(exportDir));

        // Register ViewModels
        builder.Services.AddTransient<ViewModels.LibraryViewModel>();
        builder.Services.AddTransient<ViewModels.VocabularyViewModel>();

        // Register Views
        builder.Services.AddTransient<Views.LibraryPage>();
        builder.Services.AddTransient<Views.VocabularyPage>();
        builder.Services.AddTransient<Views.ReaderPage>();
        builder.Services.AddTransient<Views.SettingsPage>();

        return builder.Build();
    }
}

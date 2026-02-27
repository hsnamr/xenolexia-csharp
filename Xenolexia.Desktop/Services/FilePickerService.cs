using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Xenolexia.Desktop.Views;

namespace Xenolexia.Desktop.Services;

/// <summary>
/// Avalonia-based file picker for the desktop app.
/// Uses native file picker when available; falls back to Avalonia file browser when native fails (e.g. no DBus/GTK on Linux).
/// </summary>
public class FilePickerService : IFilePickerService
{
    private readonly Func<TopLevel?> _getTopLevel;

    public FilePickerService(Func<TopLevel?> getTopLevel)
    {
        _getTopLevel = getTopLevel;
    }

    public async Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFilter>? filters = null)
    {
        var topLevel = _getTopLevel();
        if (topLevel == null)
            return await ShowFallbackDialogAsync(topLevel, filters);

        if (topLevel.StorageProvider != null)
        {
            try
            {
                var avaloniaFilters = filters?.Select(f => new FilePickerFileType(f.Name)
                {
                    Patterns = f.Extensions.Select(e => e.StartsWith('.') ? '*' + e : "*." + e).ToList()
                }).ToList();

                var options = new FilePickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false,
                    FileTypeFilter = avaloniaFilters ?? new List<FilePickerFileType>
                    {
                        new("Ebooks")
                        {
                            Patterns = new[] { "*.epub", "*.pdf", "*.txt", "*.fb2" }
                        }
                    }
                };

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
                if (files.Count > 0)
                {
                    var path = files[0].TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path))
                        return path;
                }
                return null; // User cancelled
            }
            catch (InvalidOperationException)
            {
                // "Neither DBus nor GTK are available" - use Avalonia file browser
            }
        }

        return await ShowFallbackDialogAsync(topLevel, filters);
    }

    private static async Task<string?> ShowFallbackDialogAsync(TopLevel? owner, IReadOnlyList<FilePickerFilter>? filters)
    {
        var parentWindow = owner as Window
            ?? (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);
        if (parentWindow == null)
            return null;
        var extensions = filters?.SelectMany(f => f.Extensions)
            .Select(e => e.StartsWith('.') ? e : "." + e)
            .ToArray();
        if (extensions == null || extensions.Length == 0)
            extensions = new[] { ".epub", ".pdf", ".txt", ".fb2" };
        var dialog = new FileOpenDialog(extensions);
        await dialog.ShowDialog(parentWindow);
        return dialog.ResultPath;
    }
}

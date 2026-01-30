using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Xenolexia.Desktop.Services;

/// <summary>
/// Avalonia-based file picker for the desktop app.
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
        if (topLevel?.StorageProvider == null)
            return null;

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
                    Patterns = new[] { "*.epub", "*.pdf", "*.txt", "*.fb2", "*.mobi", "*.azw", "*.azw3" }
                }
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
            return null;

        var file = files[0];
        var path = file.TryGetLocalPath();
        return path;
    }
}

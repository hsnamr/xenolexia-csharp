using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xenolexia.Linux.Services;

/// <summary>
/// Platform file picker for opening files (e.g. ebook import).
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Opens a file picker and returns the selected file path, or null if cancelled.
    /// </summary>
    Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFilter>? filters = null);
}

public class FilePickerFilter
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<string> Extensions { get; set; } = Array.Empty<string>();
}

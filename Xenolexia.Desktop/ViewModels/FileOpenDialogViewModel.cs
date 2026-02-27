using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Xenolexia.Desktop.ViewModels;

public partial class FileOpenDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private FileSystemItem? _selectedItem;

    [ObservableProperty]
    private string? _selectedFilter;

    public ObservableCollection<FileSystemItem> Items { get; } = new();
    public ObservableCollection<string> FileTypeFilters { get; } = new();

    private readonly string[] _allowedExtensions;
    private string _filterPattern = "*";

    public bool CanGoUp => !string.IsNullOrEmpty(CurrentPath) && Directory.Exists(CurrentPath) &&
        new DirectoryInfo(CurrentPath).Parent != null;

    public bool CanOpen => SelectedItem != null && SelectedItem.IsFile;

    public FileOpenDialogViewModel(string[]? allowedExtensions = null)
    {
        _allowedExtensions = allowedExtensions ?? new[] { ".epub", ".pdf", ".txt", ".fb2" };
        FileTypeFilters.Add("All supported (*.epub, *.pdf, *.txt, *.fb2)");
        FileTypeFilters.Add("All files (*.*)");
        SelectedFilter = FileTypeFilters[0];
    }

    partial void OnCurrentPathChanged(string value)
    {
        RefreshItems();
        OnPropertyChanged(nameof(CanGoUp));
    }

    partial void OnSelectedItemChanged(FileSystemItem? value)
    {
        OnPropertyChanged(nameof(CanOpen));
    }

    partial void OnSelectedFilterChanged(string? value)
    {
        _filterPattern = value == "All files (*.*)" ? "*" : "supported";
        RefreshItems();
    }

    [RelayCommand]
    public void GoUp()
    {
        if (!CanGoUp) return;
        var parent = Directory.GetParent(CurrentPath);
        if (parent != null)
            CurrentPath = parent.FullName;
    }

    public void SetInitialPath(string path)
    {
        if (Directory.Exists(path))
            CurrentPath = path;
        else if (File.Exists(path))
            CurrentPath = Path.GetDirectoryName(path) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        else
            CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public void RefreshItems()
    {
        Items.Clear();
        if (string.IsNullOrEmpty(CurrentPath) || !Directory.Exists(CurrentPath))
            return;

        try
        {
            var dir = new DirectoryInfo(CurrentPath);
            foreach (var subDir in dir.GetDirectories().OrderBy(d => d.Name))
            {
                if (!subDir.Name.StartsWith('.'))
                    Items.Add(new FileSystemItem(subDir.FullName, subDir.Name, "📁", false));
            }
            foreach (var file in dir.GetFiles().OrderBy(f => f.Name))
            {
                var ext = file.Extension.ToLowerInvariant();
                var extWithDot = ext.StartsWith('.') ? ext : "." + ext;
                if (_filterPattern == "*" || _allowedExtensions.Contains(extWithDot))
                    Items.Add(new FileSystemItem(file.FullName, file.Name, "📄", true));
            }
        }
        catch (UnauthorizedAccessException) { }
    }
}

public class FileSystemItem
{
    public string FullPath { get; }
    public string Name { get; }
    public string Icon { get; }
    public bool IsFile { get; }

    public FileSystemItem(string fullPath, string name, string icon, bool isFile)
    {
        FullPath = fullPath;
        Name = name;
        Icon = icon;
        IsFile = isFile;
    }
}

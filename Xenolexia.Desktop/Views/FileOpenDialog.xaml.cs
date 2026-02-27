using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Xenolexia.Desktop.ViewModels;

namespace Xenolexia.Desktop.Views;

public partial class FileOpenDialog : Window
{
    private FileOpenDialogViewModel ViewModel => (FileOpenDialogViewModel)DataContext!;

    public string? ResultPath { get; private set; }

    public FileOpenDialog() : this(null) { }

    public FileOpenDialog(string[]? allowedExtensions)
    {
        InitializeComponent();
        DataContext = new FileOpenDialogViewModel(allowedExtensions);
        ViewModel.SetInitialPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnGoUpClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.GoUp();
    }

    private void OnGoClick(object? sender, RoutedEventArgs e)
    {
        var path = ViewModel.CurrentPath?.Trim();
        if (string.IsNullOrEmpty(path)) return;
        if (Directory.Exists(path))
            ViewModel.SetInitialPath(path);
    }

    private void OnItemDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var item = ViewModel.SelectedItem;
        if (item == null) return;
        if (item.IsFile)
        {
            ResultPath = item.FullPath;
            Close();
        }
        else
        {
            ViewModel.SetInitialPath(item.FullPath);
            ViewModel.RefreshItems();
        }
    }

    private void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        var item = ViewModel.SelectedItem;
        if (item != null && item.IsFile)
        {
            ResultPath = item.FullPath;
            Close();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

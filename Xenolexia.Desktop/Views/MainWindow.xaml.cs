using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Xenolexia.Desktop.ViewModels;

namespace Xenolexia.Desktop.Views;

public partial class MainWindow : Window
{
    private static readonly string WindowStatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".xenolexia", "window.json");

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        Opened -= OnOpened;
        RestoreWindowState();
        if (DataContext is MainWindowViewModel vm)
            await vm.LoadAsync();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        SaveWindowState();
    }

    private void RestoreWindowState()
    {
        try
        {
            var dir = Path.GetDirectoryName(WindowStatePath);
            if (string.IsNullOrEmpty(dir) || !File.Exists(WindowStatePath))
                return;
            var json = File.ReadAllText(WindowStatePath);
            var state = JsonSerializer.Deserialize<WindowStateDto>(json);
            if (state == null) return;
            if (state.X >= 0 && state.Y >= 0)
                Position = new PixelPoint(state.X, state.Y);
            if (state.Width > 0 && state.Height > 0)
            {
                Width = state.Width;
                Height = state.Height;
            }
            if (state.WindowState == 1)
                WindowState = WindowState.Maximized;
        }
        catch { /* ignore */ }
    }

    private void SaveWindowState()
    {
        try
        {
            var dir = Path.GetDirectoryName(WindowStatePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var state = new WindowStateDto
            {
                X = Position.X,
                Y = Position.Y,
                Width = Width,
                Height = Height,
                WindowState = WindowState == WindowState.Maximized ? 1 : 0
            };
            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(WindowStatePath, json);
        }
        catch { /* ignore */ }
    }

    private class WindowStateDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int WindowState { get; set; }
    }
}

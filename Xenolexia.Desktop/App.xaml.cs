using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Xenolexia.Desktop.Views;

namespace Xenolexia.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (XamlLoadException)
        {
            if (!TryLoadFromAvaloniaResource())
                throw;
        }
    }

    private bool TryLoadFromAvaloniaResource()
    {
        try
        {
            var uri = new Uri("avares://Xenolexia.Desktop/App.xaml");
            var baseUri = new Uri("avares://Xenolexia.Desktop/");
            var loaded = AvaloniaXamlLoader.Load(uri, baseUri);
            if (loaded is Application app)
            {
                Styles.AddRange(app.Styles);
                RequestedThemeVariant = app.RequestedThemeVariant;
                return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            SetupTrayIcon(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static TrayIcon? _trayIcon;

    private static void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainWindow = desktop.MainWindow;
        if (mainWindow == null) return;

        _trayIcon = new TrayIcon
        {
            ToolTipText = "Xenolexia"
        };

        var showHideItem = new NativeMenuItem("Show/Hide");
        showHideItem.Click += (_, _) =>
        {
            if (mainWindow.IsVisible)
            {
                mainWindow.Hide();
            }
            else
            {
                mainWindow.Show();
                mainWindow.Activate();
            }
        };

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) =>
        {
            desktop.Shutdown();
        };

        _trayIcon.Menu = new NativeMenu();
        _trayIcon.Menu.Items.Add(showHideItem);
        _trayIcon.Menu.Items.Add(new NativeMenuItemSeparator());
        _trayIcon.Menu.Items.Add(quitItem);
    }
}

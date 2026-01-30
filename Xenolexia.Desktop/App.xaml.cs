using System;
using Avalonia;
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
        }

        base.OnFrameworkInitializationCompleted();
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xenolexia.Desktop.ViewModels;

namespace Xenolexia.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

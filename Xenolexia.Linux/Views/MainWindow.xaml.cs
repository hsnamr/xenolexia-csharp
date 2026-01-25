using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xenolexia.Linux.ViewModels;

namespace Xenolexia.Linux.Views;

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

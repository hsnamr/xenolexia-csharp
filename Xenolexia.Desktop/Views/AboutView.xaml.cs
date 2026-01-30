using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Xenolexia.Desktop.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
        DataContext = new ViewModels.AboutViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

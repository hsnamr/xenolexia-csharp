using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Xenolexia.Desktop.Views;

public partial class ReaderView : UserControl
{
    public ReaderView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

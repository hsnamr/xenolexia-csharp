using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Xenolexia.Desktop.Views;

public partial class ReaderView : UserControl
{
    public ReaderView()
    {
        InitializeComponent();
        // Words-revealed count: when ToolTip opening API is available, add handler here
        // to call segment.NotifyRevealedCommand so session tracks hover-to-reveal count.
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

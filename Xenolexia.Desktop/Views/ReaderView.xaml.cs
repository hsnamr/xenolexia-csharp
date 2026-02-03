using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Xenolexia.Core.Models;

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

    private void OnForeignWordPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.DataContext is ReaderContentSegment segment &&
            segment.IsForeign && segment.NotifyRevealedCommand is ICommand cmd)
            cmd.Execute(null);
    }
}

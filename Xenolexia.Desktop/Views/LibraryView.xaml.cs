using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Xenolexia.Core.Models;
using Xenolexia.Desktop.ViewModels;

namespace Xenolexia.Desktop.Views;

public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
        DataContext = new LibraryViewModel();

        if (DataContext is LibraryViewModel vm)
        {
            vm.LoadBooksCommand.Execute(null);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BookCard_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is Book book && DataContext is LibraryViewModel vm)
        {
            vm.OpenBookCommand.Execute(book);
        }
    }

    private void OnlineSearchOverlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Close only when the dimmed overlay itself was clicked (not the dialog)
        if (sender is Border overlay && e.Source == overlay && DataContext is LibraryViewModel vm)
        {
            vm.HideOnlineSearchCommand.Execute(null);
        }
    }

    private void OnlineSearchDialog_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Prevent click inside dialog from closing the overlay
        e.Handled = true;
    }

    private void OnlineSearchOverlay_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is LibraryViewModel vm)
        {
            vm.HideOnlineSearchCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnlineSearchDialog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is LibraryViewModel vm)
        {
            vm.HideOnlineSearchCommand.Execute(null);
            e.Handled = true;
        }
    }
}

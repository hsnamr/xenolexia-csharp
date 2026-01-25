using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xenolexia.Linux.ViewModels;

namespace Xenolexia.Linux.Views;

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
}

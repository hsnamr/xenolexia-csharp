using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
}

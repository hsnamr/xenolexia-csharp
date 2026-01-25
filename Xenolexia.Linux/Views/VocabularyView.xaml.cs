using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Xenolexia.Linux.ViewModels;

namespace Xenolexia.Linux.Views;

public partial class VocabularyView : UserControl
{
    public VocabularyView()
    {
        InitializeComponent();
        DataContext = new VocabularyViewModel();
        
        if (DataContext is VocabularyViewModel vm)
        {
            vm.LoadVocabularyCommand.Execute(null);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Xenolexia.Desktop.Views;

public partial class OnboardingView : UserControl
{
    public OnboardingView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

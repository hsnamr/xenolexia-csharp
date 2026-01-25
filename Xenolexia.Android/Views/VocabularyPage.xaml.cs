using Xenolexia.Android.ViewModels;

namespace Xenolexia.Android.Views;

public partial class VocabularyPage : ContentPage
{
    public VocabularyPage(VocabularyViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is VocabularyViewModel vm)
        {
            await vm.LoadVocabularyAsync();
        }
    }
}

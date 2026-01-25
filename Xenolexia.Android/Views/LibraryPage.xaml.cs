using Xenolexia.Android.ViewModels;

namespace Xenolexia.Android.Views;

public partial class LibraryPage : ContentPage
{
    public LibraryPage(LibraryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LibraryViewModel vm)
        {
            await vm.LoadBooksAsync();
        }
    }
}

using FitApp.ViewModels;

namespace FitApp.Views;

public partial class AccountPage : ContentPage
{
    private readonly AccountViewModel _vm;

    public AccountPage(AccountViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///SettingsPage");
    }
}

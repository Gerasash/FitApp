// Файл MainPage — code-behind.
// Этап 0: навигация переехала в MainViewModel.
// Этап 1: подгрузка сводок в OnAppearing — после возврата с других
// экранов (добавил тренировку → главный экран) карточки обновляются.

using FitApp.ViewModels;

namespace FitApp.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}

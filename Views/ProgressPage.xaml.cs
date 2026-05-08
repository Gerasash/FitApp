using FitApp.Data;
using FitApp.ViewModels;

namespace FitApp.Views;

public partial class ProgressPage : ContentPage
{
    private readonly ProgressViewModel _viewModel;

    public ProgressPage(WorkoutDataBase database)
    {
        InitializeComponent();
        _viewModel = new ProgressViewModel(database);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadExercises();
    }
}
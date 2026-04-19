using FitApp.Data;
using FitApp.Models;
using FitApp.ViewModels;

namespace FitApp;

public partial class ExercisePage : ContentPage
{
    private readonly ExerciseSelectionViewModel _viewModel;

    public ExercisePage(WorkoutDataBase database)
    {
        InitializeComponent();
        _viewModel = new ExerciseSelectionViewModel(database);
        BindingContext = _viewModel;
    }

    public void SetCallback(Action<Exercise> callback)
    {
        _viewModel.SetCallback(callback);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        if (Navigation.ModalStack.Count > 0)
            await Navigation.PopModalAsync();
        else
            await Navigation.PopAsync();
    }
}

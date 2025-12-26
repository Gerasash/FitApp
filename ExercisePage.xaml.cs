using FitApp.Data;
using FitApp.Models;
using FitApp.ViewModels;

namespace FitApp;

public partial class ExercisePage : ContentPage
{
    public ExercisePage(Action<Exercise> exerciseSelected = null)
    {
        InitializeComponent();
        var database = new WorkoutDataBase();
        BindingContext = new ExerciseSelectionViewModel(database, exerciseSelected);
    }
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        if (Navigation.ModalStack.Count > 0)
            await Navigation.PopModalAsync();
        else
            await Navigation.PopAsync();
    }
}

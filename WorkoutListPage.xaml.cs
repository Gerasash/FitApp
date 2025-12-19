using FitApp.Data;
using FitApp.Models;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;

namespace FitApp;

public partial class WorkoutListPage : ContentPage
{
    private readonly WorkoutDataBase _database;
    private readonly WorkoutViewModel _viewModel;

    public WorkoutListPage()
    {

        // Создаём БД (вручную, т.к. DI не используешь)
        _database = new WorkoutDataBase();

        // Создаём ViewModel с БД
        _viewModel = new WorkoutViewModel(_database);
        BindingContext = _viewModel;
    }

    private async void OnWorkoutSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedWorkout = e.CurrentSelection.FirstOrDefault() as Workout;
        if (selectedWorkout == null)
            return;

        // Передаём БД и тренировку в WorkoutPage
        var workoutPage = new WorkoutPage(selectedWorkout, _database);
        await Navigation.PushAsync(workoutPage);

        ((CollectionView)sender).SelectedItem = null;
    }
}

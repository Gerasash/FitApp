namespace FitApp;
using FitApp.Models;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;

public partial class WorkoutListPage : ContentPage
{
    public WorkoutListPage()
    {
        InitializeComponent();
        BindingContext = new WorkoutViewModel();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Перезагружаем данные каждый раз при открытии страницы
        if (BindingContext is WorkoutViewModel vm)
        {
            await vm.LoadWorkoutsCommand.ExecuteAsync(null);
        }
    }

    private async void OnWorkoutSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedWorkout = e.CurrentSelection.FirstOrDefault() as Workout;
        if (selectedWorkout == null)
            return;

        var workoutPage = new WorkoutPage(selectedWorkout);
        await Navigation.PushAsync(workoutPage);

        ((CollectionView)sender).SelectedItem = null;
    }

}
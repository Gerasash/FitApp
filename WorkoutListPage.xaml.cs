namespace FitApp;

using FitApp.Data;
using FitApp.Models;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;

public partial class WorkoutListPage : ContentPage
{
    private readonly WorkoutDataBase _database;

    public WorkoutListPage(WorkoutDataBase database)
    {
        InitializeComponent();
        _database = database;
        BindingContext = new WorkoutViewModel(database);
    }

    private async void OnWorkoutSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Workout workout) return;

        var page = new WorkoutPage(_database);
        page.Init(workout);
        await Navigation.PushAsync(page);

        ((CollectionView)sender).SelectedItem = null;
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


}
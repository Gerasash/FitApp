namespace FitApp.Views;

using FitApp.Data;
using FitApp.Models;
using FitApp.Services;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;

public partial class WorkoutListPage : ContentPage
{
    private readonly WorkoutDataBase _database;
    private readonly AiService _aiService;

    public WorkoutListPage(WorkoutDataBase database, AiService aiService)
    {
        InitializeComponent();
        _database = database;
        _aiService = aiService;
        BindingContext = new WorkoutViewModel(database, aiService);
    }

    private async void OnWorkoutSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Workout workout) return;

        var page = new WorkoutPage(_database, _aiService);
        page.Init(workout);
        await Navigation.PushAsync(page);

        ((CollectionView)sender).SelectedItem = null;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // ������������� ������ ������ ��� ��� �������� ��������
        if (BindingContext is WorkoutViewModel vm)
        {
            await vm.LoadWorkoutsCommand.ExecuteAsync(null);
        }
    }


}
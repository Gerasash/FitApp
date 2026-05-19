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
    private readonly OnnxPredictionService _onnxService;
    private readonly WorkoutPlannerService _plannerService;

    public WorkoutListPage(WorkoutDataBase database, AiService aiService, OnnxPredictionService onnxService, WorkoutPlannerService plannerService)
    {
        InitializeComponent();
        _database = database;
        _aiService = aiService;
        _onnxService = onnxService;
        _plannerService = plannerService;
        BindingContext = new WorkoutViewModel(database, aiService, onnxService, plannerService);
    }

    private async void OnWorkoutTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Workout workout) return;

        var page = new WorkoutPage(_database, _aiService, _onnxService, _plannerService);
        page.Init(workout);
        await Navigation.PushAsync(page);
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
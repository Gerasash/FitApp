namespace FitApp.Views;
using Microsoft.Maui.Controls;
using SQLite;
using FitApp.Models;
using FitApp.ViewModels;
using System;
using static System.Net.Mime.MediaTypeNames;
using FitApp.Data;
using FitApp.Services;

public partial class WorkoutPage : ContentPage
{
    private WorkoutViewModel _viewModel;
    private readonly WorkoutDataBase _database;
    private readonly AiService _aiService;

    public WorkoutPage(WorkoutDataBase database, AiService aiService)
    {
        InitializeComponent();
        _database = database;
        _aiService = aiService;
    }

    public void Init(Workout workout)
    {
        _viewModel = new WorkoutViewModel(workout, _database, _aiService);
        BindingContext = _viewModel;
        Appearing += async (s, e) => await _viewModel.LoadExercisesForWorkout(workout.Id);
    }

    private async void ToModalPage(object? sender, EventArgs e)
    {
        var modalPage = new ExercisePage(_database);
        modalPage.SetCallback(async exercise =>
        {
            await _viewModel.AddExerciseToWorkout(exercise);
        });
        await Navigation.PushModalAsync(modalPage);
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }


}
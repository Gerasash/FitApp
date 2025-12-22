namespace FitApp;
using Microsoft.Maui.Controls;
using SQLite;
using FitApp.Models;
using FitApp.ViewModels;
using System;
using static System.Net.Mime.MediaTypeNames;

public partial class WorkoutPage : ContentPage
{
    private readonly WorkoutViewModel _viewModel;
    public WorkoutPage(Workout workout)
    {
        InitializeComponent();
        BindingContext = _viewModel = new WorkoutViewModel(workout);
        addExerciseButton.Clicked += ToModalPage;
    }
    private async void ToModalPage(object? sender, EventArgs e)
    {
        // Передаем callback в модалку
        var modalPage = new ExercisePage(exerciseSelected: async exercise =>
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
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

        //мертвая привязка если не будет работать
        //WorkoutDescriptionLabel.Text = $"Начало: {workout.StartTime}";
        //EditorWorkoutDescription.Text = workout.Description;
        //WorkoutNameLabel.Text = workout.Name;

        // Кнопка "Добавить упражнение"
        addExerciseButton.Clicked += ToModalPage;
    }
    private async void ToModalPage(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new AddExersciseModalPage());
    }
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    

}
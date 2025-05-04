namespace FitApp;
using Microsoft.Maui.Controls;
using SQLite;
using FitApp.Models;
using FitApp.ViewModels;
using System;

public partial class WorkoutPage : ContentPage
{

    private readonly WorkoutViewModel _viewModel;
    private  Workout _currentWorkout;
    public WorkoutPage(Workout workout, WorkoutViewModel viewModel)
    {
        InitializeComponent();
        _currentWorkout = workout;
        BindingContext = new WorkoutViewModel(_currentWorkout );

        //мертвая привязка если не будет работать
        //WorkoutDescriptionLabel.Text = $"Начало: {workout.StartTime}";
        //EditorWorkoutDescription.Text = workout.Description;
        //WorkoutNameLabel.Text = workout.Name;

        // Кнопка "Назад"
        backButton.Clicked += async (o, e) => await Navigation.PopAsync();
        // Кнопка "Добавить упражнение"
        addExerciseButton.Clicked += ToModalPage;
    }
    private async void ToModalPage(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new AddExersciseModalPage());
    }
    void PickerSelectedIndexChanged(object sender, EventArgs e)
    {
        WorkoutDescriptionLabel.Text = $"Вы выбрали: {WorkoutPicker.SelectedItem}";
    }

    private void EditorWorkoutDescription_TextChanged(object sender, TextChangedEventArgs e)
    {
        //_viewModel.SaveDescriptionAsync(_currentWorkout, e.NewTextValue);
    }
}
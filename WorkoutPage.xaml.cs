namespace FitApp;
using Microsoft.Maui.Controls;
using SQLite;
using FitApp.Models;
using FitApp.ViewModels;
public partial class WorkoutPage : ContentPage
{

    private readonly WorkoutViewModel _viewModel;
    private readonly Workout _currentWorkout;
    public WorkoutPage(Workout workout, WorkoutViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _currentWorkout = workout;

        //WorkoutNameLabel.Text = workout.Name;
        // Описание уже установлено в конструкторе ViewModel
        WorkoutDescriptionLabel.Text = $"Начало: {workout.StartTime}";
        //EditorWorkoutDescription.Text = workout.Description;

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
        _viewModel.SaveDescriptionAsync(_currentWorkout, e.NewTextValue);
    }
}
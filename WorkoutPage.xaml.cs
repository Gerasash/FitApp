namespace FitApp;
using Microsoft.Maui.Controls;
using System.Security.AccessControl;
using SQLite;
using System.Collections.ObjectModel;
using FitApp.Models;
public partial class WorkoutPage : ContentPage
{
    public WorkoutPage(Workout workout)
    {
        InitializeComponent();

        // Устанавливаем данные тренировки на странице
        WorkoutNameLabel.Text = workout.Name;
        WorkoutDescriptionLabel.Text = $"Начало: {workout.StartTime}";

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

}
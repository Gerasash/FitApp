using FitApp.Data;
using FitApp.Models;
using System.Collections.ObjectModel;

namespace FitApp;

public partial class ExercisePage : ContentPage
{
    private readonly WorkoutDataBase _database;
    private List<Exercise> _allExercises; // Полный список для фильтрации

    public ExercisePage()
    {
        InitializeComponent();
        _database = new WorkoutDataBase(); // Лучше через DI, но пока так
        LoadExercises();
    }

    private async void LoadExercises()
    {
        _allExercises = await _database.GetExercisesAsync();
        ExercisesList.ItemsSource = _allExercises;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            ExercisesList.ItemsSource = _allExercises;
        }
        else
        {
            ExercisesList.ItemsSource = _allExercises
                .Where(x => x.Name.ToLower().Contains(e.NewTextValue.ToLower()))
                .ToList();
        }
    }

    private async void OnAddExerciseClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var exercise = button.BindingContext as Exercise;

        if (exercise != null)
        {
            // Возвращаем результат назад
            // (В будущем здесь будет передача данных через MessagingCenter или QueryProperty)

            bool confirm = await DisplayAlert("Добавить?", $"Добавить {exercise.Name} в тренировку?", "Да", "Нет");
            if (confirm)
            {
                // Тут пока заглушка, позже свяжем с WorkoutPage
                await Navigation.PopAsync();
            }
        }
    }

    private async void OnCreateNewExerciseClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("Новое упражнение", "Введите название:");
        if (!string.IsNullOrWhiteSpace(result))
        {
            var newEx = new Exercise { Name = result, EquipmentType = 0 };
            await _database.SaveExerciseAsync(newEx);
            LoadExercises(); // Обновляем список
        }
    }
}

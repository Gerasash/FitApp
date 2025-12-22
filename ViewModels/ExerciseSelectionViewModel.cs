using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitApp.ViewModels;

public partial class ExerciseSelectionViewModel : ObservableObject
{
    private readonly WorkoutDataBase _database;
    private readonly Action<Exercise> _onExerciseSelected; // 🔥 Callback для передачи упражнения

    [ObservableProperty]
    private ObservableCollection<Exercise> exercises = new();

    [ObservableProperty]
    private List<Exercise> allExercises = new();

    [ObservableProperty]
    private string searchText = "";

    [RelayCommand]
    private async Task LoadExercises()
    {
        AllExercises = await _database.GetExercisesAsync();
        Exercises = new ObservableCollection<Exercise>(AllExercises);
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Exercises = new ObservableCollection<Exercise>(AllExercises);
        }
        else
        {
            var filtered = AllExercises
                .Where(x => x.Name.ToLower().Contains(value.ToLower()))
                .ToList();
            Exercises = new ObservableCollection<Exercise>(filtered);
        }
    }

    [RelayCommand]
    private async Task AddExercise(Exercise exercise)
    {
        bool confirm = await Application.Current.MainPage.DisplayAlert(
            "Добавить?",
            $"Добавить {exercise.Name} в тренировку?",
            "Да", "Нет");

        if (confirm)
        {
            _onExerciseSelected?.Invoke(exercise); // 🔥 Передаем упражнение родителю
        }
    }

    [RelayCommand]
    private async Task CreateNewExercise()
    {
        string result = await Application.Current.MainPage.DisplayPromptAsync(
            "Новое упражнение", "Введите название:");

        if (!string.IsNullOrWhiteSpace(result))
        {
            var newEx = new Exercise { Name = result };
            await _database.SaveExerciseAsync(newEx);
            await LoadExercises(); // Обновляем список
        }
    }

    public ExerciseSelectionViewModel(WorkoutDataBase database, Action<Exercise> onExerciseSelected)
    {
        _database = database;
        _onExerciseSelected = onExerciseSelected;
        Task.Run(async () => await LoadExercises());
    }
}

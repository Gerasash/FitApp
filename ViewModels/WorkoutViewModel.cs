using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using SQLite;
namespace FitApp.ViewModels
{
    public partial class WorkoutViewModel : ObservableObject
    {
        private readonly WorkoutDataBase _database;

        [ObservableProperty]
        private ObservableCollection<Workout> _workouts;
        [ObservableProperty]
        private Workout currentWorkout;
        [ObservableProperty]
        private string workoutName;

        [ObservableProperty]
        private string workoutDescription;

        [ObservableProperty]
        private DateTime workoutDate;
        
        [ObservableProperty]
        private List<MuscleGroup> selectedMuscleGroups = new();

        public WorkoutViewModel(WorkoutDataBase database)//DI
        {
            _database = new WorkoutDataBase();
            LoadWorkouts();
        }
        public WorkoutViewModel(Workout workout, WorkoutDataBase database)
        {
            _database = database;
            CurrentWorkout = workout;
            WorkoutName = workout.Name;
            WorkoutDescription = workout.Description;
            WorkoutDate = workout.StartTime;
        }
        [RelayCommand]
        private async Task LoadWorkouts()
        {
            try
            {
                var items = await _database.GetWorkouts();
                Workouts = new ObservableCollection<Workout>(items);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить тренировки: {ex.Message}", "OK");
            }
        }
        [RelayCommand]
        private async Task UpdateWorkout()
        {
            try
            {
                if (CurrentWorkout == null) return;

                // Обновляем данные текущей тренировки
                CurrentWorkout.Name = WorkoutName;
                CurrentWorkout.Description = WorkoutDescription;
                CurrentWorkout.StartTime = WorkoutDate;

                await _database.SaveWorkout(CurrentWorkout);

                // Удаляем старые связи
                var existingLinks = await _database.GetWorkoutMuscleGroupsForWorkoutAsync(CurrentWorkout.Id);
                foreach (var link in existingLinks)
                {
                    await _database.DeleteWorkoutMuscleGroupAsync(link);
                }

                // Добавляем новые связи
                foreach (var muscleGroup in SelectedMuscleGroups)
                {
                    var link = new WorkoutMuscleGroup(CurrentWorkout.Id, muscleGroup.Id);
                    await _database.SaveWorkoutMuscleGroupAsync(link);
                }

                await Shell.Current.DisplayAlert("Успех", "Тренировка обновлена", "OK");
                await LoadWorkouts();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось обновить тренировку: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task AddWorkout()
        {
            try
            {
                if (string.IsNullOrEmpty(WorkoutName))
                {
                    await Shell.Current.DisplayAlert("Ошибка", "Пожалуйста, введите название тренировки", "OK");
                    return;
                }

                var workout = new Workout
                {
                    Name = WorkoutName,
                    Description = WorkoutDescription,
                    StartTime = WorkoutDate,
                };

                await _database.SaveWorkout(workout);

                // Сохраняем связи между тренировкой и мышцами
                foreach (var muscleGroup in SelectedMuscleGroups)
                {
                    var link = new WorkoutMuscleGroup(workout.Id, muscleGroup.Id);
                    await _database.SaveWorkoutMuscleGroupAsync(link);
                }

                WorkoutName = string.Empty;
                WorkoutDescription = string.Empty;
                SelectedMuscleGroups.Clear();

                await Shell.Current.DisplayAlert("Успех", "Тренировка добавлена", "OK");
                await LoadWorkouts();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось сохранить тренировку: {ex.Message}", "OK");
            }
        }


        [RelayCommand]
        private async Task DeleteWorkout(Workout workout)
        {
            try
            {
                if (workout == null) return;

                // Удаляем связи с мышечными группами
                var links = await _database.GetWorkoutMuscleGroupsForWorkoutAsync(workout.Id);
                foreach (var link in links)
                {
                    await _database.DeleteWorkoutMuscleGroupAsync(link);
                }

                // Удаляем саму тренировку
                await _database.DeleteWorkout(workout);

                await Shell.Current.DisplayAlert("Успех", "Тренировка удалена", "OK");
                await LoadWorkouts();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось удалить тренировку: {ex.Message}", "OK");
            }
        }
        [RelayCommand]
        private async Task LoadWorkoutWithMuscleGroups(int workoutId)
        {
            try
            {
                CurrentWorkout = await _database.GetItemAsync(workoutId);
                var muscleGroups = await _database.GetMuscleGroupsForWorkoutAsync(workoutId);
                CurrentWorkout.MuscleGroups = muscleGroups;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", $"Не удалось загрузить детали тренировки: {ex.Message}", "OK");
            }
        }
        /*public class WorkoutTagPair
        {
            public Workout Workout { get; set; }
            public MuscleGroup MuscleGroup { get; set; }
        }*/
        /*public async Task SaveDescriptionAsync(Workout workout, string newDescreption)
        {
            workout.Description = newDescreption;
            await _database.SaveWorkout(workout);
        }*/
    }
}

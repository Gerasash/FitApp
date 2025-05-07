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
        private ObservableCollection<Workout> workouts;
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

        public WorkoutViewModel()
        {
            _database = new WorkoutDataBase();
            LoadWorkouts();
        }
        
        public WorkoutViewModel(Workout workout)
        {
            _database = new WorkoutDataBase();
            CurrentWorkout = workout;
            WorkoutName = workout.Name;
            WorkoutDescription = workout.Description;
        }
        [RelayCommand]
        private async void LoadWorkouts()
        {
            var items = await _database.GetWorkouts();
            Workouts = new ObservableCollection<Workout>(items);
        }
        [RelayCommand]
        private async Task UpdateWorkout()
        {
            if (CurrentWorkout == null) return;

            // Обновляем данные текущей тренировки
            CurrentWorkout.Name = WorkoutName;
            CurrentWorkout.Description = WorkoutDescription;
            CurrentWorkout.StartTime = WorkoutDate;
            CurrentWorkout.MuscleGroups = SelectedMuscleGroups;

            await _database.SaveWorkout(CurrentWorkout);
            await Shell.Current.DisplayAlert("Успех", "Тренировка обновлена", "OK");

            // Обновляем список
            LoadWorkouts();
        }
        [RelayCommand]
        private async void AddWorkout()
        {
            if (!string.IsNullOrEmpty(WorkoutName))
            {
                var workout = new Workout
                {
                    Name = WorkoutName,
                    Description = WorkoutDescription,
                    StartTime = WorkoutDate,
                    MuscleGroups = SelectedMuscleGroups
                };
                await _database.SaveWorkout(workout);
                WorkoutName = string.Empty;
                SelectedMuscleGroups.Clear();
                LoadWorkouts();
            }
        }

        [RelayCommand]
        private async void DeleteWorkout(Workout workout)
        {
            await _database.DeleteWorkout(workout);
            LoadWorkouts();
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

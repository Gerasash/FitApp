using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
namespace FitApp.ViewModels
{
    public partial class WorkoutViewModel : ObservableObject
    {
        private readonly WorkoutDataBase _database;
        [ObservableProperty]
        private ObservableCollection<Workout> workouts;

        [ObservableProperty]
        private string newWorkoutName;

        [ObservableProperty]
        private DateTime newWorkoutDate = DateTime.Now;
        [ObservableProperty]
        private string newWorkoutDescription;

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
            NewWorkoutName = workout.Name;
            NewWorkoutDescription = workout.Description;
        }
        [RelayCommand]
        private async void LoadWorkouts()
        {
            var items = await _database.GetWorkouts();
            Workouts = new ObservableCollection<Workout>(items);
        }

        [RelayCommand]
        private async void AddWorkout()
        {
            if (!string.IsNullOrEmpty(NewWorkoutName))
            {
                var workout = new Workout
                {
                    Name = NewWorkoutName,
                    //Description = NewWorkoutDescription,
                    StartTime = NewWorkoutDate,
                    MuscleGroups = SelectedMuscleGroups
                };

                await _database.SaveWorkout(workout);
                NewWorkoutName = string.Empty;
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

        public class WorkoutTagPair
        {
            public Workout Workout { get; set; }
            public MuscleGroup MuscleGroup { get; set; }
        }
        public async Task SaveDescriptionAsync(Workout workout, string newDescreption)
        {
            workout.Description = newDescreption;
            await _database.SaveWorkout(workout);
        }
        
    }
}

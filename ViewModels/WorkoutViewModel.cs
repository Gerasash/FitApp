using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        private List<MuscleGroup> selectedMuscleGroups = new();

        public WorkoutViewModel()
        {
            _database = new WorkoutDataBase();
            LoadWorkouts();
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
        
    
    }
}

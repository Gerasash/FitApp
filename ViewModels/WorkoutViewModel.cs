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
        private ObservableCollection<MuscleGroup> allMuscleGroups = new();
        [ObservableProperty]
        private IList<object> selectedMuscleGroups = new List<object>();
        [ObservableProperty]
        private ObservableCollection<Workout> _workouts;
        [ObservableProperty]
        private Workout currentWorkout;
        [ObservableProperty]
        private string workoutName;

        [ObservableProperty]
        private string workoutDescription;
        // 🔥 ДОБАВЬ ЭТО ПОЛЕ:
        [ObservableProperty]
        private ObservableCollection<WorkoutExercise> workoutExercises = new();
        [ObservableProperty]
        private DateTime workoutDate;
        [ObservableProperty]
        private DateTime selectedDate = DateTime.Now;

        [ObservableProperty]
        private TimeSpan selectedTime = DateTime.Now.TimeOfDay;

        public WorkoutViewModel()
        {
            _database = new WorkoutDataBase();
            LoadWorkouts();
            LoadAllMuscleGroups(); // Загружаем группы мышц при старте
        }
        
        public WorkoutViewModel(Workout workout)
        {
            _database = new WorkoutDataBase();
            CurrentWorkout = workout;
            WorkoutName = workout.Name;
            WorkoutDescription = workout.Description;
            WorkoutDate = workout.StartTime;
            Task.Run(async () =>
            {
                await LoadAllMuscleGroups();
                // 2. Загружаем УЖЕ ВЫБРАННЫЕ мышцы для этой тренировки
                await LoadSelectedMuscleGroupsForWorkout(workout.Id);
                await LoadExercisesForWorkout(workout.Id); // ← Добавь эту строку
            });
            //TODO: Здесь нужно будет загрузить связанные группы мышц для редактирования
        }

        [RelayCommand]
        private async Task LoadWorkouts()
        {
            var items = await _database.GetWorkouts();
            Workouts = new ObservableCollection<Workout>(items);
        }
        public async Task LoadExercisesForWorkout(int workoutId)
        {
            var list = await _database.GetExercisesForWorkoutAsync(workoutId);
            System.Diagnostics.Debug.WriteLine($" Загружено упражнений: {list.Count} для workoutId={workoutId}");
            WorkoutExercises = new ObservableCollection<WorkoutExercise>(list);
        }
        //Метод загрузки всех групп мышц
        [RelayCommand]
        public async Task LoadAllMuscleGroups()
        {
            var groups = await _database.GetMuscleGroupsAsync();
            AllMuscleGroups = new ObservableCollection<MuscleGroup>(groups);
        }
        public async Task AddExerciseToWorkout(Exercise exercise)
        {
            if (CurrentWorkout == null) return;

            var newLink = new WorkoutExercise
            {
                WorkoutId = CurrentWorkout.Id,
                ExerciseId = exercise.Id,
                ExerciseName = exercise.Name, // Для отображения
                OrderIndex = WorkoutExercises.Count + 1
            };

            // 1. Сохраняем в БД
            await _database.AddExerciseToWorkoutAsync(newLink);

            // 2. Добавляем в UI список
            WorkoutExercises.Add(newLink);
        }
        [RelayCommand]
        private async Task UpdateWorkout()
        {
            if (CurrentWorkout == null) return;

            // 1. Обновляем основную тренировку
            CurrentWorkout.Name = WorkoutName;
            CurrentWorkout.Description = WorkoutDescription;
            CurrentWorkout.StartTime = WorkoutDate;
            await _database.SaveWorkout(CurrentWorkout);

            // 2. УДАЛЯЕМ СТАРЫЕ СВЯЗИ
            var oldLinks = await _database.GetWorkoutMuscleGroupsForWorkoutAsync(CurrentWorkout.Id);
            foreach (var link in oldLinks)
            {
                await _database.DeleteWorkoutMuscleGroupAsync(link);
            }

            // 3. СОХРАНЯЕМ НОВЫЕ СВЯЗИ
            if (SelectedMuscleGroups != null)
            {
                foreach (var item in SelectedMuscleGroups)
                {
                    if (item is MuscleGroup mg)
                    {
                        var newLink = new WorkoutMuscleGroup(CurrentWorkout.Id, mg.Id);
                        await _database.SaveWorkoutMuscleGroupAsync(newLink);
                    }
                }
            }

            await Shell.Current.DisplayAlert("Успех", "Тренировка обновлена", "OK");
            await LoadWorkouts();
        }


        [RelayCommand]
        private async Task AddWorkout()  // Task вместо void для async
        {
            if (!string.IsNullOrEmpty(WorkoutName))
            {
                // Объединяем дату и время
                var combinedDateTime = SelectedDate.Date.Add(SelectedTime);

                var workout = new Workout
                {
                    Name = WorkoutName,
                    Description = WorkoutDescription ?? "",  // Защита от null
                    StartTime = combinedDateTime
                };
                // 1. Сохраняем саму тренировку (получаем ID)
                await _database.SaveWorkout(workout);
                // Сохраняем связи с группами мышц
                if (SelectedMuscleGroups != null && SelectedMuscleGroups.Count > 0)
                {
                    foreach (var item in SelectedMuscleGroups)
                    {
                        if (item is MuscleGroup mg)
                        {
                            var link = new WorkoutMuscleGroup(workout.Id, mg.Id);
                            await _database.SaveWorkoutMuscleGroupAsync(link);
                        }
                    }
                }
                // Сброс полей
                WorkoutName = string.Empty;
                WorkoutDescription = string.Empty;
                SelectedDate = DateTime.Today;
                SelectedTime = DateTime.Now.TimeOfDay;
                SelectedMuscleGroups.Clear();

                await LoadWorkouts();
            }
        }

        [RelayCommand]
        private async void DeleteWorkout(Workout workout)
        {
            await _database.DeleteWorkout(workout);
            LoadWorkouts();
        }
        public async Task LoadSelectedMuscleGroupsForWorkout(int workoutId)
        {
            SelectedMuscleGroups.Clear();

            // 1. Получаем связи (WorkoutId - MuscleGroupId)
            var links = await _database.GetWorkoutMuscleGroupsForWorkoutAsync(workoutId);

            // 2. Для каждой связи ищем СУЩЕСТВУЮЩИЙ объект MuscleGroup в AllMuscleGroups
            foreach (var link in links)
            {
                var mg = AllMuscleGroups.FirstOrDefault(x => x.Id == link.MuscleGroupId);
                if (mg != null)
                {
                    SelectedMuscleGroups.Add(mg);
                }
            }

            OnPropertyChanged(nameof(SelectedMuscleGroups));
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

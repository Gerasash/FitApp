using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using SQLite;
namespace FitApp.ViewModels;

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
    private DateTime filterDate = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<Workout> filteredWorkouts;

    [ObservableProperty]
    private MuscleGroup selectedFilterMuscleGroup; // выбранная мышца для фильтра

    [ObservableProperty]
    private string workoutDescription;

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
            await LoadExercisesForWorkout(workout.Id);
        });
    }

    [RelayCommand]
    private async Task LoadWorkouts()
    {
        var items = await _database.GetWorkouts();
        Workouts = new ObservableCollection<Workout>(items);
        FilteredWorkouts = Workouts;
        //FilterByDate(); // Автоматически фильтрует по сегодняшней дате
    }
    [RelayCommand]
    private async Task AddSet(WorkoutExercise workoutExercise)
    {
        if (workoutExercise == null) return;

        // Простой ввод через диалоги (быстро внедрить)
        var wText = await Shell.Current.DisplayPromptAsync("Вес", "Введите вес (кг):", keyboard: Keyboard.Numeric);
        if (!double.TryParse(wText, out var weight)) return;

        var rText = await Shell.Current.DisplayPromptAsync("Повторы", "Введите количество повторений:", keyboard: Keyboard.Numeric);
        if (!int.TryParse(rText, out var reps)) return;

        var rpeText = await Shell.Current.DisplayPromptAsync("RPE", "Введите RPE (например 7.5):", keyboard: Keyboard.Numeric);
        if (!double.TryParse(rpeText, out var rpe)) rpe = 0;

        // Следующий номер подхода
        var next = (workoutExercise.Sets?.Count ?? 0) + 1;

        var set = new ExerciseSet
        {
            WorkoutExerciseId = workoutExercise.Id,
            SetNumber = next,
            Weight = weight,
            Reps = reps,
            RPE = rpe
        };

        await _database.SaveSetAsync(set);

        // Обновляем UI (важно, чтобы Sets не был null)
        workoutExercise.Sets ??= new List<ExerciseSet>();
        workoutExercise.Sets.Add(set);

        await LoadExercisesForWorkout(CurrentWorkout.Id);

        // Если не обновляется вложенный CollectionView:
        OnPropertyChanged(nameof(WorkoutExercises));
    }

    [RelayCommand]
    private void FilterByDate()
    {
        if (Workouts == null)
        {
            FilteredWorkouts = new ObservableCollection<Workout>();
            return;
        }

        var selectedDate = FilterDate.Date;
        var filteredList = Workouts
            .Where(w => w.StartTime.Date == selectedDate)
            .OrderByDescending(w => w.StartTime)
            .ToList();

        FilteredWorkouts = new ObservableCollection<Workout>(filteredList);

        //DEBUG - смотри в Output
        System.Diagnostics.Debug.WriteLine($" Отфильтровано {FilteredWorkouts.Count} тренировок за {selectedDate:dd.MM.yyyy}");
    }
    [RelayCommand]
    private void ShowAllWorkouts()
    {
        FilterDate = DateTime.Today; // Сбрасываем дату на сегодня
        FilteredWorkouts = Workouts; // Показываем ВСЕ тренировки

        System.Diagnostics.Debug.WriteLine("🔍 Показаны ВСЕ тренировки");
    }
    public async Task LoadExercisesForWorkout(int workoutId)
    {
        var list = await _database.GetExercisesForWorkoutAsync(workoutId);
        System.Diagnostics.Debug.WriteLine($" Загружено упражнений: {list.Count} для workoutId={workoutId}");
        WorkoutExercises = new ObservableCollection<WorkoutExercise>(list);
    }

    [RelayCommand]
    private async Task FilterByMuscleGroup()
    {
        if (SelectedFilterMuscleGroup == null)
        {
            // если не выбрано — показываем всё
            FilteredWorkouts = Workouts;
            return;
        }

        var list = await _database.GetWorkoutsByMuscleGroupAsync(SelectedFilterMuscleGroup.Id);
        FilteredWorkouts = new ObservableCollection<Workout>(list);
    }

    [RelayCommand]
    private async Task DeleteWorkoutExercise(WorkoutExercise we)
    {
        if (we == null) return;

        // 1) удалить подходы этого упражнения
        if (we.Sets != null)
            foreach (var s in we.Sets)
                await _database.DeleteSetAsync(s);

        // 2) удалить сам WorkoutExercise из БД
        await _database.DeleteWorkoutExerciseAsync(we);

        // 3) обновить UI (самый надежный способ)
        await LoadExercisesForWorkout(CurrentWorkout.Id);
    }
    [RelayCommand]
    private async Task DeleteSet(ExerciseSet set)
    {
        if (set == null) return;

        await _database.DeleteSetAsync(set);
        await LoadExercisesForWorkout(CurrentWorkout.Id); // чтобы сразу обновилось
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

    partial void OnFilterDateChanged(DateTime value)
    {
        FilterByDate(); // Автоматически фильтрует при смене даты!
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
}

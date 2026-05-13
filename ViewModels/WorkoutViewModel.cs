using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using FitApp.Services;
using SQLite;
namespace FitApp.ViewModels;

public partial class WorkoutViewModel : ObservableObject
{
    private readonly WorkoutDataBase _database;
    private readonly AiService _aiService;

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

    // ====== Bottom Sheet для ввода подхода ======
    [ObservableProperty] private bool isSetSheetOpen;
    [ObservableProperty] private string sheetTitle = "Новый подход";
    [ObservableProperty] private double sheetWeight;
    [ObservableProperty] private int sheetReps;
    [ObservableProperty] private double sheetRpe;
    [ObservableProperty] private string sheetRpeTitle = "";
    [ObservableProperty] private string sheetRpeHint = "";
    private WorkoutExercise? _sheetExercise;
    private ExerciseSet? _editingSet;

    // Фиксированная Hevy-шкала RPE
    public IReadOnlyList<RpeOption> RpeOptions { get; } = new[]
    {
        new RpeOption(6.0, "6"),
        new RpeOption(7.0, "7"),
        new RpeOption(7.5, "7.5"),
        new RpeOption(8.0, "8"),
        new RpeOption(8.5, "8.5"),
        new RpeOption(9.0, "9"),
        new RpeOption(9.5, "9.5"),
        new RpeOption(10.0, "10"),
    };

    partial void OnSheetRpeChanged(double value)
    {
        // Обновляем выделение чипа
        foreach (var opt in RpeOptions)
            opt.IsSelected = Math.Abs(opt.Value - value) < 0.001;

        // Текстовое описание RPE — как в Hevy
        (SheetRpeTitle, SheetRpeHint) = value switch
        {
            <= 6.0 => ("Легко", "Могли бы сделать ещё 4+ повторений"),
            <= 7.0 => ("Умеренно", "Могли бы сделать ещё 3 повторения"),
            <= 7.5 => ("Умеренно-тяжело", "Могли бы сделать ещё 2-3 повторения"),
            <= 8.0 => ("Очень тяжёлое усилие", "Могли бы точно сделать ещё 2 повторения"),
            <= 8.5 => ("Тяжело", "Могли бы сделать ещё 1-2 повторения"),
            <= 9.0 => ("Очень тяжело", "Могли бы сделать ещё 1 повторение"),
            <= 9.5 => ("Почти максимум", "Возможно, ещё 1 повторение"),
            _      => ("Максимум", "Больше повторений уже не сделать")
        };
    }

    [RelayCommand]
    private void SelectRpe(double value) => SheetRpe = value;

    public WorkoutViewModel(WorkoutDataBase database, AiService aiService)
    {
        _database = database;
        _aiService = aiService;
        LoadWorkouts();
        LoadAllMuscleGroups();
    }

    public WorkoutViewModel(Workout workout, WorkoutDataBase database, AiService aiService)
    {
        _database = database;
        _aiService = aiService;
        CurrentWorkout = workout;
        WorkoutName = workout.Name;
        WorkoutDescription = workout.Description;
        WorkoutDate = workout.StartTime;
        Task.Run(async () =>
        {
            await LoadAllMuscleGroups();
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
    // Открытие Bottom Sheet для нового подхода
    [RelayCommand]
    private void AddSet(WorkoutExercise workoutExercise)
    {
        if (workoutExercise == null) return;

        _sheetExercise = workoutExercise;
        _editingSet = null;
        SheetTitle = "Новый подход";

        // Префилл из последнего подхода этого упражнения
        var last = workoutExercise.Sets?.LastOrDefault();
        SheetWeight = last?.Weight ?? 20;
        SheetReps = last?.Reps ?? 8;
        SheetRpe = last?.RPE ?? 7;

        IsSetSheetOpen = true;
    }

    // Открытие Bottom Sheet для редактирования подхода
    // Меняем тип подхода через ActionSheet
    [RelayCommand]
    private async Task ChangeSetType(ExerciseSet set)
    {
        if (set == null) return;
        const string warmup = "W  Разминка";
        const string normal = "1  Нормальный";
        const string failure = "F  В отказ";
        const string drop = "D  Дроп-сет";

        var choice = await Shell.Current.DisplayActionSheet(
            "Тип подхода", "Отмена", null,
            warmup, normal, failure, drop);
        if (string.IsNullOrEmpty(choice) || choice == "Отмена") return;

        set.Kind = choice switch
        {
            warmup => SetType.Warmup,
            failure => SetType.Failure,
            drop => SetType.DropSet,
            _ => SetType.Normal
        };
        await _database.SaveSetAsync(set);
        if (CurrentWorkout != null)
            await LoadExercisesForWorkout(CurrentWorkout.Id);
    }

    [RelayCommand]
    private void EditSet(ExerciseSet set)
    {
        if (set == null) return;

        var parent = WorkoutExercises.FirstOrDefault(we => we.Id == set.WorkoutExerciseId);
        if (parent == null) return;

        _sheetExercise = parent;
        _editingSet = set;
        SheetTitle = $"Подход #{set.SetNumber}";
        SheetWeight = set.Weight;
        SheetReps = set.Reps;
        SheetRpe = set.RPE;

        IsSetSheetOpen = true;
    }

    [RelayCommand]
    private void CancelSheet() => IsSetSheetOpen = false;

    [RelayCommand]
    private void AdjustWeight(string delta)
    {
        if (double.TryParse(delta, System.Globalization.CultureInfo.InvariantCulture, out var d))
            SheetWeight = Math.Max(0, SheetWeight + d);
    }

    [RelayCommand]
    private void AdjustReps(string delta)
    {
        if (int.TryParse(delta, out var d))
            SheetReps = Math.Max(0, SheetReps + d);
    }

    [RelayCommand]
    private async Task SaveSheet()
    {
        if (_sheetExercise == null) return;

        if (_editingSet != null)
        {
            _editingSet.Weight = SheetWeight;
            _editingSet.Reps = SheetReps;
            _editingSet.RPE = SheetRpe;
            await _database.SaveSetAsync(_editingSet);
        }
        else
        {
            var next = (_sheetExercise.Sets?.Count ?? 0) + 1;
            var set = new ExerciseSet
            {
                WorkoutExerciseId = _sheetExercise.Id,
                SetNumber = next,
                Weight = SheetWeight,
                Reps = SheetReps,
                RPE = SheetRpe
            };
            await _database.SaveSetAsync(set);
            _sheetExercise.Sets ??= new List<ExerciseSet>();
            _sheetExercise.Sets.Add(set);
        }

        IsSetSheetOpen = false;
        await LoadExercisesForWorkout(CurrentWorkout.Id);
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

        // Запрашиваем предсказания на основе ИСТОРИИ прошлых тренировок
        var tasks = list.Select(async we =>
        {
            var history = await _database.GetSetHistoryForExerciseAsync(we.ExerciseId);
            System.Diagnostics.Debug.WriteLine($"[AI] ExerciseId={we.ExerciseId} history={history.Count}");
            var result = await _aiService.PredictAsync(we.ExerciseId, history);
            System.Diagnostics.Debug.WriteLine($"[AI] result={result?.text ?? "null"}");
            if (result != null)
                we.AiInsight = $"💡 {result.text}";
        });
        await Task.WhenAll(tasks);

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

        // 3. Подтягиваем мышцы упражнения в тренировку (без дублей)
        await AttachExerciseMusclesToWorkoutAsync(exercise.Id);
    }

    // Линкуем все мышцы упражнения к текущей тренировке, пропуская уже существующие связи.
    // Это и кладёт строки в WorkoutMuscleGroup (для фильтра по мышцам), и обновляет UI-чипы.
    private async Task AttachExerciseMusclesToWorkoutAsync(int exerciseId)
    {
        if (CurrentWorkout == null) return;

        var exMuscles = await _database.GetExerciseMusclesAsync(exerciseId);
        if (exMuscles.Count == 0) return;

        var existingLinks = await _database.GetWorkoutMuscleGroupsForWorkoutAsync(CurrentWorkout.Id);
        var existingIds = new HashSet<int>(existingLinks.Select(l => l.MuscleGroupId));

        var addedAny = false;
        foreach (var em in exMuscles)
        {
            if (existingIds.Contains(em.MuscleGroupId)) continue;
            await _database.SaveWorkoutMuscleGroupAsync(
                new WorkoutMuscleGroup(CurrentWorkout.Id, em.MuscleGroupId));
            existingIds.Add(em.MuscleGroupId);
            addedAny = true;
        }

        if (addedAny)
        {
            // Перезагружаем выбранные мышцы для текущей тренировки, чтобы UI обновился
            await LoadSelectedMuscleGroupsForWorkout(CurrentWorkout.Id);
        }
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
    private async Task DeleteWorkout(Workout workout)
    {
        if (workout == null) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Удалить тренировку?",
            $"«{workout.Name}» от {workout.StartTime:dd.MM.yyyy} будет удалена вместе со всеми подходами. Действие необратимо.",
            "Удалить",
            "Отмена");
        if (!confirm) return;

        await _database.DeleteWorkout(workout);
        await LoadWorkouts();
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

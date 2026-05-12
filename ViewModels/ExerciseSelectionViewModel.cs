using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FitApp.ViewModels;

// Карточка для CollectionView: упражнение + готовый подзаголовок и список мышечных ID для фильтра
public partial class ExerciseRow : ObservableObject
{
    public Exercise Exercise { get; }
    public string Name => Exercise.Name;
    public string SubLabel { get; }          // "Штанга · Грудь · Трицепс"
    public int Equipment => Exercise.EquipmentType;
    public List<int> MuscleIds { get; }
    public int PrimaryMuscleId { get; }

    [ObservableProperty] private bool isFavorite;
    public string StarText => IsFavorite ? "⭐" : "☆";
    partial void OnIsFavoriteChanged(bool value) => OnPropertyChanged(nameof(StarText));

    public ExerciseRow(Exercise ex, IReadOnlyList<(MuscleGroup mg, int role)> muscles)
    {
        Exercise = ex;
        IsFavorite = ex.IsFavorite;
        MuscleIds = muscles.Select(m => m.mg.Id).ToList();
        var primary = muscles.FirstOrDefault(m => m.role == 0);
        PrimaryMuscleId = primary.mg?.Id ?? 0;

        // Сортируем: Primary → Secondary → Stabilizer
        var ordered = muscles.OrderBy(m => m.role).Select(m => m.mg.Name).ToList();
        var parts = new List<string> { EquipmentTypes.ToRu(ex.EquipmentType) };
        parts.AddRange(ordered);
        SubLabel = string.Join(" · ", parts);
    }
}

// Универсальный чип фильтра (используем и для мышц, и для оборудования)
public partial class FilterChip : ObservableObject
{
    public int Value { get; }
    public string Label { get; }
    [ObservableProperty] private bool isSelected;

    public FilterChip(int value, string label, bool isSelected = false)
    {
        Value = value;
        Label = label;
        IsSelected = isSelected;
    }
}

public partial class ExerciseSelectionViewModel : ObservableObject
{
    private readonly WorkoutDataBase _database;
    private Action<Exercise>? _onExerciseSelected;

    // Полный набор карточек (после загрузки) — фильтруем из него
    private List<ExerciseRow> _allRows = new();

    [ObservableProperty] private ObservableCollection<ExerciseRow> exercises = new();
    [ObservableProperty] private ObservableCollection<FilterChip> muscleFilters = new();
    [ObservableProperty] private ObservableCollection<FilterChip> equipmentFilters = new();

    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool favoritesOnly;
    [ObservableProperty] private bool isLoading;

    public ExerciseSelectionViewModel(WorkoutDataBase database, Action<Exercise>? onExerciseSelected = null)
    {
        _database = database;
        _onExerciseSelected = onExerciseSelected;
        _ = LoadAsync();
    }

    public void SetCallback(Action<Exercise> callback) => _onExerciseSelected = callback;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var exes = await _database.GetAllExercisesAsync();
            var allEmg = await _database.GetAllExerciseMusclesAsync();
            var allMg = await _database.GetMuscleGroupsAsync();
            var mgById = allMg.ToDictionary(m => m.Id, m => m);

            // Группируем связи по упражнению
            var emgByEx = allEmg.GroupBy(e => e.ExerciseId)
                                .ToDictionary(g => g.Key, g => g.ToList());

            var rows = new List<ExerciseRow>();
            foreach (var ex in exes.Where(e => !e.IsArchived))
            {
                var muscles = new List<(MuscleGroup mg, int role)>();
                if (emgByEx.TryGetValue(ex.Id, out var emgList))
                {
                    foreach (var emg in emgList)
                        if (mgById.TryGetValue(emg.MuscleGroupId, out var mg))
                            muscles.Add((mg, emg.Role));
                }
                rows.Add(new ExerciseRow(ex, muscles));
            }
            _allRows = rows;

            // Фильтры — только те значения, которые реально встречаются в данных
            var presentMuscleIds = _allRows.SelectMany(r => r.MuscleIds).Distinct().ToHashSet();
            var muscleChips = allMg
                .Where(m => presentMuscleIds.Contains(m.Id))
                .OrderBy(m => m.Name)
                .Select(m => new FilterChip(m.Id, m.Name))
                .ToList();
            MuscleFilters = new ObservableCollection<FilterChip>(muscleChips);

            var presentEquipment = _allRows.Select(r => r.Equipment).Distinct().OrderBy(e => e).ToList();
            var eqChips = presentEquipment
                .Select(e => new FilterChip(e, EquipmentTypes.ToRu(e)))
                .ToList();
            EquipmentFilters = new ObservableCollection<FilterChip>(eqChips);

            ApplyFilters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // === Команды фильтров ===

    [RelayCommand]
    private void ToggleMuscleFilter(FilterChip chip)
    {
        if (chip == null) return;
        chip.IsSelected = !chip.IsSelected;
        ApplyFilters();
    }

    [RelayCommand]
    private void ToggleEquipmentFilter(FilterChip chip)
    {
        if (chip == null) return;
        chip.IsSelected = !chip.IsSelected;
        ApplyFilters();
    }

    [RelayCommand]
    private void ToggleFavoritesOnly()
    {
        FavoritesOnly = !FavoritesOnly;
        ApplyFilters();
    }

    [RelayCommand]
    private void ResetFilters()
    {
        foreach (var c in MuscleFilters) c.IsSelected = false;
        foreach (var c in EquipmentFilters) c.IsSelected = false;
        FavoritesOnly = false;
        SearchText = "";
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        IEnumerable<ExerciseRow> q = _allRows;

        // Поиск ru + en
        var search = SearchText?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            q = q.Where(r =>
                r.Exercise.Name.ToLowerInvariant().Contains(search) ||
                (r.Exercise.NameEn?.ToLowerInvariant().Contains(search) ?? false));
        }

        // Мышцы — OR между выбранными
        var selMuscles = MuscleFilters.Where(c => c.IsSelected).Select(c => c.Value).ToHashSet();
        if (selMuscles.Count > 0)
            q = q.Where(r => r.MuscleIds.Any(id => selMuscles.Contains(id)));

        // Оборудование — OR между выбранными
        var selEq = EquipmentFilters.Where(c => c.IsSelected).Select(c => c.Value).ToHashSet();
        if (selEq.Count > 0)
            q = q.Where(r => selEq.Contains(r.Equipment));

        if (FavoritesOnly)
            q = q.Where(r => r.IsFavorite);

        // Избранные сверху, затем по алфавиту
        var ordered = q
            .OrderByDescending(r => r.IsFavorite)
            .ThenBy(r => r.Name)
            .ToList();

        Exercises = new ObservableCollection<ExerciseRow>(ordered);
    }

    // === Команды на строке ===

    [RelayCommand]
    private async Task ToggleFavorite(ExerciseRow row)
    {
        if (row == null) return;
        await _database.ToggleExerciseFavoriteAsync(row.Exercise.Id);
        row.IsFavorite = !row.IsFavorite;
        row.Exercise.IsFavorite = row.IsFavorite;
        ApplyFilters(); // чтобы пересортировать (избранные наверх)
    }

    [RelayCommand]
    private void SelectExercise(ExerciseRow row)
    {
        if (row == null) return;
        _onExerciseSelected?.Invoke(row.Exercise);
    }

    [RelayCommand]
    private async Task CreateNewExercise()
    {
        string result = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Новое упражнение", "Введите название:");

        if (!string.IsNullOrWhiteSpace(result))
        {
            var newEx = new Exercise
            {
                Name = result.Trim(),
                IsCustom = true,
                CreatedAt = DateTime.UtcNow
            };
            await _database.SaveExerciseAsync(newEx);
            await LoadAsync();
        }
    }
}

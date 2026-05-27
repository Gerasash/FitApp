using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Drawables;
using FitApp.Models;
using FitApp.Services;
using Microsoft.Maui.Graphics;

namespace FitApp.ViewModels;

/// <summary>
/// ViewModel главного экрана. После Этапа 4 главная — это дашборд, а не
/// лаунчер: цветная сетка плиток удалена (её роль выполняет Shell TabBar
/// внизу — Тренировки/Прогресс/Упражнения уже доступны оттуда). Здесь
/// показываются только содержательные карточки:
///
/// 1. «Следующая тренировка» — рекомендация WorkoutPlannerService по
///    самому частому упражнению пользователя (HasPlan).
///    Альтернатива, когда данных мало: подсказка (HasPlanHint).
/// 2. «Последняя тренировка» + «Активность» — две сводки.
/// 3. Мини-график 1ПМ (спарклайн) по тому же упражнению.
/// 4. Hero (логотип + кнопка «Начать тренировку») — виден только при
///    пустой БД, для свежей установки. Когда данные есть, CTA уже
///    встроен в карточку плана.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly WorkoutDataBase _db;
    private readonly WorkoutPlannerService _planner;

    // ===== Карточка «Последняя тренировка» =====
    [ObservableProperty] private bool hasLastWorkout;
    [ObservableProperty] private string lastWorkoutName = string.Empty;
    [ObservableProperty] private string lastWorkoutWhen = string.Empty;
    [ObservableProperty] private string lastWorkoutMeta = string.Empty;

    // ===== Карточка «Активность» =====
    [ObservableProperty] private string weekCountText = "0";
    [ObservableProperty] private string weekCountCaption = "тренировок за 7 дней";
    [ObservableProperty] private string monthCountText = string.Empty;

    // ===== Карточка «Следующая тренировка» (планировщик) =====
    [ObservableProperty] private bool hasPlan;
    [ObservableProperty] private bool hasPlanHint;
    [ObservableProperty] private string planExerciseName = string.Empty;
    [ObservableProperty] private string planWeightText = string.Empty;
    [ObservableProperty] private string planSetsRepsText = string.Empty;
    [ObservableProperty] private string planRpeText = string.Empty;

    // ===== Карточка «Мини-график 1ПМ» =====
    [ObservableProperty] private bool hasMiniChart;
    [ObservableProperty] private IDrawable? mini1RmChart;
    [ObservableProperty] private string miniChartTitle = string.Empty;
    [ObservableProperty] private string miniChartSubtitle = string.Empty;

    // ===== Пустое состояние =====
    [ObservableProperty] private bool isEmpty = true;

    public MainViewModel(WorkoutDataBase db, WorkoutPlannerService planner)
    {
        _db = db;
        _planner = planner;
    }

    public async Task LoadAsync()
    {
        try
        {
            var user = await _db.GetCurrentUserAsync();

            var last = await _db.GetLastWorkoutSummaryAsync(user.Id);
            ApplyLastWorkout(last);

            var times = await _db.GetRecentWorkoutTimesAsync(user.Id, daysBack: 30);
            ApplyActivity(times, last?.StartTime);

            IsEmpty = last == null;

            TopExerciseRef? top = null;
            if (!IsEmpty)
                top = await _db.GetMostFrequentExerciseAsync(user.Id, daysBack: 30);

            await LoadPlanAsync(top);
            await LoadMiniChartAsync(user.Id, top);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadAsync failed: {ex.Message}");
        }
    }

    private async Task LoadPlanAsync(TopExerciseRef? top)
    {
        HasPlan = false;
        HasPlanHint = false;

        if (IsEmpty) return;

        if (top == null)
        {
            HasPlanHint = true;
            return;
        }

        var rec = await _planner.GetRecommendationAsync(top.Id);
        if (rec == null)
        {
            HasPlanHint = true;
            PlanExerciseName = top.Name;
            return;
        }

        var ru = CultureInfo.GetCultureInfo("ru-RU");
        PlanExerciseName = top.Name;
        PlanWeightText = $"{rec.WeightKg.ToString("0.#", ru)} кг";
        PlanSetsRepsText = $"{rec.Sets}×{rec.Reps}";
        PlanRpeText = $"RPE {rec.TargetRpe.ToString("0.#", ru)}";
        HasPlan = true;
    }

    private async Task LoadMiniChartAsync(int userId, TopExerciseRef? top)
    {
        HasMiniChart = false;
        if (top == null) return;

        var history = await _db.GetExerciseWorkoutHistoryAsync(userId, top.Id, limit: 12);
        if (history.Count < 3) return;

        var values = history.Select(h => h.TopEpley1Rm).ToList();

        var primary = (Color)(Application.Current?.Resources["Primary"] ?? Colors.SteelBlue);
        Mini1RmChart = new Mini1RmChartDrawable
        {
            Values = values,
            LineColor = primary,
            FillColor = primary.WithAlpha(0.24f),
        };

        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var delta = values[^1] - values[0];
        var sign = delta >= 0 ? "+" : string.Empty;
        var workoutsWord = PluralRu(history.Count, "тренировку", "тренировки", "тренировок");

        MiniChartTitle = $"{top.Name} · 1ПМ";
        MiniChartSubtitle = $"{sign}{delta.ToString("0.#", ru)} кг за {history.Count} {workoutsWord}";
        HasMiniChart = true;
    }

    private void ApplyLastWorkout(LastWorkoutSummary? last)
    {
        if (last == null)
        {
            HasLastWorkout = false;
            return;
        }

        HasLastWorkout = true;
        LastWorkoutName = string.IsNullOrWhiteSpace(last.Name) ? "Без названия" : last.Name;
        LastWorkoutWhen = FormatRelative(last.StartTime);

        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var exercisesWord = PluralRu(last.ExerciseCount, "упражнение", "упражнения", "упражнений");
        var setsWord = PluralRu(last.NumSets, "подход", "подхода", "подходов");
        var volume = last.TotalVolume.ToString("N0", ru).Replace(",", " ");
        LastWorkoutMeta = last.NumSets > 0
            ? $"{last.ExerciseCount} {exercisesWord} · {last.NumSets} {setsWord} · {volume} кг·повт"
            : $"{last.ExerciseCount} {exercisesWord}";
    }

    private void ApplyActivity(List<DateTime> times, DateTime? lastWorkoutAt)
    {
        var nowLocal = DateTime.Now.Date;
        var weekStart = nowLocal.AddDays(-6);

        int week = 0, month = 0;
        foreach (var t in times)
        {
            var local = t.Kind == DateTimeKind.Utc ? t.ToLocalTime().Date : t.Date;
            if (local >= weekStart) week++;
            month++;
        }

        WeekCountText = week.ToString();
        WeekCountCaption = PluralRu(week, "тренировка за 7 дней", "тренировки за 7 дней", "тренировок за 7 дней");

        if (lastWorkoutAt is null)
        {
            MonthCountText = string.Empty;
            return;
        }

        var daysSince = (int)(nowLocal - (lastWorkoutAt.Value.Kind == DateTimeKind.Utc
            ? lastWorkoutAt.Value.ToLocalTime().Date
            : lastWorkoutAt.Value.Date)).TotalDays;
        var sinceText = daysSince switch
        {
            <= 0 => "последняя — сегодня",
            1 => "последняя — вчера",
            _ => $"последняя — {daysSince} {PluralRu(daysSince, "день", "дня", "дней")} назад"
        };
        MonthCountText = $"за 30 дней: {month} · {sinceText}";
    }

    private static string FormatRelative(DateTime when)
    {
        var local = when.Kind == DateTimeKind.Utc ? when.ToLocalTime() : when;
        var days = (int)(DateTime.Now.Date - local.Date).TotalDays;
        return days switch
        {
            <= 0 => "сегодня",
            1 => "вчера",
            < 7 => $"{days} {PluralRu(days, "день", "дня", "дней")} назад",
            _ => local.ToString("dd MMMM", CultureInfo.GetCultureInfo("ru-RU"))
        };
    }

    private static string PluralRu(int n, string one, string few, string many)
    {
        int mod100 = n % 100;
        if (mod100 >= 11 && mod100 <= 14) return many;
        return (n % 10) switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many
        };
    }

    [RelayCommand]
    private Task StartWorkout() => Shell.Current.GoToAsync("//WorkoutListPage");

    [RelayCommand]
    private Task OpenLastWorkout() => Shell.Current.GoToAsync("//WorkoutListPage");
}

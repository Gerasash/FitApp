using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace FitApp.ViewModels;

public partial class ProgressViewModel : ObservableObject
{
    private readonly WorkoutDataBase _database;

    // --- Режим (обзор / по упражнению) ---
    [ObservableProperty] private bool isOverviewMode = true;
    [ObservableProperty] private bool isExerciseMode;

    [RelayCommand]
    private async Task ShowOverview()
    {
        IsOverviewMode = true;
        IsExerciseMode = false;
        await LoadOverviewAsync();
    }

    [RelayCommand]
    private async Task ShowExercise()
    {
        IsOverviewMode = false;
        IsExerciseMode = true;
        if (Exercises.Count == 0) await LoadExercises();
    }

    // --- Обзор ---
    [ObservableProperty] private int workoutsThisMonth;
    [ObservableProperty] private string tonnageThisMonth = "0";
    [ObservableProperty] private int streakDays;
    [ObservableProperty] private string avgRpeWeek = "—";
    [ObservableProperty] private string weekComparison = "—";
    [ObservableProperty] private ObservableCollection<HeatmapCell> heatmapCells = new();
    [ObservableProperty] private string heatmapHint = "12 недель · цвет = тоннаж дня";
    [ObservableProperty] private ObservableCollection<PrEntry> recentPrs = new();
    [ObservableProperty] private bool hasPrs;

    // --- По упражнению (как было) ---
    [ObservableProperty] private ObservableCollection<Exercise> exercises = new();
    [ObservableProperty] private Exercise selectedExercise;
    [ObservableProperty] private ObservableCollection<ProgressPoint> chartPoints = new();
    [ObservableProperty] private ObservableCollection<ProgressPoint> forecastPoints = new();
    [ObservableProperty] private double maxWeight;
    [ObservableProperty] private double avgRpe;
    [ObservableProperty] private int totalWorkouts;
    [ObservableProperty] private double forecastWeight;
    [ObservableProperty] private string forecastLabel = "—";
    [ObservableProperty] private string trendLabel = "—";

    // --- LiveCharts серии для графика ---
    [ObservableProperty] private ISeries[] chartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] chartXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] chartYAxes = Array.Empty<Axis>();

    public ProgressViewModel(WorkoutDataBase database)
    {
        _database = database;
    }

    // ====================== ОБЗОР ======================

    public async Task LoadOverviewAsync()
    {
        var workouts = await _database.GetWorkouts();
        if (workouts == null || workouts.Count == 0)
        {
            WorkoutsThisMonth = 0;
            TonnageThisMonth = "0 кг";
            StreakDays = 0;
            AvgRpeWeek = "—";
            WeekComparison = "Нет данных";
            HeatmapCells = new ObservableCollection<HeatmapCell>();
            return;
        }

        // Подгружаем все упражнения + сеты пакетно (через GetExercisesForWorkoutAsync).
        // Параллельно копим личные рекорды: бежим по тренировкам в хронологическом порядке
        // и фиксируем рекорд, когда вес сета строго больше предыдущего максимума.
        var byDay = new Dictionary<DateTime, (double tonnage, int sets, double rpeSum, int rpeCount)>();
        var bestPerExercise = new Dictionary<int, double>();           // exerciseId -> текущий рекорд
        var bestRepsAt = new Dictionary<int, int>();                   // exerciseId -> повторы на рекорде
        var allPrs = new List<PrEntry>();
        var allExercises = await _database.GetAllExercisesAsync();
        var exerciseNames = allExercises.ToDictionary(e => e.Id, e => e.Name);

        foreach (var w in workouts.OrderBy(x => x.StartTime))
        {
            var exs = await _database.GetExercisesForWorkoutAsync(w.Id);
            var day = w.StartTime.Date;
            if (!byDay.TryGetValue(day, out var agg))
                agg = (0, 0, 0, 0);

            foreach (var we in exs)
            {
                if (we.Sets == null) continue;
                foreach (var s in we.Sets)
                {
                    agg.tonnage += s.Weight * s.Reps;
                    agg.sets += 1;
                    if (s.RPE > 0)
                    {
                        agg.rpeSum += s.RPE;
                        agg.rpeCount += 1;
                    }

                    // PR-проверка
                    if (s.Weight <= 0 || s.Reps <= 0) continue;
                    double prev = bestPerExercise.TryGetValue(we.ExerciseId, out var v) ? v : 0;
                    if (s.Weight > prev)
                    {
                        // первый сет с весом не считаем рекордом — это «база», от неё отсчитываем
                        if (prev > 0)
                        {
                            exerciseNames.TryGetValue(we.ExerciseId, out var name);
                            allPrs.Add(new PrEntry
                            {
                                ExerciseId = we.ExerciseId,
                                ExerciseName = name ?? "Упражнение",
                                Weight = s.Weight,
                                Reps = s.Reps,
                                Delta = s.Weight - prev,
                                Date = w.StartTime
                            });
                        }
                        bestPerExercise[we.ExerciseId] = s.Weight;
                        bestRepsAt[we.ExerciseId] = s.Reps;
                    }
                }
            }
            byDay[day] = agg;
        }

        // Топ-5 свежих рекордов
        RecentPrs = new ObservableCollection<PrEntry>(
            allPrs.OrderByDescending(p => p.Date).Take(5));
        HasPrs = RecentPrs.Count > 0;

        // Метрики месяца
        var now = DateTime.Now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        WorkoutsThisMonth = workouts.Count(w => w.StartTime.Date >= monthStart);
        var tonnageMonth = byDay
            .Where(kv => kv.Key >= monthStart)
            .Sum(kv => kv.Value.tonnage);
        TonnageThisMonth = FormatTonnage(tonnageMonth);

        // Стрик: подряд идущих дней с тренировкой (с сегодня или со вчера)
        int streak = 0;
        var cursor = now;
        // если сегодня тренировки нет — допускаем старт со вчера, чтобы стрик не «обнулялся» до конца дня
        if (!byDay.ContainsKey(cursor)) cursor = cursor.AddDays(-1);
        while (byDay.ContainsKey(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        StreakDays = streak;

        // Средний RPE за последнюю неделю
        var weekStart = now.AddDays(-6);
        var weekRpe = byDay
            .Where(kv => kv.Key >= weekStart && kv.Value.rpeCount > 0)
            .ToList();
        if (weekRpe.Count > 0)
        {
            double sum = weekRpe.Sum(kv => kv.Value.rpeSum);
            int cnt = weekRpe.Sum(kv => kv.Value.rpeCount);
            AvgRpeWeek = cnt > 0 ? Math.Round(sum / cnt, 1).ToString("0.0") : "—";
        }
        else
        {
            AvgRpeWeek = "—";
        }

        // Эта неделя vs прошлая
        var thisWeekStart = now.AddDays(-(((int)now.DayOfWeek + 6) % 7)); // понедельник
        var prevWeekStart = thisWeekStart.AddDays(-7);
        double thisWeekTon = byDay.Where(kv => kv.Key >= thisWeekStart).Sum(kv => kv.Value.tonnage);
        double prevWeekTon = byDay.Where(kv => kv.Key >= prevWeekStart && kv.Key < thisWeekStart).Sum(kv => kv.Value.tonnage);
        if (prevWeekTon > 0)
        {
            double delta = (thisWeekTon - prevWeekTon) / prevWeekTon * 100.0;
            string arrow = delta >= 0 ? "▲" : "▼";
            WeekComparison = $"{arrow} {Math.Abs(Math.Round(delta, 1))}% к прошлой неделе";
        }
        else if (thisWeekTon > 0)
        {
            WeekComparison = "Первая неделя с тренировками";
        }
        else
        {
            WeekComparison = "На этой неделе пока пусто";
        }

        // Heatmap: 12 недель × 7 дней, начало — понедельник 11 недель назад
        var heatStart = thisWeekStart.AddDays(-7 * 11);
        var cells = new List<HeatmapCell>();
        double maxTon = byDay.Where(kv => kv.Key >= heatStart && kv.Key <= now)
                             .Select(kv => kv.Value.tonnage)
                             .DefaultIfEmpty(0)
                             .Max();
        for (int i = 0; i < 12 * 7; i++)
        {
            var date = heatStart.AddDays(i);
            byDay.TryGetValue(date, out var agg);
            int level = 0;
            if (agg.tonnage > 0 && maxTon > 0)
            {
                double frac = agg.tonnage / maxTon;
                if (frac >= 0.75) level = 4;
                else if (frac >= 0.5) level = 3;
                else if (frac >= 0.25) level = 2;
                else level = 1;
            }
            cells.Add(new HeatmapCell
            {
                Date = date,
                Tonnage = agg.tonnage,
                Level = level,
                IsFuture = date > now,
                Tooltip = date.ToString("dd.MM") + (agg.tonnage > 0
                    ? $" · {FormatTonnage(agg.tonnage)}"
                    : " · нет тренировок")
            });
        }
        HeatmapCells = new ObservableCollection<HeatmapCell>(cells);
    }

    private static string FormatTonnage(double kg)
    {
        if (kg >= 1000) return $"{Math.Round(kg / 1000, 1)} т";
        return $"{Math.Round(kg)} кг";
    }

    // ====================== ПО УПРАЖНЕНИЮ ======================

    [RelayCommand]
    public async Task LoadExercises()
    {
        var list = await _database.GetExercisesAsync();
        Exercises = new ObservableCollection<Exercise>(list);
        if (Exercises.Count > 0)
        {
            SelectedExercise = Exercises[0];
            await LoadProgress();
        }
    }

    partial void OnSelectedExerciseChanged(Exercise value)
    {
        if (value != null)
            Task.Run(async () => await LoadProgress());
    }

    [RelayCommand]
    public async Task LoadProgress()
    {
        if (SelectedExercise == null) return;

        var allWorkouts = await _database.GetWorkouts();
        var points = new List<ProgressPoint>();

        foreach (var workout in allWorkouts.OrderBy(w => w.StartTime))
        {
            var exercises = await _database.GetExercisesForWorkoutAsync(workout.Id);
            var match = exercises.FirstOrDefault(e => e.ExerciseId == SelectedExercise.Id);
            if (match?.Sets == null || match.Sets.Count == 0) continue;

            var maxSet = match.Sets.OrderByDescending(s => s.Weight).First();
            points.Add(new ProgressPoint
            {
                Date = workout.StartTime,
                Weight = maxSet.Weight,
                Reps = maxSet.Reps,
                Rpe = maxSet.RPE,
                Label = workout.StartTime.ToString("dd.MM")
            });
        }

        if (points.Count == 0)
        {
            ChartPoints = new ObservableCollection<ProgressPoint>();
            ForecastLabel = "Нет данных";
            return;
        }

        ChartPoints = new ObservableCollection<ProgressPoint>(points);

        // LiveCharts: основная линия (рабочий вес) + расчётный 1RM по Эпли (вес * (1 + reps/30))
        var weightValues = points.Select(p => new DateTimePoint(p.Date, p.Weight)).ToArray();
        var oneRmValues = points.Select(p => new DateTimePoint(p.Date,
            Math.Round(p.Weight * (1 + p.Reps / 30.0), 1))).ToArray();

        ChartSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Рабочий вес",
                Values = weightValues,
                Stroke = new SolidColorPaint(SKColor.Parse("673AB7")) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(SKColor.Parse("673AB7").WithAlpha(40)),
                GeometrySize = 10,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("673AB7")) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                LineSmoothness = 0.4
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Расчётный 1ПМ",
                Values = oneRmValues,
                Stroke = new SolidColorPaint(SKColor.Parse("FF9800")) { StrokeThickness = 2, PathEffect = new DashEffect(new float[]{6,4}) },
                Fill = null,
                GeometrySize = 6,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("FF9800")) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                LineSmoothness = 0.4
            }
        };
        ChartXAxes = new[]
        {
            new Axis
            {
                Labeler = v => new DateTime((long)v).ToString("dd.MM"),
                UnitWidth = TimeSpan.FromDays(1).Ticks,
                MinStep = TimeSpan.FromDays(1).Ticks,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Gray)
            }
        };
        ChartYAxes = new[]
        {
            new Axis
            {
                Labeler = v => $"{v:0} кг",
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Gray)
            }
        };

        // Статистика
        MaxWeight = points.Max(p => p.Weight);
        AvgRpe = Math.Round(points.Average(p => p.Rpe), 1);
        TotalWorkouts = points.Count;

        // Прогноз — берём последние точки, но прогноз не может быть ниже максимума
        var last = points.TakeLast(Math.Min(5, points.Count)).ToList();
        if (last.Count >= 2)
        {
            var rawForecast = LinearForecast(last);

            // Прогноз не может быть ниже исторического максимума
            ForecastWeight = Math.Round(Math.Max(rawForecast, MaxWeight), 1);

            var trend = ForecastWeight - last.Last().Weight;
            ForecastLabel = $"{ForecastWeight} кг через 2 недели";
            TrendLabel = trend >= 0
                ? $"▲ +{Math.Round(trend, 1)} кг"
                : $"— удержание максимума {MaxWeight} кг";
        }
    }

    private double LinearForecast(List<ProgressPoint> points)
    {
        // Простая линейная регрессия по индексам
        int n = points.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += points[i].Weight;
            sumXY += i * points[i].Weight;
            sumX2 += i * i;
        }
        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;

        // Прогноз на 2 недели вперед (примерно +2 тренировки)
        return slope * (n + 1) + intercept;
    }
}

public class ProgressPoint
{
    public DateTime Date { get; set; }
    public double Weight { get; set; }
    public int Reps { get; set; }
    public double Rpe { get; set; }
    public string Label { get; set; }
}

public class PrEntry
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int Reps { get; set; }
    public double Delta { get; set; }
    public DateTime Date { get; set; }

    public string Title => $"{ExerciseName} — {Weight} кг × {Reps}";
    public string Subtitle => $"+{Math.Round(Delta, 1)} кг · {Date:dd.MM.yyyy}";
}

public class HeatmapCell
{
    public DateTime Date { get; set; }
    public double Tonnage { get; set; }
    public int Level { get; set; }      // 0..4
    public bool IsFuture { get; set; }
    public string Tooltip { get; set; } = string.Empty;

    public Color CellColor => Level switch
    {
        0 => IsFuture
            ? Color.FromArgb("#1A1A1A")
            : Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#2A2A2A")
                : Color.FromArgb("#EBEDF0"),
        1 => Color.FromArgb("#C8E6C9"),
        2 => Color.FromArgb("#81C784"),
        3 => Color.FromArgb("#43A047"),
        4 => Color.FromArgb("#1B5E20"),
        _ => Colors.Transparent
    };
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitApp.Data;
using FitApp.Models;
using System.Collections.ObjectModel;

namespace FitApp.ViewModels;

public partial class ProgressViewModel : ObservableObject
{
    private readonly WorkoutDataBase _database;

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

    public ProgressViewModel(WorkoutDataBase database)
    {
        _database = database;
    }

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
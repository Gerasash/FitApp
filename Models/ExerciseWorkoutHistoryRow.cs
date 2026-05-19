using SQLite;
using System;

namespace FitApp.Models;

/// <summary>
/// DTO: одна строка истории по упражнению — агрегаты ОДНОЙ тренировки.
/// Используется ML-сервисом прогнозирования (OnnxPredictionService) для
/// построения вектора фичей. Поля соответствуют агрегации, делаемой
/// в Python-pipeline (см. ml/notebooks/02_lightgbm.py).
/// </summary>
public class ExerciseWorkoutHistoryRow
{
    public DateTime Date { get; set; }
    public double TopEpley1Rm { get; set; }   // max(weight * (1 + reps/30))
    public double TopWeight { get; set; }
    public int NSets { get; set; }
    public double AvgRpe { get; set; }
    public double MinRpe { get; set; }
    public double MaxRpe { get; set; }
    public double AvgReps { get; set; }
}

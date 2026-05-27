using System;

namespace FitApp.Models;

/// <summary>
/// DTO для карточки «Последняя тренировка» на главном экране.
/// Заполняется одним агрегирующим SQL-запросом в WorkoutDataBase —
/// не тянем всю иерархию упражнений и подходов в память, считаем сразу
/// в БД. Разминочные подходы (Kind == Warmup) из объёма исключены, чтобы
/// число «кг·повт» совпадало с тем, что показывает страница тренировки.
/// </summary>
public class LastWorkoutSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartTime { get; set; }
    public int NumSets { get; set; }
    public double TotalVolume { get; set; }   // Σ (вес × повторения), без разминок
    public int ExerciseCount { get; set; }
}

/// <summary>
/// Самое часто используемое упражнение за период. Берётся для карточки
/// «Следующая тренировка» на главном экране: планировщик строит
/// рекомендацию именно по нему. Это разумный фокус — то, что человек
/// чаще всего тренирует, скорее всего станет первым упражнением и
/// следующей сессии.
/// </summary>
public class TopExerciseRef
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int UsageCount { get; set; }
}

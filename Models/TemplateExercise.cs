using SQLite;

namespace FitApp.Models;

[Table("TemplateExercises")]
public class TemplateExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TemplateId { get; set; }

    public int ExerciseId { get; set; }

    public int OrderIndex { get; set; }

    // План на тренировку
    public int TargetSets { get; set; } = 3;
    public int? RepsMin { get; set; }
    public int? RepsMax { get; set; }
    public double? TargetRpe { get; set; }
    public int? RestSeconds { get; set; }

    public string? Notes { get; set; }

    // Номер суперсета: упражнения с одним числом выполняются связкой
    public int? SupersetGroup { get; set; }

    // Поля синхронизации
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // Не сохраняется — подтягиваем для UI
    [Ignore] public string? ExerciseName { get; set; }
}

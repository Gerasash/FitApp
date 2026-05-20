namespace FitApp.Api.Dtos;

/// <summary>
/// Полный батч синхронизации: клиент шлёт всё что изменилось локально с
/// момента LastSyncUtc, сервер применяет (last-write-wins по UpdatedAtUtc) и
/// возвращает все изменения сервера, которые клиент ещё не видел.
/// </summary>
public class SyncRequest
{
    /// <summary>Время последней успешной синхронизации (UTC, ISO 8601). При первом sync — null.</summary>
    public DateTime? LastSyncUtc { get; set; }

    public List<WorkoutSyncDto> Workouts { get; set; } = new();
    public List<WorkoutExerciseSyncDto> WorkoutExercises { get; set; } = new();
    public List<ExerciseSetSyncDto> ExerciseSets { get; set; } = new();
}

public class SyncResponse
{
    /// <summary>Время на сервере в момент ответа — клиент сохранит как новый LastSyncUtc.</summary>
    public DateTime ServerTimeUtc { get; set; }

    public List<WorkoutSyncDto> Workouts { get; set; } = new();
    public List<WorkoutExerciseSyncDto> WorkoutExercises { get; set; } = new();
    public List<ExerciseSetSyncDto> ExerciseSets { get; set; } = new();
}

public class WorkoutSyncDto
{
    public string SyncId { get; set; } = "";
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}

public class WorkoutExerciseSyncDto
{
    public string SyncId { get; set; } = "";
    public string WorkoutSyncId { get; set; } = "";
    public int ExerciseRefId { get; set; }
    public int OrderIndex { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}

public class ExerciseSetSyncDto
{
    public string SyncId { get; set; } = "";
    public string WorkoutExerciseSyncId { get; set; } = "";
    public int SetNumber { get; set; }
    public double Weight { get; set; }
    public int Reps { get; set; }
    public double RPE { get; set; }
    public bool IsAssisted { get; set; }
    public int Kind { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}

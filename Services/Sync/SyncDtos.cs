namespace FitApp.Services.Sync;

// Клиентские DTO для обмена с FitApp.Api. Структура совпадает с серверными
// (FitApp.Api/Dtos/SyncDtos.cs) — общий "контракт" sync.

public class SyncRequest
{
    public DateTime? LastSyncUtc { get; set; }
    public List<WorkoutSyncDto> Workouts { get; set; } = new();
    public List<WorkoutExerciseSyncDto> WorkoutExercises { get; set; } = new();
    public List<ExerciseSetSyncDto> ExerciseSets { get; set; } = new();
}

public class SyncResponse
{
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

public record AuthRequest(string Email, string Password);
public record AuthResponse(string Token, DateTime ExpiresAtUtc, long UserId, string Email);

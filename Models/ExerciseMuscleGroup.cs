using SQLite;

namespace FitApp.Models;

// Связь упражнение ↔ группа мышц с ролью.
// Одно упражнение тянет несколько мышц с разной "силой":
// жим лёжа → Грудь (Primary), Трицепс (Secondary), Передняя дельта (Secondary)
[Table("ExerciseMuscleGroups")]
public class ExerciseMuscleGroup
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ExerciseId { get; set; }

    [Indexed]
    public int MuscleGroupId { get; set; }

    // 0 Primary, 1 Secondary, 2 Stabilizer
    public int Role { get; set; }
}

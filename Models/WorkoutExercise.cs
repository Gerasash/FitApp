using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitApp.Models;

[Table("WorkoutExercises")]
public class WorkoutExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ForeignKey(typeof(Workout))]
    public int WorkoutId { get; set; }

    [ForeignKey(typeof(Exercise))]
    public int ExerciseId { get; set; }

    // Порядок упражнения в тренировке (1, 2, 3...)
    // ИИ важно знать: делал ты это свежим или уставшим
    public int OrderIndex { get; set; }

    // Навигационные свойства (для удобства в коде)
    [OneToMany(CascadeOperations = CascadeOperation.All)]
    public List<ExerciseSet> Sets { get; set; } = new();

    [Ignore] // Не сохраняем в БД, подтягиваем join-ом или отдельно
    public string ExerciseName { get; set; }
}
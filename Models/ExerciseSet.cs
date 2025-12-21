using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLiteNetExtensions.Attributes;
namespace FitApp.Models;

[Table("ExerciseSets")]
public class ExerciseSet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ForeignKey(typeof(WorkoutExercise))]
    public int WorkoutExerciseId { get; set; }

    public int SetNumber { get; set; } // Номер подхода (1, 2, 3)
    public double Weight { get; set; } // Вес (кг)
    public int Reps { get; set; }      // Повторения

    // 🔥 Ключевое поле для Нейросети: RPE (Rate of Perceived Exertion)
    // Оценка тяжести от 1 до 10. 
    // Если нейросеть видит, что вес растет, а RPE падает -> ты стал сильнее.
    public double RPE { get; set; }

    // Была ли помощь страхующего / читинг (для чистоты данных)
    public bool IsAssisted { get; set; }
}
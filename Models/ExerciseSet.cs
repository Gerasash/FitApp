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

    public int SetNumber { get; set; } // Физический порядок в БД (1, 2, 3)
    public double Weight { get; set; } // Вес (кг)
    public int Reps { get; set; }      // Повторения

    // 🔥 Ключевое поле для Нейросети: RPE (Rate of Perceived Exertion)
    // Оценка тяжести от 1 до 10.
    // Если нейросеть видит, что вес растет, а RPE падает -> ты стал сильнее.
    public double RPE { get; set; }

    // Была ли помощь страхующего / читинг (для чистоты данных)
    public bool IsAssisted { get; set; }

    // Тип подхода (Hevy-style): Normal/Warmup/Failure/DropSet.
    // sqlite-net хранит enum как int; миграция CreateTableAsync добавит колонку автоматически.
    public SetType Kind { get; set; } = SetType.Normal;

    // Поля синхронизации
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // --- Только для UI: бейдж и его цвет. Перевычисляются после загрузки сетов. ---
    [Ignore] public string DisplayBadge { get; set; } = "1";
    [Ignore] public Microsoft.Maui.Graphics.Color BadgeColor { get; set; }
        = Microsoft.Maui.Graphics.Color.FromArgb("#212121");

    // Пересчёт бейджей у списка сетов: Normal — это 1,2,3..., прочие — свои буквы.
    public static void RecomputeBadges(IList<ExerciseSet> sets)
    {
        if (sets == null) return;
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var normalColor = isDark
            ? Microsoft.Maui.Graphics.Colors.White
            : Microsoft.Maui.Graphics.Color.FromArgb("#212121");

        int normalIdx = 0;
        foreach (var s in sets)
        {
            switch (s.Kind)
            {
                case SetType.Warmup:
                    s.DisplayBadge = "W";
                    s.BadgeColor = Microsoft.Maui.Graphics.Color.FromArgb("#FB8C00");
                    break;
                case SetType.Failure:
                    s.DisplayBadge = "F";
                    s.BadgeColor = Microsoft.Maui.Graphics.Color.FromArgb("#E53935");
                    break;
                case SetType.DropSet:
                    s.DisplayBadge = "D";
                    s.BadgeColor = Microsoft.Maui.Graphics.Color.FromArgb("#1E88E5");
                    break;
                default:
                    normalIdx++;
                    s.DisplayBadge = normalIdx.ToString();
                    s.BadgeColor = normalColor;
                    break;
            }
        }
    }
}
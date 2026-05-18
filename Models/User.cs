using SQLite;
using System;

namespace FitApp.Models;

[Table("Users")]
public class User
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Логин для будущей синхронизации. Может быть пустым у локального профиля,
    // созданного при первом запуске до того, как юзер залогинился.
    public string? Email { get; set; }

    // Отображаемое имя — для приветствия на главном экране
    public string? DisplayName { get; set; }

    // Анкета — используется как фичи в модели прогнозирования
    public double Bodyweight { get; set; }      // кг
    public int Age { get; set; }                // лет
    public int Sex { get; set; }                // 0 = не указан, 1 = муж, 2 = жен
    public DateTime? ExperienceStartDate { get; set; } // когда начал заниматься (для стажа в месяцах)

    // Целевой RPE — параметр модуля планирования
    public double TargetRpe { get; set; } = 7.5;

    // Поля для синхронизации
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

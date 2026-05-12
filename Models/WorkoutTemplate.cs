using SQLite;
using System;

namespace FitApp.Models;

[Table("WorkoutTemplates")]
public class WorkoutTemplate
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Группировка ("PPL", "Upper/Lower", "5x5", "Мои")
    public string? FolderName { get; set; }

    public int OrderIndex { get; set; }

    // Счётчики использования — для сортировки "Популярные" / "Недавние"
    public int TimesUsed { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Встроенный шаблон vs созданный юзером (нельзя удалить, но можно скрыть)
    public bool IsBuiltIn { get; set; }
    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

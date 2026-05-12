using SQLite;
using System;

namespace FitApp.Models;

[Table("Exercises")]
public class Exercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Английское название — для поиска ("bench" найдёт жим лёжа)
    public string? NameEn { get; set; }

    // Основная группа мышц — оставляем для обратной совместимости со старыми записями.
    // Новые упражнения используют таблицу ExerciseMuscleGroups (роли Primary/Secondary).
    public int PrimaryMuscleGroupId { get; set; }

    // 0 Штанга, 1 Гантели, 2 Тренажёр, 3 Свой вес, 4 Блок, 5 Гиря, 6 Резина, 7 Прочее
    public int EquipmentType { get; set; }

    // Compound / Isolation / Cardio / Stretching
    public int Category { get; set; }

    // Push / Pull / Squat / Hinge / Carry / Core
    public int Mechanic { get; set; }

    // Короткая инструкция по технике (3-5 пунктов через \n)
    public string? Instructions { get; set; }

    // true — добавлено юзером вручную; false — пришло из встроенной базы
    public bool IsCustom { get; set; }

    // Скрыто из выбора, но история остаётся
    public bool IsArchived { get; set; }

    // В избранном (звёздочка)
    public bool IsFavorite { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

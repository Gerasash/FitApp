namespace FitApp.Models;

// Категория упражнения — для фильтров и для ИИ (compound = больше нагрузка, длиннее отдых)
public enum ExerciseCategory
{
    Compound = 0,    // Базовое многосуставное (жим, присед, тяга)
    Isolation = 1,   // Изолирующее (бицепс на скамье, разгибание ног)
    Cardio = 2,
    Stretching = 3,
    Other = 4
}

// Расширение существующего EquipmentType (0..3 уже используются, добавляем дальше)
// 0 - Штанга, 1 - Гантели, 2 - Тренажер, 3 - Свой вес
// 4 - Трос/блок, 5 - Гиря, 6 - Резина, 7 - Прочее
public static class EquipmentTypes
{
    public const int Barbell = 0;
    public const int Dumbbell = 1;
    public const int Machine = 2;
    public const int Bodyweight = 3;
    public const int Cable = 4;
    public const int Kettlebell = 5;
    public const int Band = 6;
    public const int Other = 7;

    public static string ToRu(int t) => t switch
    {
        Barbell => "Штанга",
        Dumbbell => "Гантели",
        Machine => "Тренажёр",
        Bodyweight => "Свой вес",
        Cable => "Блок",
        Kettlebell => "Гиря",
        Band => "Резина",
        _ => "Прочее"
    };
}

// Биомеханика — для подбора замены и группировки в шаблонах
public enum ExerciseMechanic
{
    Push = 0,     // Жим
    Pull = 1,     // Тяга
    Squat = 2,    // Присед
    Hinge = 3,    // Наклон/становая
    Carry = 4,    // Перенос
    Core = 5,     // Корпус
    Other = 6
}

// Роль мышцы в упражнении
public enum MuscleRole
{
    Primary = 0,     // Целевая
    Secondary = 1,   // Помогает
    Stabilizer = 2   // Стабилизатор
}

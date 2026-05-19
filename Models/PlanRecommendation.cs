namespace FitApp.Models;

/// <summary>
/// Рекомендация модуля планирования (см. <see cref="Services.WorkoutPlannerService"/>):
/// предложение, как выполнить упражнение на ближайшей тренировке — рабочий вес,
/// число повторений, число рабочих подходов и целевой RPE.
///
/// Поля рассчитываются из прогноза ONNX-модели на 28-дневный горизонт,
/// интерполированного на ~7 дней, обратной формулы Эпли и индивидуального
/// целевого RPE из профиля пользователя.
/// </summary>
public record PlanRecommendation(
    double WeightKg,
    int Reps,
    int Sets,
    double TargetRpe,
    string Text);

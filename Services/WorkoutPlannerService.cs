using FitApp.Data;
using FitApp.Models;

namespace FitApp.Services;

/// <summary>
/// Модуль планирования следующей тренировки (этап 5 ML-модуля).
///
/// На вход: упражнение, у которого есть история. На выход — рекомендация
/// рабочего веса, повторений и числа подходов на ближайшую тренировку.
///
/// Алгоритм:
/// 1. Из истории берём текущий top 1ПМ (по формуле Эпли — лучший рабочий
///    подход последней тренировки).
/// 2. ONNX-модель предсказывает 1ПМ через 28 дней.
/// 3. Интерполируем приращение на горизонт следующей тренировки (~7 дней),
///    учитывая фактическую паузу с момента последнего занятия упражнением.
/// 4. Целевой 1ПМ + целевые повторения → рабочий вес через обратную
///    формулу Эпли с поправкой на целевой RPE из профиля пользователя.
/// 5. Округляем до шага веса оборудования (2.5 кг для штанги/гантелей,
///    5 кг для тренажёров).
///
/// Формулы согласованы с симулятором (ml/generator/athlete.py) — это даёт
/// рекомендации в той же «системе координат», в которой обучалась модель.
/// </summary>
public class WorkoutPlannerService
{
    private const int ShortHorizonDays = 7;     // следующая тренировка ≈ через неделю
    private const double DefaultTargetRpe = 7.5;
    // Защита от слишком оптимистичного прогноза: за одну неделю не
    // рекомендуем приращение больше 5% (это уже неделя сильного роста
    // даже у новичка).
    private const double MaxWeeklyGrowth = 0.05;

    private readonly WorkoutDataBase _db;
    private readonly OnnxPredictionService _onnx;

    public WorkoutPlannerService(WorkoutDataBase db, OnnxPredictionService onnx)
    {
        _db = db;
        _onnx = onnx;
    }

    /// <summary>
    /// Возвращает рекомендацию на следующую тренировку упражнения, либо
    /// null, если данных недостаточно (нет истории) или модель не загружена.
    /// </summary>
    public async Task<PlanRecommendation?> GetRecommendationAsync(int exerciseId)
    {
        try
        {
            var user = await _db.GetCurrentUserAsync();
            var history = await _db.GetExerciseWorkoutHistoryAsync(user.Id, exerciseId, limit: 30);
            if (history.Count == 0) return null;

            var exercise = await _db.GetExerciseByIdAsync(exerciseId);
            if (exercise == null) return null;

            // Текущий 1ПМ — последняя тренировка упражнения.
            var current = history[^1];
            double current1rm = current.TopEpley1Rm;
            if (current1rm <= 0) return null;

            // Прогноз на 28 дней. Если модель недоступна — план всё равно
            // строим, просто без приращения (фактически рекомендуем повторить
            // текущий уровень).
            var prediction = await _onnx.PredictAsync(exerciseId);

            double target1rm;
            if (prediction != null && prediction.HorizonDays > 0)
            {
                // Линейная интерполяция: 1ПМ через 7 дней ≈ current + delta * (7/28).
                // Если с последней тренировки прошло больше 7 дней, шагаем
                // на фактическую паузу, но не дальше горизонта модели.
                double daysAhead = Math.Clamp(
                    ShortHorizonDays,
                    1,
                    prediction.HorizonDays);
                double delta = prediction.Predicted1RmKg - current1rm;
                target1rm = current1rm + delta * (daysAhead / prediction.HorizonDays);
            }
            else
            {
                target1rm = current1rm;
            }

            // Защита 1: модель иногда даёт отрицательный тренд при шумной
            // короткой истории — в плане это превратилось бы в снижение
            // веса, чего пользователь не ждёт. Берём не меньше текущего.
            if (target1rm < current1rm) target1rm = current1rm;

            // Защита 2: ограничиваем максимальное приращение за неделю
            // (короткие истории + ONNX дают шум).
            double maxTarget = current1rm * (1.0 + MaxWeeklyGrowth);
            if (target1rm > maxTarget) target1rm = maxTarget;

            // Целевой RPE из профиля атлета — показываем в подсказке как
            // ориентир «насколько тяжёлый должен быть этот вес», но НЕ
            // используем как множитель уменьшения веса (это раньше давало
            // систематический сдвиг рекомендации вниз).
            double targetRpe = user.TargetRpe > 0 ? user.TargetRpe : DefaultTargetRpe;
            targetRpe = Math.Clamp(targetRpe, 6.0, 9.5);

            // Целевые повторения и число подходов — по категории упражнения.
            (int targetReps, int targetSets) = PickRepsAndSets(exercise, current);

            // Якорим рекомендацию на ФАКТИЧЕСКИЙ рабочий вес прошлой
            // тренировки, а не на абстрактный 1ПМ при «другом» RPE.
            // Шаги: (а) считаем коэффициент роста по прогнозу модели
            // (target/current), (б) умножаем на него прошлый top_weight,
            // (в) если повторения сменились — корректируем через Эпли.
            double growth = target1rm / current1rm; // ≥ 1.0 после защит выше
            double lastReps = Math.Max(1, current.AvgReps); // защита от 0
            double rawWeight = current.TopWeight * growth
                               * (1.0 + lastReps / 30.0)
                               / (1.0 + targetReps / 30.0);

            // Округление до шага оборудования.
            double step = WeightStep(exercise.EquipmentType);
            double weight = Math.Round(rawWeight / step) * step;
            if (weight < step) weight = step;

            // Защита 3: рекомендация не должна быть меньше прошлого
            // рабочего веса при тех же повторениях — это самая частая
            // жалоба «почему оно мне предлагает меньше, чем я уже жму».
            if (Math.Abs(targetReps - lastReps) < 1.0 && weight < current.TopWeight)
                weight = current.TopWeight;

            string text =
                $"План: {targetSets}×{targetReps} @ {weight:0.#} кг (RPE {targetRpe:0.#})";

            return new PlanRecommendation(
                WeightKg: weight,
                Reps: targetReps,
                Sets: targetSets,
                TargetRpe: targetRpe,
                Text: text);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Planner] GetRecommendationAsync failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>Выбор целевых повторений и числа подходов по категории
    /// упражнения. Усреднённые рабочие диапазоны: 5–8 для базы, 8–12 для
    /// изоляции, повторяем привычку атлета по средним повторам последней
    /// тренировки, если она попадает в разумные пределы.</summary>
    private static (int reps, int sets) PickRepsAndSets(Exercise exercise, ExerciseWorkoutHistoryRow last)
    {
        bool isCompound = exercise.Category == (int)ExerciseCategory.Compound;
        int defaultReps = isCompound ? 6 : 10;
        int defaultSets = isCompound ? 4 : 3;

        // Если атлет в прошлый раз делал X повторений и это в адекватной
        // зоне — повторим тот же режим, чтобы план не «прыгал» по объёму.
        int lastReps = (int)Math.Round(last.AvgReps);
        if (isCompound && lastReps >= 3 && lastReps <= 10) defaultReps = lastReps;
        else if (!isCompound && lastReps >= 6 && lastReps <= 15) defaultReps = lastReps;

        // Число подходов: ориентируемся на то, что атлет делал, но в
        // пределах 3–5.
        int lastSets = last.NSets;
        if (lastSets >= 3 && lastSets <= 5) defaultSets = lastSets;

        return (defaultReps, defaultSets);
    }

    /// <summary>Шаг веса в килограммах для типа оборудования. Штанга и
    /// гантели — 2.5 кг (мелкие блины редкость в общедоступных залах),
    /// тренажёры — 5 кг (плитка стека), резина/собственный вес — без
    /// округления (вернём 2.5 как нейтральный шаг).</summary>
    private static double WeightStep(int equipmentType) => equipmentType switch
    {
        EquipmentTypes.Barbell => 2.5,
        EquipmentTypes.Dumbbell => 2.5,
        EquipmentTypes.Machine => 5.0,
        EquipmentTypes.Cable => 2.5,
        EquipmentTypes.Kettlebell => 4.0,
        _ => 2.5
    };
}

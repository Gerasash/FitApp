# ПРИЛОЖЕНИЕ А

## Листинги программного кода

В приложении приведены ключевые фрагменты исходного кода, иллюстрирующие центральные алгоритмические решения работы: якорную схему планирования нагрузки (А.1), цикл двунаправленной синхронизации с защитой от расхождения часов (А.2), сборку признакового вектора для модели машинного обучения (А.3) и параметрический симулятор тренировочной сессии (А.4). Полный исходный код доступен в репозитории проекта.

---

### А.1 — Алгоритм планирования следующей тренировки

**Файл:** `Services/WorkoutPlannerService.cs` (метод `GetRecommendationAsync`). Реализует якорную схему: рабочий вес следующей тренировки рассчитывается от фактически выполненного веса последней тренировки, скорректированного на прогнозируемый прирост одноповторного максимума. Подробное описание алгоритма приведено в разделе 4.6.

```csharp
public async Task<PlanRecommendation?> GetRecommendationAsync(int exerciseId)
{
    var user = await _db.GetCurrentUserAsync();
    var history = await _db.GetExerciseWorkoutHistoryAsync(user.Id, exerciseId, limit: 30);
    if (history.Count == 0) return null;

    var exercise = await _db.GetExerciseByIdAsync(exerciseId);
    if (exercise == null) return null;

    // Текущий 1ПМ — лучший подход последней тренировки упражнения.
    var current = history[^1];
    double current1rm = current.TopEpley1Rm;
    if (current1rm <= 0) return null;

    // Прогноз на 28 дней + интерполяция на горизонт 7 дней.
    var prediction = await _onnx.PredictAsync(exerciseId);
    double target1rm;
    if (prediction != null && prediction.HorizonDays > 0)
    {
        double daysAhead = Math.Clamp(ShortHorizonDays, 1, prediction.HorizonDays);
        double delta = prediction.Predicted1RmKg - current1rm;
        target1rm = current1rm + delta * (daysAhead / prediction.HorizonDays);
    }
    else target1rm = current1rm;

    // Защита 1: не снижаем рекомендацию ниже текущего 1ПМ.
    if (target1rm < current1rm) target1rm = current1rm;
    // Защита 2: ограничение приращения за неделю.
    double maxTarget = current1rm * (1.0 + MaxWeeklyGrowth);
    if (target1rm > maxTarget) target1rm = maxTarget;

    double targetRpe = Math.Clamp(
        user.TargetRpe > 0 ? user.TargetRpe : DefaultTargetRpe, 6.0, 9.5);

    (int targetReps, int targetSets) = PickRepsAndSets(exercise, current);

    // ЯКОРНАЯ СХЕМА: рекомендация привязана к фактическому top_weight
    // последней тренировки, а не к 1ПМ при «другом» RPE.
    double growth = target1rm / current1rm;
    double lastReps = Math.Max(1, current.AvgReps);
    double rawWeight = current.TopWeight * growth
                       * (1.0 + lastReps / 30.0)
                       / (1.0 + targetReps / 30.0);

    // Округление до шага оборудования (2,5 / 5 / 4 кг).
    double step = WeightStep(exercise.EquipmentType);
    double weight = Math.Round(rawWeight / step) * step;
    if (weight < step) weight = step;

    // Защита 3: при тех же повторениях рекомендация не должна быть ниже
    // прошлого рабочего веса — исключает каскад снижения.
    if (Math.Abs(targetReps - lastReps) < 1.0 && weight < current.TopWeight)
        weight = current.TopWeight;

    return new PlanRecommendation(weight, targetReps, targetSets, targetRpe,
        $"План: {targetSets}×{targetReps} @ {weight:0.#} кг (RPE {targetRpe:0.#})");
}
```

---

### А.2 — Цикл двунаправленной синхронизации

**Файл:** `Services/Sync/SyncService.cs` (метод `RunOnceAsync`). Реализует одну итерацию обмена с сервером: формирует исходящий пакет, отправляет запрос с JWT-аутентификацией, применяет ответный пакет к локальной базе и сохраняет курсор синхронизации в клиентской временно́й шкале. Ключевой момент — защита от расхождения часов клиента и сервера (п. 3.7.5).

```csharp
public async Task<SyncStats> RunOnceAsync()
{
    var token = await _auth.GetTokenAsync()
                ?? throw new InvalidOperationException("Не залогинен — токен отсутствует.");

    // Фиксируем клиентское «сейчас» ДО отправки запроса — этот же
    // момент сохраним как новый курсор после успешного завершения.
    var clientNowAtStart = DateTime.UtcNow;
    var since = LastSyncUtc;

    // Защита от битого курсора: если LastSyncUtc оказался в будущем
    // относительно клиентских часов (расхождение часов или перевод
    // системного времени назад), курсор сбрасывается — иначе ни одна
    // локальная запись с UpdatedAt = DateTime.UtcNow не пройдёт фильтр.
    if (since.HasValue && since.Value > clientNowAtStart)
    {
        since = null;
        Preferences.Default.Remove(LastSyncKey);
    }

    var push = await BuildPushBatchAsync(since);

    using var req = new HttpRequestMessage(HttpMethod.Post, "sync")
    {
        Content = JsonContent.Create(push)
    };
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    using var resp = await _http.SendAsync(req);
    if (resp.StatusCode == HttpStatusCode.Unauthorized)
    {
        await _auth.LogoutAsync();
        throw new InvalidOperationException("Токен отклонён сервером. Войдите заново.");
    }
    resp.EnsureSuccessStatusCode();
    var pulled = await resp.Content.ReadFromJsonAsync<SyncResponse>()
                 ?? throw new InvalidOperationException("Пустой ответ /sync");

    var applied = await ApplyServerResponseAsync(pulled);

    // КРИТИЧЕСКИ ВАЖНО: сохраняется клиентское время старта, а НЕ
    // pulled.ServerTimeUtc — это устраняет расхождение часов между
    // клиентом и сервером (см. п. 3.7.5).
    Preferences.Default.Set(LastSyncKey, clientNowAtStart.ToString("o"));

    return new SyncStats(
        Pushed: push.Workouts.Count + push.WorkoutExercises.Count + push.ExerciseSets.Count,
        PulledNew: applied.inserted,
        PulledUpdated: applied.updated);
}
```

---

### А.3 — Сборка признакового вектора для ONNX-инференса

**Файл:** `Services/OnnxPredictionService.cs` (метод `BuildFeatures`). Воспроизводит на стороне клиента признаковый конвейер, идентичный Python-этапу обучения (раздел 4.4.1). Идентичность подтверждена расхождением прогнозов LightGBM (Python) и ONNX Runtime (C#) менее 0,001 кг на тестовой выборке.

```csharp
private float[]? BuildFeatures(
    User user, Exercise exercise, string? primaryMuscleName,
    IList<ExerciseWorkoutHistoryRow> history)
{
    if (_meta == null || _muscleToCode == null) return null;
    if (history.Count == 0) return null;

    // «Текущая» тренировка — последняя завершённая (история ASC).
    var current = history[^1];
    var prior  = history.Take(history.Count - 1).ToList();

    // Лаги top_1rm на K тренировок назад
    double? Lag(int k) =>
        prior.Count >= k ? prior[prior.Count - k].TopEpley1Rm : (double?)null;
    var lag1 = Lag(1); var lag2 = Lag(2);
    var lag3 = Lag(3); var lag5 = Lag(5);
    var diff1 = (lag1.HasValue && lag2.HasValue) ? lag1 - lag2 : null;

    // Скользящие средние и линейные наклоны по последним W тренировкам
    var last5_1rm = prior.Skip(Math.Max(0, prior.Count - 5))
                         .Select(r => r.TopEpley1Rm).ToArray();
    var last5_rpe = prior.Skip(Math.Max(0, prior.Count - 5))
                         .Select(r => r.AvgRpe).ToArray();
    var last3_1rm = prior.Skip(Math.Max(0, prior.Count - 3))
                         .Select(r => r.TopEpley1Rm).ToArray();
    double? mean(double[] a) => a.Length >= 2 ? a.Average()
                              : a.Length == 1 ? a[0] : (double?)null;
    double? slope_5 = LinregSlope(last5_1rm);
    double? slope_3 = LinregSlope(last3_1rm);

    double days_since_first = history.Count > 1
        ? (current.Date - history[0].Date).TotalDays : 0;
    double? days_since_last = prior.Count >= 1
        ? (current.Date - prior[^1].Date).TotalDays : null;

    // Анкета атлета (дефолты для отсутствующих полей)
    int    sex_male            = user.Sex == 1 ? 1 : 0;
    double bodyweight          = user.Bodyweight  > 0 ? user.Bodyweight  : 75.0;
    double age                 = user.Age         > 0 ? user.Age         : 28;
    double target_rpe          = user.TargetRpe   > 0 ? user.TargetRpe   : 7.5;
    double training_age_months = user.ExperienceStartDate.HasValue
        ? Math.Max(0, (DateTime.UtcNow - user.ExperienceStartDate.Value).TotalDays / 30.44)
        : 0;

    int primary_muscle_code =
        primaryMuscleName != null && _muscleToCode.TryGetValue(primaryMuscleName, out var c)
            ? c : -1;

    // Финальный вектор — порядок строго как в meta.FeatureColumns.
    // NaN допустим: LightGBM-ONNX-конвертер трактует его как отсутствие.
    float N(double? v) => v.HasValue ? (float)v.Value : float.NaN;
    var features = new float[]
    {
        N(lag1), N(lag2), N(lag3), N(lag5), N(diff1),
        N(mean(last5_1rm)), N(mean(last5_rpe)), N(slope_3), N(slope_5),
        prior.Count, (float)days_since_first, N(days_since_last),
        (float)current.TopEpley1Rm, (float)current.TopWeight,
        (float)current.NSets, (float)current.AvgRpe,
        (float)current.MinRpe, (float)current.MaxRpe, (float)current.AvgReps,
        sex_male, (float)bodyweight, (float)age,
        (float)training_age_months, (float)target_rpe,
        exercise.Id,
        exercise.Category == (int)ExerciseCategory.Compound ? 1 : 0,
        exercise.EquipmentType, primary_muscle_code,
    };
    return features.Length == _meta.FeatureColumns.Length ? features : null;
}
```

---

### А.4 — Симулятор одной тренировки виртуального атлета

**Файл:** `ml/generator/athlete.py` (метод `VirtualAthlete.simulate_workout`). Генерирует одну тренировочную сессию для виртуального атлета: рассчитывает рабочий вес по целевому RPE, добавляет 1–2 разминочных подхода, моделирует дрейф RPE и недобор повторений к концу серии, обновляет 1ПМ через метод `_apply_session_progression` (раздел 4.2.1).

```python
def simulate_workout(
    self, workout_date: date, exercise_ids: Iterable[int]
) -> list[dict]:
    """Симулирует одну тренировку. Возвращает список строк-сетов."""
    rows: list[dict] = []
    for order_idx, ex_id in enumerate(exercise_ids, start=1):
        ex = EXERCISE_BY_ID[ex_id]
        fatigue_here = self.fatigue.get(ex.primary_muscle, 0.0)
        target_rpe = self.profile.target_rpe
        # Целевые повторы — случайные из диапазона упражнения
        target_reps = self.rng.randint(*ex.rep_range)

        # 1–2 разминочных сета (для штанги/гантелей/тренажёров)
        warmup_sets = 0
        if ex.equipment in (0, 1, 2):
            warmup_sets = self.rng.choice([1, 2])
        base_working_weight = self._weight_for(ex, target_reps, target_rpe)
        step = 2.5 if ex.equipment in (0, 1) else 5.0

        set_num = 1
        for w in range(warmup_sets):
            frac = 0.4 + 0.2 * w  # 40 %, 60 %
            wt = max(step, round(base_working_weight * frac / step) * step)
            reps = self.rng.choice([8, 10, 12])
            rpe = round(self.rng.uniform(4.0, 6.0), 1)
            rows.append(self._row(
                workout_date, order_idx, ex, set_num,
                weight=wt, reps=reps, rpe=rpe, is_warmup=True,
            ))
            set_num += 1

        # Рабочие подходы (3–5)
        n_working = self.rng.randint(3, 5)
        rpes: list[float] = []
        for i in range(n_working):
            # Дрейф RPE вверх к последним подходам (накопление усталости)
            drift = i * 0.25
            actual_rpe = (
                target_rpe + drift + fatigue_here
                + self.rng.gauss(0.0, self.profile.noise_rpe)
            )
            actual_rpe = max(5.0, min(10.0, round(actual_rpe, 1)))
            # Иногда атлет не доходит до планового количества повторов
            reps_done = target_reps
            if actual_rpe >= 9.5 and self.rng.random() < 0.4:
                reps_done = max(1, target_reps - self.rng.randint(1, 2))
            rows.append(self._row(
                workout_date, order_idx, ex, set_num,
                weight=base_working_weight, reps=reps_done,
                rpe=actual_rpe, is_warmup=False,
            ))
            rpes.append(actual_rpe)
            set_num += 1

        # Прогрессия 1ПМ после упражнения — по минимальному RPE
        if rpes:
            self._apply_session_progression(ex, min(rpes))
        # Прирост усталости в мышечной группе
        self.fatigue[ex.primary_muscle] = (
            self.fatigue.get(ex.primary_muscle, 0.0) + 0.35
        )
    return rows
```

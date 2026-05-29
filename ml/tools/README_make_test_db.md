# Генератор тестовых данных для проверки ML-прогнозов (`make_test_db.py`)

Инструмент заливает в локальную БД FitApp правдоподобные истории тренировок и
печатает ожидаемый прогноз модели — чтобы можно было проверить ML-модуль прямо
в приложении (в т.ч. на защите ВКР).

## Зачем

ML-модуль (`Services/OnnxPredictionService.cs`) прогнозирует 1ПМ через 28 дней.
Чтобы увидеть прогноз в приложении, нужна история тренировок по упражнению.
Скрипт генерирует такую историю + считает, что модель *должна* выдать, той же
ONNX-моделью и тем же pipeline'ом фичей, что и приложение. Совпадение числа в
приложении с напечатанным = сквозная проверка работоспособности (БД → фичи →
ONNX → UI).

## Запуск

Из корня проекта `C:\Users\Gera1\dev\FitApp` (PowerShell):

```powershell
# 1) Сухой прогон: печатает прогнозы, создаёт ml\data\test_seed.sql. БД не трогает.
ml\.venv\Scripts\python.exe ml\tools\make_test_db.py

# 2) Заливка в БД (ЗАКРОЙ FitApp заранее). Делает резервную копию Workout.db.bak.
ml\.venv\Scripts\python.exe ml\tools\make_test_db.py --apply
```

Альтернатива заливки, если установлен `sqlite3` CLI:

```powershell
sqlite3 "<путь к Workout.db>" ".read ml\data\test_seed.sql"
```

После заливки открой FitApp → зайди в упражнения сценариев → сверь прогноз с
таблицей, которую напечатал скрипт.

## Сценарии (по умолчанию)

| Сценарий | Упражнение | Что проверяет |
|---|---|---|
| Травма+восстановление | Жим штанги лёжа | Заглавный кейс: после просадки (пик 126 → 90) модель прогнозирует **восстановление вверх**, а не падение |
| Стабильный рост | Тяга штанги в наклоне | Модель продолжает тренд вверх |
| Плато | Подъём штанги на бицепс | Модель держит уровень, не фантазирует рост |

Выбраны упражнения с первичной мышцей из `muscle_classes` модели (Грудь, Спина,
Бицепс) — чтобы `primary_muscle_code` кодировался корректно, а не как -1.

## Ключевые детали реализации (чтобы не сломать при правках)

- **Даты = .NET ticks** (`to_ticks`). sqlite-net хранит `DateTime` при
  `storeDateTimeAsTicks=true` (дефолт) как `bigint` ticks с эпохи 0001-01-01.
  Писать ISO-строки нельзя — C# их не прочитает.
- **Колонки таблиц** должны точно совпадать со схемой: `Workouts(id, name,
  Description, StartTime, UserId, UpdatedAt, IsDeleted, SyncId)`,
  `WorkoutExercises(...,WorkoutSyncId)`, `ExerciseSets(...,Kind,
  WorkoutExerciseSyncId)`. `SyncId` генерируется через `lower(hex(randomblob(16)))`.
- **Фильтры истории** (как в `GetExerciseWorkoutHistoryAsync`): сеты идут в
  выборку только если `Kind <> 1` (не разминка) и `Weight > 0`. Поэтому все
  рабочие сеты пишутся с `Kind=0` и реальным весом.
- **Pipeline фичей** в `build_features()` — построчная копия
  `OnnxPredictionService.BuildFeatures` (30 фичей, порядок из
  `meta.json["feature_columns"]`). При изменении фичей в C#/ноутбуке — править
  здесь синхронно.
- **Профиль юзера и id упражнений** читаются из реальной БД (`load_static_features`),
  поэтому прогноз скрипта точно совпадает с приложением (а не приблизительно).
  `exercise_id`, `equipment`, `primary_muscle_code`, пол/вес/возраст/стаж/target_rpe
  берутся из `Users` и `Exercises`.
- **Идемпотентность**: тренировки помечаются `[ТЕСТ-ML]` в имени; SQL сначала
  удаляет прошлые записи с этим маркером. Повторный прогон не плодит дубли и не
  трогает реальные тренировки пользователя.

## Пути (правь в шапке скрипта при необходимости)

- `DB_PATH` — рабочая БД MAUI:
  `C:\Users\Gera1\AppData\Local\User Name\com.companyname.fitapp\Data\Workout.db`
  (Windows, unpackaged). Найти можно так:
  `Get-ChildItem $env:LOCALAPPDATA -Recurse -Filter Workout.db`.
- Модель/мета: `ml\data\models\lightgbm_1rm_28d.onnx` / `.meta.json`.
- Выходной SQL: `ml\data\test_seed.sql`.

## Как добавить свой сценарий

В функции `make_scenarios()` создай `Scenario(title, exercise_name, rep_range,
targets, rng)`, где `targets` — список целевых top-1ПМ на каждую тренировку
(скрипт сам подберёт вес/повторы/RPE так, чтобы Эпли-1ПМ совпал с целью).
`exercise_name` должен точно совпадать с `Name` в таблице `Exercises`.

## Откат

Если заливка через `--apply` что-то испортила:

```powershell
Copy-Item "C:\Users\Gera1\AppData\Local\User Name\com.companyname.fitapp\Data\Workout.db.bak" `
          "C:\Users\Gera1\AppData\Local\User Name\com.companyname.fitapp\Data\Workout.db" -Force
```

"""
Генератор тестовых данных для проверки ML-прогнозов FitApp прямо в приложении.

Что делает:
  1. Строит несколько правдоподобных историй тренировок по упражнению
     (рост / травма+восстановление / плато) — по 2 тренировки в неделю.
  2. Складывает их в .sql-файл с INSERT'ами в таблицы Workouts /
     WorkoutExercises / ExerciseSets ровно с теми колонками и форматом дат
     (.NET ticks), которые ждёт sqlite-net в FitApp.
  3. Для ПОСЛЕДНЕЙ тренировки каждого сценария прогоняет ту же ONNX-модель
     с тем же pipeline'ом фичей, что и Services/OnnxPredictionService.cs,
     и печатает ОЖИДАЕМЫЙ прогноз 1ПМ через 28 дней — чтобы в приложении
     можно было сверить число.

Запуск (из корня репозитория, в venv c onnxruntime):
    ml/.venv/Scripts/python.exe ml/tools/make_test_db.py

Затем заливка в БД приложения:
    sqlite3 "C:\\Users\\<user>\\AppData\\Local\\User Name\\com.companyname.fitapp\\Data\\Workout.db" ".read ml/data/test_seed.sql"

(закрой приложение перед заливкой, открой после — на экране упражнения
появится прогноз; он должен совпасть с напечатанным здесь числом).

Скрипт НЕ трогает БД сам по себе — только читает её (профиль юзера и id
упражнений), чтобы посчитать прогноз так же, как это сделает приложение.
Запись в БД делаешь ты вручную командой sqlite3 — так безопаснее.
"""
from __future__ import annotations

import json
import sqlite3
import sys
from dataclasses import dataclass, field
from datetime import datetime, timedelta
from pathlib import Path

import numpy as np
import onnxruntime as ort

# --- Пути -------------------------------------------------------------------

REPO = Path(__file__).resolve().parents[2]
MODELS_DIR = REPO / "ml" / "data" / "models"
ONNX_MODEL = MODELS_DIR / "lightgbm_1rm_28d.onnx"
META_PATH = MODELS_DIR / "lightgbm_1rm_28d.meta.json"
OUT_SQL = REPO / "ml" / "data" / "test_seed.sql"

# Рабочая БД приложения (MAUI, Windows, unpackaged). Поменяй при необходимости.
DB_PATH = Path(
    r"C:\Users\Gera1\AppData\Local\User Name\com.companyname.fitapp\Data\Workout.db"
)

# Маркер в имени тренировки — по нему .sql удаляет свои прошлые вставки,
# чтобы повторный прогон не плодил дубли и не ломал чужие данные.
TEST_TAG = "[ТЕСТ-ML]"

NET_EPOCH = datetime(1, 1, 1)


def to_ticks(dt: datetime) -> int:
    """.NET DateTime.Ticks (100-нс интервалы с 0001-01-01) — формат, в котором
    sqlite-net хранит DateTime при storeDateTimeAsTicks=true (дефолт)."""
    delta = dt - NET_EPOCH
    return delta.days * 864_000_000_000 + delta.seconds * 10_000_000 + delta.microseconds * 10


def round_plate(x: float, step: float = 2.5) -> float:
    """Округление веса до реального шага блинов на штанге."""
    return round(round(x / step) * step, 1)


# --- Модель данных одной тренировки ----------------------------------------

@dataclass
class Session:
    date: datetime
    sets: list[tuple[float, int, float]]  # (weight, reps, rpe)

    # Агрегаты ОДНОЙ тренировки — как в GetExerciseWorkoutHistoryAsync.
    @property
    def top_1rm(self) -> float:
        return max(w * (1 + r / 30) for w, r, _ in self.sets)

    @property
    def top_weight(self) -> float:
        return max(w for w, _, _ in self.sets)

    @property
    def n_sets(self) -> int:
        return len(self.sets)

    @property
    def avg_rpe(self) -> float:
        return float(np.mean([e for _, _, e in self.sets]))

    @property
    def min_rpe(self) -> float:
        return float(min(e for _, _, e in self.sets))

    @property
    def max_rpe(self) -> float:
        return float(max(e for _, _, e in self.sets))

    @property
    def avg_reps(self) -> float:
        return float(np.mean([r for _, r, _ in self.sets]))


@dataclass
class Scenario:
    title: str
    exercise_name: str          # точное имя из Exercises (для подзапроса в SQL)
    rep_range: tuple[int, int]
    targets: list[float]        # целевой top-1ПМ на каждую тренировку
    rng: np.random.Generator = field(default_factory=lambda: np.random.default_rng(0))

    def build_sessions(self, last_date: datetime, step_days: int = 3) -> list[Session]:
        """Раскладывает targets в тренировки назад во времени от last_date,
        подбирая вес/повторы/RPE так, чтобы top-1ПМ совпал с целью."""
        n = len(self.targets)
        dates = [last_date - timedelta(days=step_days * (n - 1 - i)) for i in range(n)]
        sessions: list[Session] = []
        lo, hi = self.rep_range
        for d, target in zip(dates, self.targets):
            top_reps = int(self.rng.integers(lo, hi + 1))
            top_weight = round_plate(target / (1 + top_reps / 30))
            # «Тяжесть» сета привязана к близости 1ПМ к историческому максимуму:
            base_rpe = 7.5
            top_set = (top_weight, top_reps, round(min(10.0, base_rpe + 1.0), 1))
            # Два рабочих бэкофф-сета чуть легче и с большим числом повторов.
            back_w = round_plate(top_weight * 0.92)
            back_reps = min(hi, top_reps + 2)
            backoff = (back_w, back_reps, round(base_rpe, 1))
            sessions.append(Session(d, [top_set, backoff, backoff]))
        return sessions


# --- Фичи: точная копия логики OnnxPredictionService.BuildFeatures ----------

def linreg_slope(y: list[float]) -> float | None:
    n = len(y)
    if n < 2:
        return None
    x_mean = (n - 1) / 2.0
    y_mean = float(np.mean(y))
    num = den = 0.0
    for i in range(n):
        dx = i - x_mean
        num += dx * (y[i] - y_mean)
        den += dx * dx
    return None if den == 0 else num / den


def mean_if_enough(arr: list[float]) -> float | None:
    if len(arr) >= 2:
        return float(np.mean(arr))
    if len(arr) == 1:
        return arr[0]
    return None


def build_features(
    sessions: list[Session],
    feature_cols: list[str],
    static: dict[str, float],
) -> np.ndarray:
    """history = sessions (ASC по дате). current = последняя, prior = всё до неё."""
    current = sessions[-1]
    prior = sessions[:-1]
    prior_top = [s.top_1rm for s in prior]
    prior_rpe = [s.avg_rpe for s in prior]

    def lag(k: int) -> float | None:
        return prior_top[-k] if len(prior) >= k else None

    lag1, lag2, lag3, lag5 = lag(1), lag(2), lag(3), lag(5)
    diff1 = (lag1 - lag2) if (lag1 is not None and lag2 is not None) else None

    last5_top = prior_top[-5:]
    last5_rpe = prior_rpe[-5:]
    last3_top = prior_top[-3:]

    mean_1rm_5 = mean_if_enough(last5_top)
    mean_rpe_5 = mean_if_enough(last5_rpe)
    slope_5 = linreg_slope(last5_top)
    slope_3 = linreg_slope(last3_top)

    max_1rm_hist = max(prior_top) if prior_top else None
    drop_from_peak = (max_1rm_hist - lag1) if (max_1rm_hist is not None and lag1 is not None) else None

    n_history = len(prior)
    days_since_first = (current.date - sessions[0].date).days if len(sessions) > 1 else 0.0
    days_since_last = (current.date - prior[-1].date).days if prior else None

    feats = {
        "lag_1rm_1": lag1, "lag_1rm_2": lag2, "lag_1rm_3": lag3, "lag_1rm_5": lag5,
        "diff_1": diff1,
        "mean_1rm_5": mean_1rm_5, "mean_rpe_5": mean_rpe_5,
        "slope_3": slope_3, "slope_5": slope_5,
        "max_1rm_hist": max_1rm_hist, "drop_from_peak": drop_from_peak,
        "n_history": float(n_history),
        "days_since_first": float(days_since_first),
        "days_since_last": days_since_last,
        "top_1rm": current.top_1rm, "top_weight": current.top_weight,
        "n_sets": float(current.n_sets), "avg_rpe": current.avg_rpe,
        "min_rpe": current.min_rpe, "max_rpe": current.max_rpe,
        "avg_reps": current.avg_reps,
        **static,
    }
    vec = [feats[c] if feats[c] is not None else np.nan for c in feature_cols]
    return np.array([vec], dtype=np.float32)


# --- Чтение профиля юзера из БД (как делает C#) -----------------------------

def load_static_features(db: sqlite3.Connection, exercise_name: str,
                         muscle_classes: list[str]) -> tuple[dict[str, float], int]:
    cur = db.cursor()
    u = cur.execute(
        "SELECT Sex, Bodyweight, Age, ExperienceStartDate, TargetRpe FROM Users WHERE Id=1"
    ).fetchone()
    sex, bw, age, exp_ticks, target_rpe = u

    sex_male = 1.0 if sex == 1 else 0.0
    bodyweight = bw if bw and bw > 0 else 75.0
    age_v = age if age and age > 0 else 28.0
    if exp_ticks and exp_ticks > 0:
        exp_date = NET_EPOCH + timedelta(microseconds=exp_ticks // 10)
        training_age_months = max(0.0, (datetime.utcnow() - exp_date).days / 30.44)
    else:
        training_age_months = 0.0
    target_rpe_v = target_rpe if target_rpe and target_rpe > 0 else 7.5

    ex = cur.execute(
        "SELECT Id, EquipmentType, Category, PrimaryMuscleGroupId FROM Exercises WHERE Name=?",
        (exercise_name,),
    ).fetchone()
    if ex is None:
        raise SystemExit(f"Упражнение не найдено в БД: {exercise_name!r}")
    ex_id, equipment, category, prim_mg_id = ex

    # primary_muscle_code — как GetPrimaryMuscleNameAsync → индекс в muscle_classes.
    muscle_row = cur.execute(
        "SELECT Name FROM MuscleGroup WHERE Id=?", (prim_mg_id,)
    ).fetchone()
    muscle_name = muscle_row[0] if muscle_row else None
    code = muscle_classes.index(muscle_name) if muscle_name in muscle_classes else -1

    is_compound = 1.0 if category == 0 else 0.0  # ExerciseCategory.Compound == 0

    static = {
        "sex_male": sex_male,
        "bodyweight": float(bodyweight),
        "age": float(age_v),
        "training_age_months": float(training_age_months),
        "target_rpe_profile": float(target_rpe_v),
        "exercise_id": float(ex_id),
        "is_compound": is_compound,
        "equipment": float(equipment),
        "primary_muscle_code": float(code),
    }
    return static, ex_id


# --- Генерация SQL ----------------------------------------------------------

def emit_sql(scenarios_sessions: list[tuple[Scenario, list[Session]]]) -> str:
    lines: list[str] = [
        "-- Автосгенерировано ml/tools/make_test_db.py — тестовые истории для проверки ML.",
        "-- Заливка:  sqlite3 \"<путь>\\Workout.db\" \".read ml/data/test_seed.sql\"",
        "BEGIN TRANSACTION;",
        "",
        f"-- 1) Чистим прошлые тестовые вставки (по маркеру {TEST_TAG} в имени тренировки).",
        "DELETE FROM ExerciseSets WHERE WorkoutExerciseId IN ("
        "  SELECT we.Id FROM WorkoutExercises we JOIN Workouts w ON we.WorkoutId=w.id"
        f"  WHERE w.name LIKE '{TEST_TAG}%');",
        "DELETE FROM WorkoutExercises WHERE WorkoutId IN ("
        f"  SELECT id FROM Workouts WHERE name LIKE '{TEST_TAG}%');",
        f"DELETE FROM Workouts WHERE name LIKE '{TEST_TAG}%';",
        "",
        "-- 2) Вставляем сценарии.",
    ]

    for scen, sessions in scenarios_sessions:
        ex_subq = (
            f"(SELECT Id FROM Exercises WHERE Name='{scen.exercise_name.replace(chr(39), chr(39)*2)}' LIMIT 1)"
        )
        lines.append("")
        lines.append(f"-- === {scen.title} — {scen.exercise_name} ===")
        for i, s in enumerate(sessions):
            wname = f"{TEST_TAG} {scen.title} #{i + 1}"
            ticks = to_ticks(s.date)
            lines.append(
                f"INSERT INTO Workouts(name,Description,StartTime,UserId,UpdatedAt,IsDeleted,SyncId) "
                f"VALUES('{wname}','',{ticks},1,{ticks},0,lower(hex(randomblob(16))));"
            )
            lines.append(
                "INSERT INTO WorkoutExercises(WorkoutId,ExerciseId,OrderIndex,UpdatedAt,IsDeleted,SyncId,WorkoutSyncId) "
                f"VALUES(last_insert_rowid(),{ex_subq},1,{ticks},0,lower(hex(randomblob(16))),"
                "(SELECT SyncId FROM Workouts WHERE id=last_insert_rowid()));"
            )
            for sn, (w, r, rpe) in enumerate(s.sets, start=1):
                lines.append(
                    "INSERT INTO ExerciseSets(WorkoutExerciseId,SetNumber,Weight,Reps,RPE,IsAssisted,Kind,UpdatedAt,IsDeleted,SyncId,WorkoutExerciseSyncId) "
                    f"VALUES(last_insert_rowid(),{sn},{w},{r},{rpe},0,0,{ticks},0,"
                    "lower(hex(randomblob(16))),"
                    "(SELECT SyncId FROM WorkoutExercises WHERE id=last_insert_rowid()));"
                )
    lines.append("")
    lines.append("COMMIT;")
    return "\n".join(lines)


# --- Сборка сценариев -------------------------------------------------------

def make_scenarios() -> list[Scenario]:
    rng = np.random.default_rng(42)

    # 1) Травма + восстановление (заглавный сценарий): рост 100→125, резкая
    #    просадка после травмы до ~90, затем восстановление к пику (но ещё не
    #    дошёл — модель должна прогнозировать продолжение роста вверх).
    grow = list(np.linspace(100, 125, 12))
    injury = [90.0]                                   # травма: -28% от пика
    recover = list(np.linspace(94, 106, 9))           # частично отбился, но ещё заметно ниже пика 125
    bench = Scenario(
        "Травма+восстановление", "Жим штанги лёжа", (4, 8),
        [round(x, 1) for x in grow + injury + recover],
        np.random.default_rng(1),
    )

    # 2) Стабильный рост: плавная логистическая кривая 70→92.
    t = np.linspace(0, 1, 18)
    growth_curve = 70 + 22 * (1 / (1 + np.exp(-6 * (t - 0.5))))
    row = Scenario(
        "Стабильный рост", "Тяга штанги в наклоне", (5, 10),
        [round(float(x), 1) for x in growth_curve],
        np.random.default_rng(2),
    )

    # 3) Плато: топчется вокруг 50 кг с лёгким шумом.
    plateau = 50 + rng.normal(0, 0.6, 16)
    curl = Scenario(
        "Плато", "Подъём штанги на бицепс", (6, 12),
        [round(float(x), 1) for x in plateau],
        np.random.default_rng(3),
    )
    return [bench, row, curl]


def main() -> None:
    meta = json.loads(META_PATH.read_text(encoding="utf-8"))
    feature_cols = meta["feature_columns"]
    muscle_classes = meta["muscle_classes"]
    horizon = meta["horizon_days"]

    if not DB_PATH.exists():
        raise SystemExit(f"Не найдена БД приложения: {DB_PATH}\n"
                         "Запусти приложение хотя бы раз или поправь DB_PATH в скрипте.")
    db = sqlite3.connect(str(DB_PATH))

    session = ort.InferenceSession(str(ONNX_MODEL))
    in_name = session.get_inputs()[0].name
    out_name = session.get_outputs()[0].name

    last_date = datetime.now().replace(hour=18, minute=0, second=0, microsecond=0) - timedelta(days=1)

    scenarios = make_scenarios()
    built: list[tuple[Scenario, list[Session]]] = []

    print(f"\nОжидаемые прогнозы модели (через {horizon} дней), сверь с приложением:\n")
    print(f"{'Сценарий':<26}{'Упражнение':<26}{'Тек.1ПМ':>9}{'Пик':>8}{'Прогноз':>10}")
    print("-" * 79)
    for scen in scenarios:
        sessions = scen.build_sessions(last_date)
        built.append((scen, sessions))

        static, ex_id = load_static_features(db, scen.exercise_name, muscle_classes)
        X = build_features(sessions, feature_cols, static)
        pred = float(np.asarray(session.run([out_name], {in_name: X})[0]).reshape(-1)[0])

        cur_1rm = sessions[-1].top_1rm
        peak = max(s.top_1rm for s in sessions)
        print(f"{scen.title:<26}{scen.exercise_name:<26}"
              f"{cur_1rm:>9.1f}{peak:>8.1f}{pred:>10.1f}")

    db.close()

    sql_text = emit_sql(built)
    OUT_SQL.write_text(sql_text, encoding="utf-8")
    print(f"\nSQL записан: {OUT_SQL}")

    if "--apply" in sys.argv:
        # Прямая заливка в БД (когда нет sqlite3 CLI). Делаем .bak на всякий.
        backup = DB_PATH.with_suffix(DB_PATH.suffix + ".bak")
        backup.write_bytes(DB_PATH.read_bytes())
        conn = sqlite3.connect(str(DB_PATH))
        conn.executescript(sql_text)
        conn.commit()
        conn.close()
        print(f"Залито в БД: {DB_PATH}")
        print(f"Резервная копия: {backup}")
        print("ВАЖНО: FitApp должен быть закрыт во время заливки. Теперь открой приложение.")
    else:
        print("Залить в приложение (закрой FitApp перед этим). Вариант А — sqlite3 CLI:")
        print(f'  sqlite3 "{DB_PATH}" ".read {OUT_SQL.as_posix()}"')
        print("Вариант Б — без sqlite3, прямо этим скриптом:")
        print("  ml/.venv/Scripts/python.exe ml/tools/make_test_db.py --apply")


if __name__ == "__main__":
    main()

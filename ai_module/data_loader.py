import sqlite3
import random
import numpy as np
import pandas as pd
from pathlib import Path


DB_PATH = Path(__file__).parent.parent / "fitapp.db"


def load_from_sqlite(db_path: str | Path = DB_PATH) -> pd.DataFrame:
    """Экспортирует историю подходов из SQLite FitApp в DataFrame."""
    conn = sqlite3.connect(db_path)
    query = """
        SELECT
            es.Id        AS set_id,
            es.Weight    AS weight,
            es.Reps      AS reps,
            es.RPE       AS rpe,
            we.ExerciseId AS exercise_id,
            w.StartTime  AS date
        FROM ExerciseSets es
        JOIN WorkoutExercises we ON es.WorkoutExerciseId = we.Id
        JOIN Workouts w          ON we.WorkoutId = w.id
        ORDER BY we.ExerciseId, w.StartTime
    """
    df = pd.read_sql(query, conn)
    conn.close()
    df["date"] = pd.to_datetime(df["date"])
    return df


def generate_synthetic(n_exercises: int = 20, sessions_per: int = 60) -> pd.DataFrame:
    """Генерирует синтетические тренировочные логи по правилу прогрессивной перегрузки."""
    rows = []
    for ex_id in range(1, n_exercises + 1):
        weight = random.uniform(40, 100)
        date = pd.Timestamp("2023-01-01")
        for i in range(sessions_per):
            rpe = float(np.clip(np.random.normal(7.0, 0.8), 5.0, 10.0))
            reps = int(np.clip(np.random.normal(5, 1), 1, 12))
            rows.append({
                "exercise_id": ex_id,
                "weight": round(weight, 1),
                "reps": reps,
                "rpe": round(rpe, 1),
                "date": date,
            })
            date += pd.Timedelta(days=random.choice([2, 3, 4]))
            if rpe > 9.0:
                weight = max(weight - 10, 20)
            elif i % 2 == 0:
                weight += 2.5
    return pd.DataFrame(rows)


def build_features(df: pd.DataFrame) -> pd.DataFrame:
    """
    Строит обучающие фичи из сырого лога.
    Для каждой сессии добавляет признаки на основе предыдущих 3 сессий.
    Таргеты: next_weight, next_reps.
    """
    records = []
    for ex_id, group in df.groupby("exercise_id"):
        group = group.sort_values("date").reset_index(drop=True)
        for i in range(3, len(group)):
            prev = group.iloc[i - 3:i]
            curr = group.iloc[i]
            records.append({
                "exercise_id": ex_id,
                "avg_weight_last3": prev["weight"].mean(),
                "avg_reps_last3": prev["reps"].mean(),
                "avg_rpe_last3": prev["rpe"].mean(),
                "delta_weight": prev["weight"].iloc[-1] - prev["weight"].iloc[0],
                "days_since_last": (curr["date"] - prev["date"].iloc[-1]).days,
                "session_index": i,
                "next_weight": curr["weight"],
                "next_reps": curr["reps"],
            })
    return pd.DataFrame(records)


def prepare_dataset(db_path: str | Path = DB_PATH) -> pd.DataFrame:
    """Собирает финальный датасет: реальные данные + синтетика."""
    frames = [generate_synthetic()]

    if Path(db_path).exists():
        real = load_from_sqlite(db_path)
        if not real.empty:
            frames.append(real)

    raw = pd.concat(frames, ignore_index=True)
    return build_features(raw)

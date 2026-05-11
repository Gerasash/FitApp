import numpy as np
import joblib
from pathlib import Path
from sklearn.linear_model import Ridge
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

MODEL_PATH = Path(__file__).parent / "model.pkl"

FEATURES = [
    "avg_weight_last3",
    "avg_reps_last3",
    "avg_rpe_last3",
    "delta_weight",
    "days_since_last",
    "session_index",
]


def build_model() -> Pipeline:
    return Pipeline([
        ("scaler", StandardScaler()),
        ("ridge", Ridge(alpha=1.0)),
    ])


def train(df):
    """Обучает две модели (вес и повторения) и сохраняет в model.pkl."""
    X = df[FEATURES].values
    y_weight = df["next_weight"].values
    y_reps = df["next_reps"].values

    model_weight = build_model()
    model_weight.fit(X, y_weight)

    model_reps = build_model()
    model_reps.fit(X, y_reps)

    joblib.dump({"weight": model_weight, "reps": model_reps}, MODEL_PATH)
    print(f"Модель сохранена: {MODEL_PATH}")


def load_models():
    if not MODEL_PATH.exists():
        raise FileNotFoundError("model.pkl не найден, запусти train.py")
    return joblib.load(MODEL_PATH)


def predict(history: list[dict]) -> dict:
    """
    history — список dict с ключами weight, reps, rpe, date (ISO str).
    Возвращает {predicted_weight, predicted_reps, trend, text}.
    """
    models = load_models()

    import pandas as pd
    df = pd.DataFrame(history)
    df["date"] = pd.to_datetime(df["date"])
    df = df.sort_values("date").tail(4)

    if len(df) < 3:
        return _fallback(df)

    prev = df.iloc[-3:]
    last = df.iloc[-1]
    days_since = (pd.Timestamp.now(tz="UTC") - last["date"].tz_convert("UTC")).days

    X = np.array([[
        prev["weight"].mean(),
        prev["reps"].mean(),
        prev["rpe"].mean(),
        prev["weight"].iloc[-1] - prev["weight"].iloc[0],
        max(days_since, 1),
        len(df),
    ]])

    pred_weight = round(float(models["weight"].predict(X)[0]), 1)
    pred_reps = max(1, int(round(float(models["reps"].predict(X)[0]))))
    trend = get_trend(df["weight"].tolist())

    return {
        "predicted_weight": pred_weight,
        "predicted_reps": pred_reps,
        "trend": trend,
        "text": _insight_text(pred_weight, pred_reps, trend, prev["weight"].mean()),
    }


def get_trend(weights: list[float]) -> str:
    if len(weights) < 3:
        return "unknown"
    w = weights[-5:]
    x = np.arange(len(w))
    slope = float(np.polyfit(x, w, 1)[0])
    if slope > 0.5:
        return "growth"
    if slope < -0.5:
        return "regress"
    return "plateau"


TREND_RU = {"growth": "Прогресс", "plateau": "Стабильно", "regress": "Нужен отдых", "unknown": ""}


def _insight_text(pred_weight: float, pred_reps: int, trend: str, avg_prev: float) -> str:
    delta = pred_weight - avg_prev
    label = TREND_RU.get(trend, "")
    if trend == "growth":
        return f"{label}! {pred_weight} kg x {pred_reps} ({delta:+.1f} kg)"
    if trend == "regress":
        return f"{label}. {pred_weight} kg x {pred_reps}"
    return f"{label}. {pred_weight} kg x {pred_reps}"


def _fallback(df) -> dict:
    if df.empty:
        return {"predicted_weight": 0, "predicted_reps": 0, "trend": "unknown", "text": "No history"}
    last = df.iloc[-1]
    return {
        "predicted_weight": float(last["weight"]),
        "predicted_reps": int(last["reps"]),
        "trend": "unknown",
        "text": f"Repeat: {last['weight']} kg x {int(last['reps'])}",
    }

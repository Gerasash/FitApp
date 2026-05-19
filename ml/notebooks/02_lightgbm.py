# ---
# title: "Этап 3 — LightGBM для прогноза 1ПМ через 28 дней"
# description: |
#   Строим табличные фичи (лаги, наклоны, окна по RPE, анкета атлета,
#   мета упражнения) и обучаем градиентный бустинг LightGBM. Сравниваем
#   с baseline'ами на тех же тестовых атлетах. Сохраняем модель для
#   последующего ONNX-экспорта.
# ---

# %% [markdown]
# # Этап 3 — LightGBM
#
# **Задача:** та же, что в `01_baselines.py` — предсказать `top_1rm`
# (Epley) ближайшей тренировки данного упражнения данного атлета,
# прошедшей через >=28 дней от текущей.
#
# **Сравниваем с baseline'ами** из этапа 2 (Naive 2.46 / Rolling-3 2.72 /
# LinReg-5 2.24 кг MAE). Цель — обогнать LinReg-5 на 10–20%.

# %%
from __future__ import annotations
import warnings
from pathlib import Path
import numpy as np
import pandas as pd
import lightgbm as lgb

warnings.filterwarnings("ignore", category=FutureWarning)
pd.set_option("display.float_format", "{:.3f}".format)
pd.set_option("display.max_columns", 50)

DATA_PATH = Path(__file__).resolve().parents[1] / "data" / "synth.csv"
MODELS_DIR = Path(__file__).resolve().parents[1] / "data" / "models"
MODELS_DIR.mkdir(parents=True, exist_ok=True)

HORIZON_DAYS = 28
RNG_SEED = 42

print("Reading", DATA_PATH)
df = pd.read_csv(DATA_PATH)
print(f"Rows: {len(df):,}  Athletes: {df['athlete_id'].nunique()}  "
      f"Exercises: {df['exercise_id'].nunique()}")

# %% [markdown]
# ## Агрегация подходов в результат тренировки
#
# Та же логика, что в baseline-notebook: одна строка на (athlete, exercise,
# date). Берём только рабочие подходы, считаем top_1rm по Эпли.

# %%
work = df[~df["is_warmup"]].copy()
work["epley_1rm"] = work["weight_kg"] * (1 + work["reps"] / 30)

agg = (
    work.groupby(["athlete_id", "exercise_id", "date"], as_index=False)
        .agg(
            top_1rm=("epley_1rm", "max"),
            top_weight=("weight_kg", "max"),
            n_sets=("set_number", "count"),
            avg_rpe=("rpe", "mean"),
            min_rpe=("rpe", "min"),
            max_rpe=("rpe", "max"),
            avg_reps=("reps", "mean"),
            sex=("sex", "first"),
            bodyweight=("bodyweight_kg", "first"),
            age=("age", "first"),
            training_age_months=("training_age_months", "first"),
            target_rpe_profile=("target_rpe_profile", "first"),
            is_compound=("is_compound", "first"),
            equipment=("equipment", "first"),
            primary_muscle=("primary_muscle", "first"),
        )
)
agg["date_dt"] = pd.to_datetime(agg["date"])
agg = agg.sort_values(["athlete_id", "exercise_id", "date_dt"]).reset_index(drop=True)
print(f"Aggregated rows: {len(agg):,}")

# %% [markdown]
# ## Целевая переменная — 1ПМ через >= 28 дней

# %%
def _targets_for_group(g: pd.DataFrame) -> pd.DataFrame:
    dates = g["date_dt"].values
    one_rms = g["top_1rm"].values
    deadlines = dates + np.timedelta64(HORIZON_DAYS, "D")
    j = np.searchsorted(dates, deadlines, side="left")
    found = j < len(dates)
    j_clipped = np.clip(j, 0, len(dates) - 1)
    return pd.DataFrame(
        {
            "target_1rm": np.where(found, one_rms[j_clipped], np.nan),
            "target_gap_days": np.where(
                found,
                (dates[j_clipped] - dates).astype("timedelta64[D]").astype(float),
                np.nan,
            ),
        },
        index=g.index,
    )

targets = (
    agg.groupby(["athlete_id", "exercise_id"], group_keys=False, sort=False)
       [["date_dt", "top_1rm"]]
       .apply(_targets_for_group)
)
agg = agg.join(targets)

# %% [markdown]
# ## Feature engineering
#
# Строим лаги и скользящие статистики **внутри серии (athlete, exercise)**.
# Все фичи доступны на момент текущей тренировки — никакой утечки из будущего.
#
# - `lag_1rm_1..lag_1rm_5` — top_1rm на 1..5 тренировок назад
# - `diff_1` — приращение от предыдущей тренировки (lag_0 - lag_1)
# - `slope_5`, `slope_3` — наклон прямой по последним 5 / 3 точкам (кг/тренировка)
# - `mean_1rm_5`, `mean_rpe_5` — средние за последние 5 тренировок
# - `n_history` — сколько тренировок этого упражнения было до текущей
# - `days_since_first` — стаж в днях по этому упражнению у атлета
# - `days_since_last` — пауза между текущей и предыдущей тренировкой
# - `progress_proxy` — груботкое (lag_1rm_5 - lag_1rm_1) / 5 как индикатор тренда

# %%
def _linreg_slope(y: np.ndarray) -> float:
    """Наклон прямой y = m*x + b на индексах 0..n-1. Если точек <2, возвращаем NaN."""
    n = len(y)
    if n < 2:
        return np.nan
    x = np.arange(n, dtype=float)
    x_mean = x.mean()
    y_mean = y.mean()
    denom = ((x - x_mean) ** 2).sum()
    if denom == 0:
        return np.nan
    return float(((x - x_mean) * (y - y_mean)).sum() / denom)


def _build_features_per_group(g: pd.DataFrame) -> pd.DataFrame:
    g = g.reset_index(drop=False).rename(columns={"index": "_orig_idx"})
    y = g["top_1rm"].values
    rpe = g["avg_rpe"].values
    dates = g["date_dt"].values
    n = len(g)

    # Лаги top_1rm
    out = {}
    for k in (1, 2, 3, 5):
        out[f"lag_1rm_{k}"] = np.concatenate([np.full(k, np.nan), y[:-k]]) if n > k else np.full(n, np.nan)

    # Разность с предыдущим
    out["diff_1"] = out["lag_1rm_1"] - out.get("lag_1rm_2", np.full(n, np.nan))

    # Скользящие средние (по строкам строго ДО текущей, не включая её)
    def _expanding_mean(values: np.ndarray, window: int) -> np.ndarray:
        res = np.full(n, np.nan)
        for i in range(n):
            start = max(0, i - window)
            if i - start >= 2:
                res[i] = values[start:i].mean()
            elif i - start == 1:
                res[i] = values[start]
        return res

    out["mean_1rm_5"] = _expanding_mean(y, 5)
    out["mean_rpe_5"] = _expanding_mean(rpe, 5)

    # Наклоны прямых на скользящих окнах
    def _rolling_slope(values: np.ndarray, window: int) -> np.ndarray:
        res = np.full(n, np.nan)
        for i in range(n):
            start = max(0, i - window)
            if i - start >= 2:
                res[i] = _linreg_slope(values[start:i])
        return res

    out["slope_3"] = _rolling_slope(y, 3)
    out["slope_5"] = _rolling_slope(y, 5)

    # Счётчики и временные интервалы
    out["n_history"] = np.arange(n, dtype=float)
    out["days_since_first"] = (dates - dates[0]).astype("timedelta64[D]").astype(float)
    days_since_last = np.full(n, np.nan)
    if n > 1:
        days_since_last[1:] = (dates[1:] - dates[:-1]).astype("timedelta64[D]").astype(float)
    out["days_since_last"] = days_since_last

    feat = pd.DataFrame(out, index=g["_orig_idx"].values)
    feat.index.name = None
    return feat


# Применяем построение фичей по группам
feature_chunks = []
for (_, _), g in agg.groupby(["athlete_id", "exercise_id"], sort=False):
    feature_chunks.append(_build_features_per_group(g))
features = pd.concat(feature_chunks).sort_index()
agg = agg.join(features)

print(f"Feature columns added. Total agg shape: {agg.shape}")

# %% [markdown]
# ## Финальный фрейм для обучения
#
# - Оставляем только пары, у которых есть и таргет, и **хотя бы один лаг**
#   (чтобы у модели было на что опереться).
# - Кодируем `sex` (M/F → 1/0).
# - `primary_muscle` и `equipment` оставляем числовыми (LightGBM съест как
#   категориальные).

# %%
data = agg.dropna(subset=["target_1rm", "lag_1rm_1"]).copy()
data["sex_male"] = (data["sex"] == "M").astype(int)

# Категориальные фичи — пометим явно для LightGBM
muscle_cat = data["primary_muscle"].astype("category")
data["primary_muscle_code"] = muscle_cat.cat.codes
muscle_classes = list(muscle_cat.cat.categories)

FEATURE_COLS = [
    # История по упражнению
    "lag_1rm_1", "lag_1rm_2", "lag_1rm_3", "lag_1rm_5",
    "diff_1",
    "mean_1rm_5", "mean_rpe_5",
    "slope_3", "slope_5",
    "n_history",
    "days_since_first", "days_since_last",
    # Текущая тренировка
    "top_1rm",
    "top_weight",
    "n_sets",
    "avg_rpe", "min_rpe", "max_rpe",
    "avg_reps",
    # Атлет
    "sex_male", "bodyweight", "age", "training_age_months", "target_rpe_profile",
    # Упражнение
    "exercise_id", "is_compound", "equipment", "primary_muscle_code",
]
CATEGORICAL_COLS = ["exercise_id", "equipment", "primary_muscle_code", "sex_male", "is_compound"]
TARGET_COL = "target_1rm"

print(f"Examples after filtering: {len(data):,}")
print(f"Features: {len(FEATURE_COLS)}")

# %% [markdown]
# ## Train / Val / Test — сплит по атлетам
#
# Тот же сид (42) и та же доля тестовых атлетов (20%), что и в baseline,
# чтобы метрики были сравнимы.

# %%
rng = np.random.default_rng(RNG_SEED)
all_athletes = data["athlete_id"].unique()
shuffled = all_athletes.copy()
rng.shuffle(shuffled)
n_test = int(len(shuffled) * 0.2)
n_val = int(len(shuffled) * 0.1)
test_athletes = set(shuffled[:n_test])
val_athletes = set(shuffled[n_test : n_test + n_val])
train_athletes = set(shuffled[n_test + n_val :])

train = data[data["athlete_id"].isin(train_athletes)]
val = data[data["athlete_id"].isin(val_athletes)]
test = data[data["athlete_id"].isin(test_athletes)]
print(f"Train: {len(train):,} ({len(train_athletes)} athletes)")
print(f"Val:   {len(val):,} ({len(val_athletes)} athletes)")
print(f"Test:  {len(test):,} ({len(test_athletes)} athletes)")

X_train, y_train = train[FEATURE_COLS], train[TARGET_COL]
X_val, y_val = val[FEATURE_COLS], val[TARGET_COL]
X_test, y_test = test[FEATURE_COLS], test[TARGET_COL]

# %% [markdown]
# ## Обучение LightGBM
#
# Параметры подобраны эвристически (для диплома — отправная точка, можно
# далее тюнить через Optuna).
# Цель — `regression_l1` (MAE), потому что у нас тяжёлые хвосты по 1ПМ
# (опытные атлеты с большими весами).

# %%
train_set = lgb.Dataset(X_train, label=y_train, categorical_feature=CATEGORICAL_COLS)
val_set = lgb.Dataset(X_val, label=y_val, categorical_feature=CATEGORICAL_COLS,
                      reference=train_set)

params = {
    "objective": "regression_l1",   # MAE
    "metric": "l1",
    "learning_rate": 0.05,
    "num_leaves": 63,
    "feature_fraction": 0.85,
    "bagging_fraction": 0.85,
    "bagging_freq": 5,
    "min_data_in_leaf": 50,
    "lambda_l2": 1.0,
    "verbose": -1,
    "seed": RNG_SEED,
}

model = lgb.train(
    params,
    train_set,
    num_boost_round=2000,
    valid_sets=[train_set, val_set],
    valid_names=["train", "val"],
    callbacks=[
        lgb.early_stopping(stopping_rounds=50),
        lgb.log_evaluation(period=100),
    ],
)

# %% [markdown]
# ## Метрики на тесте

# %%
def mae(p, t): return float(np.mean(np.abs(p - t)))
def rmse(p, t): return float(np.sqrt(np.mean((p - t) ** 2)))
def mape(p, t): return float(np.mean(np.abs((p - t) / t)) * 100)

pred_test = model.predict(X_test, num_iteration=model.best_iteration)
metrics = {
    "mae": mae(pred_test, y_test.values),
    "rmse": rmse(pred_test, y_test.values),
    "mape": mape(pred_test, y_test.values),
}
print(f"\nLightGBM test:")
print(f"  MAE  = {metrics['mae']:.3f} kg")
print(f"  RMSE = {metrics['rmse']:.3f} kg")
print(f"  MAPE = {metrics['mape']:.3f} %")

# %% [markdown]
# ## Сравнение с baseline'ами (из этапа 2)
#
# Baseline'ы пересчитываем здесь же на тех же тестовых строках, чтобы
# гарантировать одинаковый знаменатель.

# %%
# Naive: предсказание = текущее top_1rm
naive_pred = test["top_1rm"].values
# LinReg по последним 5 значениям
def _linreg_predict_series(values: np.ndarray, k: int = 5) -> np.ndarray:
    n = len(values)
    out = np.empty(n, dtype=float)
    for i in range(n):
        start = max(0, i - k + 1)
        ys = values[start:i + 1]
        if len(ys) >= 2:
            xs = np.arange(len(ys))
            m, b = np.polyfit(xs, ys, 1)
            out[i] = m * len(ys) + b
        else:
            out[i] = ys[-1]
    return out

# Считаем LinReg-предсказание на всех агрегированных данных, не только на data,
# чтобы экстраполяция строилась по полной истории.
linreg_chunks = []
for (a, e), g in agg.groupby(["athlete_id", "exercise_id"], sort=False):
    preds = _linreg_predict_series(g["top_1rm"].values, k=5)
    linreg_chunks.append(pd.Series(preds, index=g.index))
linreg_full = pd.concat(linreg_chunks).sort_index()
agg["linreg5"] = linreg_full
test_linreg = agg.loc[test.index, "linreg5"].values

results_df = pd.DataFrame([
    {"model": "Naive (last)",      "mae": mae(naive_pred, y_test.values),
     "rmse": rmse(naive_pred, y_test.values),
     "mape": mape(naive_pred, y_test.values)},
    {"model": "LinReg k=5",        "mae": mae(test_linreg, y_test.values),
     "rmse": rmse(test_linreg, y_test.values),
     "mape": mape(test_linreg, y_test.values)},
    {"model": "LightGBM",          **metrics},
])
print("\nComparison on test set:")
print(results_df.to_string(index=False))

# %% [markdown]
# ## Разрез ошибок LightGBM по группам стажа

# %%
test_with_pred = test.copy()
test_with_pred["pred"] = pred_test
test_with_pred["bucket"] = pd.cut(
    test_with_pred["training_age_months"],
    bins=[-0.5, 6, 12, 36, 240],
    labels=["<6mo", "6-12mo", "12-36mo", ">36mo"],
)

bucket_stats = (
    test_with_pred.assign(
        abs_err=lambda d: (d.pred - d.target_1rm).abs(),
        ape=lambda d: 100 * (d.pred - d.target_1rm).abs() / d.target_1rm,
    )
    .groupby("bucket", observed=True)
    .agg(n=("abs_err", "size"),
         mae=("abs_err", "mean"),
         mean_target_1rm=("target_1rm", "mean"),
         mape_pct=("ape", "mean"))
)
print(bucket_stats.to_string())

# %% [markdown]
# ## Feature importance
#
# Сколько раз каждая фича использовалась как split в дереве (по умолчанию
# `importance_type='split'`). Альтернатива — `'gain'` (суммарный выигрыш
# по MAE при использовании этой фичи).

# %%
imp_split = pd.Series(model.feature_importance(importance_type="split"),
                      index=FEATURE_COLS, name="splits").sort_values(ascending=False)
imp_gain = pd.Series(model.feature_importance(importance_type="gain"),
                     index=FEATURE_COLS, name="gain").sort_values(ascending=False)
print("\nTop-15 by gain:")
print(imp_gain.head(15).to_string())
print("\nTop-15 by splits:")
print(imp_split.head(15).to_string())

# %% [markdown]
# ## Сохранение модели
#
# Сохраняем модель и список фичей в `ml/data/models/`. На этапе 4 этот
# файл будет конвертирован в ONNX для использования внутри MAUI-приложения.

# %%
MODEL_PATH = MODELS_DIR / "lightgbm_1rm_28d.txt"
META_PATH = MODELS_DIR / "lightgbm_1rm_28d.meta.json"

model.save_model(str(MODEL_PATH), num_iteration=model.best_iteration)

import json
meta = {
    "horizon_days": HORIZON_DAYS,
    "feature_columns": FEATURE_COLS,
    "categorical_columns": CATEGORICAL_COLS,
    "target_column": TARGET_COL,
    "muscle_classes": muscle_classes,
    "best_iteration": int(model.best_iteration or 0),
    "test_metrics": metrics,
    "training_examples": int(len(train)),
    "test_examples": int(len(test)),
}
META_PATH.write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"\nModel saved: {MODEL_PATH}")
print(f"Meta saved:  {META_PATH}")

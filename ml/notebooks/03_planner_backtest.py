# ---
# title: "Этап 5 — бэктест модуля планирования"
# description: |
#   Проверка корректности рекомендаций WorkoutPlannerService на синтетических
#   данных. Для каждой пары (атлет × упражнение) проходим тренировки
#   по порядку: на каждой тренировке симулируем работу planner'а —
#   считаем фичи, прогоняем через обученную LightGBM-модель, применяем
#   anchor-формулу из C# (last_top_weight * growth_ratio с защитами).
#   Сравниваем рекомендацию с тем, что атлет реально сделал на следующей
#   тренировке.
#
#   Метрики:
#   - signed_delta = recommended_weight − actual_next_weight (хотим ~0)
#   - within_5_pct — доля рекомендаций в пределах ±5% от факта
#   - regression_rate — доля случаев, где рекомендация МЕНЬШЕ прошлого
#     рабочего веса (главная жалоба пользователя — должно быть 0%)
#   - growth_ratio — распределение приращений (хотим почти всегда ≥ 1.0,
#     медиана ~1.005–1.01)
# ---

# %%
from __future__ import annotations
from pathlib import Path
import numpy as np
import pandas as pd
import lightgbm as lgb

pd.set_option("display.float_format", "{:.4f}".format)
pd.set_option("display.max_columns", 50)

ROOT = Path(__file__).resolve().parents[1]
DATA_PATH = ROOT / "data" / "synth.csv"
MODEL_PATH = ROOT / "data" / "models" / "lightgbm_1rm_28d.txt"
META_PATH = ROOT / "data" / "models" / "lightgbm_1rm_28d.meta.json"

# Те же константы, что в Services/WorkoutPlannerService.cs.
SHORT_HORIZON_DAYS = 7
MAX_WEEKLY_GROWTH = 0.05
WEIGHT_STEP_BY_EQUIPMENT = {
    0: 2.5,   # Barbell
    1: 2.5,   # Dumbbell
    2: 5.0,   # Machine
    3: 2.5,   # Bodyweight
    4: 2.5,   # Cable
    5: 4.0,   # Kettlebell
    6: 2.5,   # Band
    7: 2.5,   # Other
}

# %% [markdown]
# ## 1. Подготовка данных — точно как в 02_lightgbm.py

# %%
import json
meta = json.loads(META_PATH.read_text(encoding="utf-8"))
FEATURE_COLS = list(meta["feature_columns"])
MUSCLE_CLASSES = list(meta["muscle_classes"])
MUSCLE_TO_CODE = {m: i for i, m in enumerate(MUSCLE_CLASSES)}

print("Loading model...")
model = lgb.Booster(model_file=str(MODEL_PATH))
print(f"  features expected: {len(FEATURE_COLS)}")

print("Loading data...")
df = pd.read_csv(DATA_PATH)
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
agg["sex_male"] = (agg["sex"] == "M").astype(int)
agg["primary_muscle_code"] = agg["primary_muscle"].map(MUSCLE_TO_CODE).fillna(-1).astype(int)
print(f"  aggregated rows: {len(agg):,}")

# %% [markdown]
# ## 2. Расчёт фичей для одной строки (повторяет C# BuildFeatures)

# %%
def _linreg_slope(y: np.ndarray) -> float:
    n = len(y)
    if n < 2:
        return np.nan
    x = np.arange(n, dtype=float)
    denom = ((x - x.mean()) ** 2).sum()
    if denom == 0:
        return np.nan
    return float(((x - x.mean()) * (y - y.mean())).sum() / denom)


def build_features_row(history: pd.DataFrame, current: pd.Series) -> np.ndarray:
    """history — все ПРЕДЫДУЩИЕ тренировки этой серии, current — текущая.
    Возвращает np.array длиной 28 в порядке FEATURE_COLS."""
    prior_1rm = history["top_1rm"].values
    prior_rpe = history["avg_rpe"].values
    n_prior = len(prior_1rm)

    def lag(k):
        return prior_1rm[-k] if n_prior >= k else np.nan

    lag1, lag2, lag3, lag5 = lag(1), lag(2), lag(3), lag(5)
    diff_1 = lag1 - lag2 if not np.isnan(lag1) and not np.isnan(lag2) else np.nan

    def mean_window(arr, w):
        tail = arr[-w:] if len(arr) >= 1 else arr
        if len(tail) >= 2:
            return float(tail.mean())
        if len(tail) == 1:
            return float(tail[0])
        return np.nan

    mean_1rm_5 = mean_window(prior_1rm, 5)
    mean_rpe_5 = mean_window(prior_rpe, 5)
    slope_3 = _linreg_slope(prior_1rm[-3:]) if n_prior >= 2 else np.nan
    slope_5 = _linreg_slope(prior_1rm[-5:]) if n_prior >= 2 else np.nan

    days_since_first = (current["date_dt"] - history["date_dt"].iloc[0]).days if n_prior >= 1 else 0.0
    days_since_last = (current["date_dt"] - history["date_dt"].iloc[-1]).days if n_prior >= 1 else np.nan

    vals = {
        "lag_1rm_1": lag1, "lag_1rm_2": lag2, "lag_1rm_3": lag3, "lag_1rm_5": lag5,
        "diff_1": diff_1,
        "mean_1rm_5": mean_1rm_5, "mean_rpe_5": mean_rpe_5,
        "slope_3": slope_3, "slope_5": slope_5,
        "n_history": float(n_prior),
        "days_since_first": float(days_since_first),
        "days_since_last": days_since_last,
        "top_1rm": current["top_1rm"],
        "top_weight": current["top_weight"],
        "n_sets": current["n_sets"],
        "avg_rpe": current["avg_rpe"],
        "min_rpe": current["min_rpe"],
        "max_rpe": current["max_rpe"],
        "avg_reps": current["avg_reps"],
        "sex_male": current["sex_male"],
        "bodyweight": current["bodyweight"],
        "age": current["age"],
        "training_age_months": current["training_age_months"],
        "target_rpe_profile": current["target_rpe_profile"],
        "exercise_id": current["exercise_id"],
        "is_compound": current["is_compound"],
        "equipment": current["equipment"],
        "primary_muscle_code": current["primary_muscle_code"],
    }
    return np.array([vals[c] for c in FEATURE_COLS], dtype=float)


# %% [markdown]
# ## 3. Логика planner'а — копия C# WorkoutPlannerService

# %%
def pick_reps_and_sets(is_compound: bool, last_avg_reps: float, last_n_sets: int):
    default_reps = 6 if is_compound else 10
    default_sets = 4 if is_compound else 3
    lr = int(round(last_avg_reps))
    if is_compound and 3 <= lr <= 10:
        default_reps = lr
    elif (not is_compound) and 6 <= lr <= 15:
        default_reps = lr
    if 3 <= last_n_sets <= 5:
        default_sets = last_n_sets
    return default_reps, default_sets


def recommend(current_1rm, predicted_1rm, last_top_weight, last_reps,
              equipment, is_compound, last_n_sets, horizon_days=28):
    # Интерполируем прогноз на 7 дней.
    if predicted_1rm > 0 and horizon_days > 0:
        days_ahead = min(SHORT_HORIZON_DAYS, horizon_days)
        delta = predicted_1rm - current_1rm
        target_1rm = current_1rm + delta * (days_ahead / horizon_days)
    else:
        target_1rm = current_1rm

    # Защита 1: не ниже текущего.
    if target_1rm < current_1rm:
        target_1rm = current_1rm
    # Защита 2: за неделю не больше +5%.
    max_target = current_1rm * (1.0 + MAX_WEEKLY_GROWTH)
    if target_1rm > max_target:
        target_1rm = max_target

    target_reps, target_sets = pick_reps_and_sets(bool(is_compound), last_reps, last_n_sets)

    growth = target_1rm / current_1rm if current_1rm > 0 else 1.0
    last_reps_eff = max(1.0, last_reps)
    raw_weight = last_top_weight * growth * (1.0 + last_reps_eff / 30.0) / (1.0 + target_reps / 30.0)

    step = WEIGHT_STEP_BY_EQUIPMENT.get(int(equipment), 2.5)
    weight = round(raw_weight / step) * step
    if weight < step:
        weight = step

    # Защита 3: при тех же повторениях не ниже прошлого рабочего веса.
    if abs(target_reps - last_reps_eff) < 1.0 and weight < last_top_weight:
        weight = last_top_weight

    return weight, target_reps, target_sets, growth


# %% [markdown]
# ## 4. Бэктест: проходим серии тренировок и собираем рекомендации
#
# На каждой тренировке (начиная со второй, чтобы был хоть один лаг)
# симулируем работу planner'а и сравниваем с фактом следующей
# тренировки той же пары (athlete, exercise).

# %%
# Чтобы не считать бесконечно — берём подвыборку 50 атлетов (50 * 11 упр
# = ~3000 серий, ~150 тыс. рекомендаций — этого с запасом).
N_ATHLETES_SAMPLE = 50
rng = np.random.default_rng(123)
athlete_ids = agg["athlete_id"].unique()
sample_ids = rng.choice(athlete_ids, size=min(N_ATHLETES_SAMPLE, len(athlete_ids)), replace=False)
sub = agg[agg["athlete_id"].isin(sample_ids)].copy()
print(f"Backtest on {sub['athlete_id'].nunique()} athletes, "
      f"{sub.groupby(['athlete_id','exercise_id']).ngroups} series")

records = []
for (aid, eid), g in sub.groupby(["athlete_id", "exercise_id"], sort=False):
    g = g.reset_index(drop=True)
    if len(g) < 3:  # нужна хотя бы одна история, текущая и следующая
        continue
    for i in range(1, len(g) - 1):
        history = g.iloc[:i]
        current = g.iloc[i]
        nxt = g.iloc[i + 1]

        x = build_features_row(history, current)
        pred = float(model.predict(x.reshape(1, -1))[0])

        weight, target_reps, target_sets, growth = recommend(
            current_1rm=current["top_1rm"],
            predicted_1rm=pred,
            last_top_weight=current["top_weight"],
            last_reps=current["avg_reps"],
            equipment=current["equipment"],
            is_compound=current["is_compound"],
            last_n_sets=current["n_sets"],
            horizon_days=28,
        )

        records.append({
            "athlete_id": aid, "exercise_id": eid,
            "current_top_weight": current["top_weight"],
            "current_top_1rm": current["top_1rm"],
            "predicted_1rm": pred,
            "recommended_weight": weight,
            "recommended_reps": target_reps,
            "actual_next_top_weight": nxt["top_weight"],
            "actual_next_avg_reps": nxt["avg_reps"],
            "days_to_next": (nxt["date_dt"] - current["date_dt"]).days,
            "growth_ratio": growth,
            "training_age_months": current["training_age_months"],
            "is_compound": current["is_compound"],
        })

res = pd.DataFrame.from_records(records)
print(f"\nTotal recommendations: {len(res):,}")

# %% [markdown]
# ## 5. Метрики

# %%
def _pct(x, denom=None):
    denom = denom if denom is not None else len(res)
    return f"{x:,} ({100*x/denom:.1f}%)" if denom else "0"

signed = res["recommended_weight"] - res["actual_next_top_weight"]
abs_err = signed.abs()
rel_err = abs_err / res["actual_next_top_weight"]
within5 = (rel_err <= 0.05).sum()
within10 = (rel_err <= 0.10).sum()
regress = (res["recommended_weight"] < res["current_top_weight"]).sum()
same_or_up = (res["recommended_weight"] >= res["current_top_weight"]).sum()

print("=" * 60)
print("PLANNER BACKTEST METRICS")
print("=" * 60)
print(f"  n recommendations           : {len(res):,}")
print(f"  median signed delta (rec-act): {signed.median():+.2f} kg")
print(f"  mean signed delta            : {signed.mean():+.2f} kg")
print(f"  median |delta|               : {abs_err.median():.2f} kg")
print(f"  mean |delta|                 : {abs_err.mean():.2f} kg")
print(f"  within +/-5% of actual       : {_pct(within5)}")
print(f"  within +/-10% of actual      : {_pct(within10)}")
print()
print(f"  recommended < last (regress) : {_pct(regress)}   <- want 0%")
print(f"  recommended >= last          : {_pct(same_or_up)}")
print()
print(f"  growth_ratio median          : {res['growth_ratio'].median():.4f}")
print(f"  growth_ratio mean            : {res['growth_ratio'].mean():.4f}")
print(f"  growth_ratio p95             : {res['growth_ratio'].quantile(0.95):.4f}")
print(f"  growth_ratio max             : {res['growth_ratio'].max():.4f}")

# %% [markdown]
# ## 6. Разрез по стажу

# %%
def _bucket(m):
    if m < 6: return "<6mo (новичок)"
    if m < 12: return "6-12mo"
    if m < 36: return "12-36mo"
    return ">36mo (опытный)"

res["bucket"] = res["training_age_months"].map(_bucket)
by_exp = res.groupby("bucket", sort=False).agg(
    n=("recommended_weight", "count"),
    median_delta=("recommended_weight", lambda s: (s - res.loc[s.index, "actual_next_top_weight"]).median()),
    mean_abs=("recommended_weight", lambda s: (s - res.loc[s.index, "actual_next_top_weight"]).abs().mean()),
    regress_pct=("recommended_weight", lambda s: 100 * (s < res.loc[s.index, "current_top_weight"]).mean()),
    median_growth=("growth_ratio", "median"),
)
print("\nПо группам стажа:")
print(by_exp.to_string())

# %% [markdown]
# ## 7. Симуляция «цепочки»: что будет, если атлет всегда применяет план
#
# Главный риск, который мы фиксили: рекомендация → атлет применяет →
# на следующей тренировке planner снова уменьшает → каскад вниз.
# Имитируем последовательное применение плана 8 тренировок подряд
# и смотрим, падает ли вес.

# %%
def chain_simulation(all_rows, start_row=3, n_steps=8):
    """all_rows — все тренировки этой серии (DataFrame). start_row —
    индекс точки старта. Возвращает список рекомендаций по шагам.
    На каждом шаге фактический вес заменяется рекомендацией (worst case)."""
    history = all_rows.iloc[:start_row].copy().to_dict("records")
    chain = []
    for step in range(n_steps):
        if not history:
            break
        # Текущая «тренировка» — последний элемент истории.
        current = history[-1]
        hist_df = pd.DataFrame(history[:-1]) if len(history) > 1 else pd.DataFrame(columns=all_rows.columns)
        hist_df["date_dt"] = pd.to_datetime(hist_df["date_dt"]) if "date_dt" in hist_df and len(hist_df) else hist_df.get("date_dt")
        cur_series = pd.Series(current)
        if len(hist_df) >= 1:
            x = build_features_row(hist_df, cur_series)
            pred = float(model.predict(x.reshape(1, -1))[0])
        else:
            pred = current["top_1rm"]
        weight, target_reps, _, growth = recommend(
            current_1rm=current["top_1rm"],
            predicted_1rm=pred,
            last_top_weight=current["top_weight"],
            last_reps=current["avg_reps"],
            equipment=current["equipment"],
            is_compound=current["is_compound"],
            last_n_sets=current["n_sets"],
        )
        chain.append((step, weight, current["top_weight"], growth))
        # Симулируем «применил план» — следующая тренировка: вес из плана,
        # повторы из плана, RPE считаем средним предыдущего (упрощение).
        new = dict(current)
        new["top_weight"] = weight
        new["avg_reps"] = target_reps
        new["top_1rm"] = weight * (1 + target_reps / 30.0)
        new["date_dt"] = (pd.Timestamp(current["date_dt"]) + pd.Timedelta(days=7))
        history.append(new)
    return chain


# Пробуем на 30 случайных сериях ≥ 5 тренировок.
chains_data = []
candidate_series = [(aid, eid, g) for (aid, eid), g in sub.groupby(["athlete_id", "exercise_id"]) if len(g) >= 5]
rng2 = np.random.default_rng(7)
picks = rng2.choice(len(candidate_series), size=min(30, len(candidate_series)), replace=False)
for idx in picks:
    aid, eid, g = candidate_series[idx]
    g = g.reset_index(drop=True)
    chain = chain_simulation(g, start_row=3, n_steps=8)
    if not chain:
        continue
    start_w = chain[0][2]
    end_w = chain[-1][1]
    chains_data.append({"athlete_id": aid, "exercise_id": eid,
                        "start_weight": start_w, "end_weight": end_w,
                        "diff_kg": end_w - start_w,
                        "diff_pct": 100 * (end_w - start_w) / start_w if start_w else 0})

chain_df = pd.DataFrame(chains_data)
print("\nCHAIN-SIMULATION (8 шагов «применил план»):")
print(f"  series tested: {len(chain_df)}")
print(f"  median diff after 8 sessions : {chain_df['diff_kg'].median():+.2f} kg "
      f"({chain_df['diff_pct'].median():+.2f}%)")
print(f"  mean diff                    : {chain_df['diff_kg'].mean():+.2f} kg")
print(f"  n series with end < start    : {(chain_df['diff_kg'] < 0).sum()}/{len(chain_df)}")
print(f"  n series with end > start    : {(chain_df['diff_kg'] > 0).sum()}/{len(chain_df)}")
print(f"  worst regression             : {chain_df['diff_kg'].min():+.2f} kg")
print(f"  best growth                  : {chain_df['diff_kg'].max():+.2f} kg")

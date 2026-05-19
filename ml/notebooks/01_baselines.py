# ---
# title: "Этап 2 — Базовые модели прогноза 1ПМ"
# description: |
#   Загружаем синтетические тренировки, строим целевую переменную
#   (1ПМ следующей тренировки по упражнению) и оцениваем три baseline-модели.
#   Получаем точку отсчёта, ниже которой "умная" модель не должна работать.
#
#   Файл открывается в VS Code как интерактивный Python-notebook благодаря
#   маркерам `# %%` (расширение Python → "Run Cell"). По желанию можно
#   конвертировать в .ipynb через `jupytext --to ipynb 01_baselines.py`.
# ---

# %% [markdown]
# # Этап 2 — Baseline-модели прогноза 1ПМ
#
# **Постановка задачи.** Для упражнения, которое атлет уже делал, предсказать
# его 1ПМ (one-rep max, максимальный вес на одно повторение) через **4 недели**
# (28 дней). Метрика — MAE и MAPE в килограммах.
#
# Шаг "следующая тренировка" (1 шаг вперёд) тестировался отдельно и оказался
# слишком простым: рост 1ПМ за одну сессию около 1%, и наивный прогноз даёт
# MAPE ~2%. Горизонт в месяц делает задачу интереснее и практичнее —
# на месяц вперёд имеет смысл планировать программу.
#
# **Источник данных** — синтетический CSV (`ml/data/synth.csv`), 500 виртуальных
# атлетов × 26 недель, ~760k подходов.
#
# **План:**
# 1. Загрузка и краткий EDA
# 2. Агрегация подходов в "результат упражнения за тренировку" (`top_1rm`)
# 3. Построение пар (текущее → следующее) для обучения и оценки
# 4. Сплит train/test **по атлетам** (тестируем на тех, кого модель не видела)
# 5. Три baseline-модели:
#    1. Наивный — следующий = текущий
#    2. Скользящее среднее по последним 3 тренировкам
#    3. Линейная регрессия по последним 5 точкам

# %%
import numpy as np
import pandas as pd
from pathlib import Path

pd.set_option("display.float_format", "{:.2f}".format)
pd.set_option("display.max_columns", 30)

DATA_PATH = Path(__file__).resolve().parents[1] / "data" / "synth.csv"
print("Reading", DATA_PATH)
df = pd.read_csv(DATA_PATH)
print(f"Rows: {len(df):,}  Athletes: {df['athlete_id'].nunique()}  "
      f"Exercises: {df['exercise_id'].nunique()}")
df.head()

# %% [markdown]
# ## EDA: распределения

# %%
df[["weight_kg", "reps", "rpe", "true_1rm_kg",
    "bodyweight_kg", "age", "training_age_months"]].describe()

# %%
# Сколько тренировок (уникальных дат) у каждого атлета
workouts_per_athlete = df.groupby("athlete_id")["date"].nunique()
print(f"Workouts per athlete: median={workouts_per_athlete.median()}, "
      f"mean={workouts_per_athlete.mean():.1f}, "
      f"min={workouts_per_athlete.min()}, max={workouts_per_athlete.max()}")

# %% [markdown]
# ## Агрегация: один результат на (athlete, exercise, date)
#
# Берём только **рабочие** подходы. Для каждой пары (атлет, упражнение, дата)
# вычисляем максимальный 1ПМ по формуле Эпли:
# $$1RM = вес \cdot (1 + reps / 30)$$
# Это будет нашей таргетной величиной для всех моделей.

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
            true_1rm=("true_1rm_kg", "first"),
            sex=("sex", "first"),
            bodyweight=("bodyweight_kg", "first"),
            age=("age", "first"),
            training_age_months=("training_age_months", "first"),
            is_compound=("is_compound", "first"),
            equipment=("equipment", "first"),
            primary_muscle=("primary_muscle", "first"),
        )
        .sort_values(["athlete_id", "exercise_id", "date"])
        .reset_index(drop=True)
)
print(f"Aggregated rows: {len(agg):,}")
agg.head()

# %% [markdown]
# ## Целевая переменная: 1ПМ через 4 недели
#
# Шаг "следующая тренировка" слишком короткий — рост 1ПМ за одну сессию ~1%,
# и наивный прогноз "следующий = текущий" получается избыточно точным.
# Для практической ценности (и для интересной диплому задачи) берём
# горизонт **28 дней**: для каждой строки ищем ближайшую тренировку
# того же упражнения у того же атлета, прошедшую **через >= 28 дней**.
# Если такой нет (атлет закончил серию или сменил программу) — пара
# выбрасывается.

# %%
HORIZON_DAYS = 28

agg["date_dt"] = pd.to_datetime(agg["date"])
agg = agg.sort_values(["athlete_id", "exercise_id", "date_dt"]).reset_index(drop=True)

def _targets_for_group(g: pd.DataFrame) -> pd.DataFrame:
    """Внутри одной серии (athlete, exercise): для каждой строки ищем ближайшую
    последующую запись через >= HORIZON_DAYS дней."""
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
pairs = agg.dropna(subset=["target_1rm"]).copy()
print(f"Aggregated rows: {len(agg):,}")
print(f"Pairs with target at >= {HORIZON_DAYS}d: {len(pairs):,} "
      f"(dropped {len(agg) - len(pairs):,} tail rows)")
print(f"Median gap to target: {pairs['target_gap_days'].median():.0f} days  "
      f"(p90={pairs['target_gap_days'].quantile(0.9):.0f} days)")
pairs[["athlete_id", "exercise_id", "date",
       "top_1rm", "target_1rm", "target_gap_days"]].head(10)

# %% [markdown]
# ## Train / Test split — по атлетам
#
# Важно: тестируем на атлетах, которых модель **не видела вообще**. Это
# проверяет обобщающую способность модели, а не запоминание конкретных
# траекторий прогресса.

# %%
np.random.seed(42)
all_athletes = pairs["athlete_id"].unique()
np.random.shuffle(all_athletes)
test_size = int(len(all_athletes) * 0.2)
test_athletes = set(all_athletes[:test_size])

train = pairs[~pairs["athlete_id"].isin(test_athletes)].copy()
test = pairs[pairs["athlete_id"].isin(test_athletes)].copy()
print(f"Train: {len(train):,} pairs, {train['athlete_id'].nunique()} athletes")
print(f"Test:  {len(test):,} pairs, {test['athlete_id'].nunique()} athletes")

# %% [markdown]
# ## Метрики

# %%
def mae(pred, true):
    return float(np.mean(np.abs(np.asarray(pred) - np.asarray(true))))

def rmse(pred, true):
    return float(np.sqrt(np.mean((np.asarray(pred) - np.asarray(true)) ** 2)))

def mape(pred, true):
    return float(np.mean(np.abs((np.asarray(pred) - np.asarray(true))
                                / np.asarray(true))) * 100)

# %% [markdown]
# ## Baseline 1: наивный — «следующий = текущий»
#
# Самая простая отправная точка. Если модель не побеждает её, что-то не так.

# %%
naive_pred = test["top_1rm"].values
naive_true = test["target_1rm"].values
b1 = {
    "mae": mae(naive_pred, naive_true),
    "rmse": rmse(naive_pred, naive_true),
    "mape": mape(naive_pred, naive_true),
}
print(f"Naive - MAE: {b1['mae']:.2f} kg | RMSE: {b1['rmse']:.2f} kg | MAPE: {b1['mape']:.2f}%")

# %% [markdown]
# ## Baseline 2: скользящее среднее по последним 3 тренировкам

# %%
def rolling_pred(df_sorted, window):
    """Для каждой строки — среднее `top_1rm` по последним `window` тренировкам
    включая текущую."""
    return (
        df_sorted.groupby(["athlete_id", "exercise_id"])["top_1rm"]
                 .rolling(window=window, min_periods=1)
                 .mean()
                 .reset_index(level=[0, 1], drop=True)
    )

# Считаем по всему датасету, потом фильтруем тестовых атлетов
pairs_sorted = pairs.sort_values(["athlete_id", "exercise_id", "date"]).copy()
pairs_sorted["roll3"] = rolling_pred(pairs_sorted, window=3)

mask_test = pairs_sorted["athlete_id"].isin(test_athletes)
roll_pred = pairs_sorted.loc[mask_test, "roll3"].values
roll_true = pairs_sorted.loc[mask_test, "target_1rm"].values
b2 = {
    "mae": mae(roll_pred, roll_true),
    "rmse": rmse(roll_pred, roll_true),
    "mape": mape(roll_pred, roll_true),
}
print(f"Rolling-3 - MAE: {b2['mae']:.2f} kg | RMSE: {b2['rmse']:.2f} kg | MAPE: {b2['mape']:.2f}%")

# %% [markdown]
# ## Baseline 3: линейная регрессия по последним 5 точкам
#
# Для каждой строки берём последние 5 `top_1rm` (или сколько есть до неё),
# подгоняем прямую методом наименьших квадратов и предсказываем значение
# на следующем шаге. Это **именно тот алгоритм, что сейчас работает в FitApp**.

# %%
def linreg_predict(values: np.ndarray, k: int = 5) -> np.ndarray:
    """Предсказание линейной регрессии на скользящем окне k.

    Для i-й точки фитим прямую y(x) = m*x + b на последних `k` точках
    (или меньше, если в начале) и возвращаем y(i+1) — т.е. следующее значение.
    Если точек < 2 — возвращаем последнее наблюдаемое значение."""
    n = len(values)
    pred = np.empty(n, dtype=float)
    for i in range(n):
        start = max(0, i - k + 1)
        ys = values[start: i + 1]
        if len(ys) >= 2:
            xs = np.arange(len(ys))
            m, b = np.polyfit(xs, ys, 1)
            pred[i] = m * len(ys) + b   # на одну точку вперёд
        else:
            pred[i] = ys[-1]
    return pred

linreg_results = []
for (_, _), group in pairs_sorted.groupby(["athlete_id", "exercise_id"], sort=False):
    linreg_results.append(
        pd.Series(linreg_predict(group["top_1rm"].values, k=5),
                  index=group.index)
    )
pairs_sorted["linreg5"] = pd.concat(linreg_results).sort_index()

mask_test = pairs_sorted["athlete_id"].isin(test_athletes)
lr_pred = pairs_sorted.loc[mask_test, "linreg5"].values
lr_true = pairs_sorted.loc[mask_test, "target_1rm"].values
b3 = {
    "mae": mae(lr_pred, lr_true),
    "rmse": rmse(lr_pred, lr_true),
    "mape": mape(lr_pred, lr_true),
}
print(f"LinReg-5 - MAE: {b3['mae']:.2f} kg | RMSE: {b3['rmse']:.2f} kg | MAPE: {b3['mape']:.2f}%")

# %% [markdown]
# ## Итоговая таблица baseline'ов

# %%
summary = pd.DataFrame([
    {"baseline": "Naive (last)",      **b1},
    {"baseline": "Rolling mean k=3",  **b2},
    {"baseline": "LinReg k=5",        **b3},
])
print(summary.to_string(index=False))

# %% [markdown]
# ## Разрез ошибки по группам стажа
#
# Для диплома полезно показать, **где** модель ошибается. Опытные атлеты
# растут медленно — наивный прогноз там работает лучше. Новички растут быстро,
# и наивный baseline сильно отстаёт. Это тот сигнал, который должна "съесть"
# умная модель (LightGBM, этап 3).

# %%
test_with_pred = test.copy()
test_with_pred["pred_naive"] = naive_pred

test_with_pred["bucket"] = pd.cut(
    test_with_pred["training_age_months"],
    bins=[-0.5, 6, 12, 36, 240],
    labels=["<6mo", "6-12mo", "12-36mo", ">36mo"],
)

bucket_stats = (
    test_with_pred.assign(abs_err=lambda d: (d.pred_naive - d.target_1rm).abs())
                  .groupby("bucket", observed=True)
                  .agg(n=("abs_err", "size"),
                       mae=("abs_err", "mean"),
                       mean_target_1rm=("target_1rm", "mean"))
)
bucket_stats["mape_%"] = (
    test_with_pred.assign(ape=lambda d: 100 * (d.pred_naive - d.target_1rm).abs() / d.target_1rm)
                  .groupby("bucket", observed=True)["ape"].mean()
)
print(bucket_stats.to_string())

# %% [markdown]
# ## Выводы
#
# - Базовая ошибка наивного прогноза задаёт планку: LightGBM на этапе 3
#   должен явно её обыгрывать (особенно на новичках, где есть тренд).
# - Скользящее среднее обычно **хуже** наивного — оно занижает прогноз, потому
#   что среднее по прошлому всегда отстаёт от растущей траектории.
# - Линейная регрессия по 5 точкам должна выигрывать у наивного на новичках
#   и быть близкой к нему у опытных. Если выигрывает по всему датасету —
#   значит у нас есть стабильный сигнал тренда, и LightGBM сможет ещё лучше.

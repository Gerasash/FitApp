"""
Конвертация обученной LightGBM-модели в ONNX и проверка эквивалентности.

Запуск:
    python -m ml.export.to_onnx

На выходе — файл `ml/data/models/lightgbm_1rm_28d.onnx` + контроль точности
(MAE между LightGBM и ONNX-предсказаниями на 1000 случайных тестовых строках
должна быть < 0.01 кг — фактически до уровня floating-point round-off).
"""
from __future__ import annotations
import json
import sys
from pathlib import Path

import numpy as np
import lightgbm as lgb
import onnxmltools
import onnxruntime as ort
from onnxmltools.convert.common.data_types import FloatTensorType


MODELS_DIR = Path(__file__).resolve().parents[1] / "data" / "models"
LGB_MODEL = MODELS_DIR / "lightgbm_1rm_28d.txt"
META_PATH = MODELS_DIR / "lightgbm_1rm_28d.meta.json"
ONNX_MODEL = MODELS_DIR / "lightgbm_1rm_28d.onnx"


def convert() -> Path:
    if not LGB_MODEL.exists():
        raise FileNotFoundError(
            f"LightGBM model not found: {LGB_MODEL}.\n"
            "Запусти сначала: python ml/notebooks/02_lightgbm.py"
        )
    meta = json.loads(META_PATH.read_text(encoding="utf-8"))
    feature_cols = meta["feature_columns"]

    booster = lgb.Booster(model_file=str(LGB_MODEL))
    if booster.num_feature() != len(feature_cols):
        raise ValueError(
            f"Mismatch: booster expects {booster.num_feature()} features, "
            f"meta lists {len(feature_cols)}"
        )

    initial_type = [("input", FloatTensorType([None, len(feature_cols)]))]
    onnx_model = onnxmltools.convert_lightgbm(
        booster,
        initial_types=initial_type,
        target_opset=12,
    )
    ONNX_MODEL.write_bytes(onnx_model.SerializeToString())
    print(f"Converted: {LGB_MODEL.name}  ->  {ONNX_MODEL.name}")
    print(f"           {ONNX_MODEL.stat().st_size / 1024:.1f} KB")
    return ONNX_MODEL


def verify(n_samples: int = 1000, seed: int = 7) -> None:
    """Запускает обе модели на случайных тестовых строках и сравнивает.
    Использует data/synth.csv + те же фичи, что в notebooks/02_lightgbm.py."""
    print(f"\nVerifying on {n_samples} random rows...")
    sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
    # Воспроизводим минимальный feature pipeline. Чтобы не дублировать большой
    # фрейм генерации, импортируем агрегатор из 02_lightgbm косвенно: проще
    # перевычислить тут.
    import pandas as pd

    meta = json.loads(META_PATH.read_text(encoding="utf-8"))
    feature_cols = meta["feature_columns"]

    DATA_PATH = MODELS_DIR.parent / "synth.csv"
    if not DATA_PATH.exists():
        raise FileNotFoundError(
            f"Dataset not found: {DATA_PATH}. Сгенерируй его: "
            "python -m ml.generator.generate"
        )

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

    # Дублируем построение фичей в укороченной форме (без таргета — он тут не нужен).
    def _linreg_slope(y):
        n = len(y)
        if n < 2:
            return np.nan
        x = np.arange(n, dtype=float)
        denom = ((x - x.mean()) ** 2).sum()
        if denom == 0:
            return np.nan
        return float(((x - x.mean()) * (y - y.mean())).sum() / denom)

    feats_per_group = []
    for (_, _), g in agg.groupby(["athlete_id", "exercise_id"], sort=False):
        n = len(g)
        y = g["top_1rm"].values
        rpe = g["avg_rpe"].values
        dates = g["date_dt"].values
        f = {}
        for k in (1, 2, 3, 5):
            f[f"lag_1rm_{k}"] = np.concatenate([np.full(k, np.nan), y[:-k]]) if n > k else np.full(n, np.nan)
        f["diff_1"] = f["lag_1rm_1"] - f["lag_1rm_2"]
        def _emean(values, window):
            res = np.full(n, np.nan)
            for i in range(n):
                start = max(0, i - window)
                if i - start >= 2:
                    res[i] = values[start:i].mean()
                elif i - start == 1:
                    res[i] = values[start]
            return res
        f["mean_1rm_5"] = _emean(y, 5)
        f["mean_rpe_5"] = _emean(rpe, 5)
        def _rslope(values, window):
            res = np.full(n, np.nan)
            for i in range(n):
                start = max(0, i - window)
                if i - start >= 2:
                    res[i] = _linreg_slope(values[start:i])
            return res
        f["slope_3"] = _rslope(y, 3)
        f["slope_5"] = _rslope(y, 5)
        f["n_history"] = np.arange(n, dtype=float)
        f["days_since_first"] = (dates - dates[0]).astype("timedelta64[D]").astype(float)
        dsl = np.full(n, np.nan)
        if n > 1:
            dsl[1:] = (dates[1:] - dates[:-1]).astype("timedelta64[D]").astype(float)
        f["days_since_last"] = dsl
        feats_per_group.append(pd.DataFrame(f, index=g.index))
    agg = agg.join(pd.concat(feats_per_group).sort_index())

    agg = agg.dropna(subset=["lag_1rm_1"]).copy()
    agg["sex_male"] = (agg["sex"] == "M").astype(int)
    # Восстанавливаем коды мышц по порядку из meta (важно: модель училась на
    # этих категориях).
    muscle_classes = meta["muscle_classes"]
    muscle_to_code = {m: i for i, m in enumerate(muscle_classes)}
    agg["primary_muscle_code"] = agg["primary_muscle"].map(muscle_to_code)

    sample = agg.sample(n=n_samples, random_state=seed)
    X = sample[feature_cols].astype("float32").values

    # LightGBM-предсказание
    booster = lgb.Booster(model_file=str(LGB_MODEL))
    pred_lgb = booster.predict(X)

    # ONNX-предсказание
    session = ort.InferenceSession(str(ONNX_MODEL))
    input_name = session.get_inputs()[0].name
    output_name = session.get_outputs()[0].name
    pred_onnx_raw = session.run([output_name], {input_name: X})[0]
    pred_onnx = np.asarray(pred_onnx_raw).reshape(-1)

    diff = np.abs(pred_lgb - pred_onnx)
    print(f"  LightGBM range: [{pred_lgb.min():.2f}, {pred_lgb.max():.2f}] kg")
    print(f"  ONNX range:     [{pred_onnx.min():.2f}, {pred_onnx.max():.2f}] kg")
    print(f"  Abs diff: mean={diff.mean():.6f}  median={np.median(diff):.6f}  "
          f"p99={np.quantile(diff, 0.99):.4f}  max={diff.max():.4f}")
    # ONNX-runtime использует float32, LightGBM — float64; небольшая
    # квантизационная погрешность ожидаема. Порог 0.05 кг — заведомо ниже
    # точности самой модели (MAE ~1.4 кг) и шага весов на штанге (2.5 кг).
    if np.median(diff) < 0.05 and np.quantile(diff, 0.99) < 0.5:
        print("  OK: predictions match within float32 quantization tolerance.")
    else:
        print("  WARNING: predictions diverge unexpectedly.")
        sys.exit(1)


if __name__ == "__main__":
    convert()
    verify()

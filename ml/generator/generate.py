"""
CLI для генератора синтетических тренировок.

Запуск из корня репозитория:
    python -m ml.generator.generate --athletes 200 --weeks 26 --seed 42 \\
        --out ml/data/synth.csv

На выходе — CSV, одна строка = один подход (рабочий или разминочный).
Колонки описаны в docstring VirtualAthlete._row.
"""
from __future__ import annotations
import argparse
import csv
import random
import sys
from datetime import date
from pathlib import Path

try:
    from tqdm import tqdm
except ImportError:  # tqdm — необязательная зависимость для приятного вывода
    def tqdm(iterable, **kwargs):  # type: ignore
        return iterable

from .athlete import simulate_athlete


CSV_COLUMNS = [
    "athlete_id", "sex", "bodyweight_kg", "age", "training_age_months",
    "split", "target_rpe_profile",
    "date", "exercise_id", "exercise_name", "exercise_name_en",
    "is_compound", "equipment", "primary_muscle",
    "exercise_order", "set_number", "is_warmup",
    "weight_kg", "reps", "rpe", "true_1rm_kg",
]


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Генератор синтетических тренировок для FitApp ML",
    )
    parser.add_argument("--athletes", type=int, default=200,
                        help="Сколько виртуальных атлетов сгенерировать")
    parser.add_argument("--weeks", type=int, default=26,
                        help="На сколько недель моделировать историю")
    parser.add_argument("--seed", type=int, default=42,
                        help="Зерно ГПСЧ (для воспроизводимости)")
    parser.add_argument("--out", type=Path,
                        default=Path("ml/data/synth.csv"),
                        help="Путь для CSV-файла")
    parser.add_argument("--end-date", type=date.fromisoformat, default=None,
                        help="Дата окончания истории (ISO YYYY-MM-DD). "
                             "По умолчанию — сегодня.")
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    args.out.parent.mkdir(parents=True, exist_ok=True)

    end_date = args.end_date or date.today()
    rng_master = random.Random(args.seed)

    n_rows = 0
    with args.out.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=CSV_COLUMNS)
        writer.writeheader()

        for athlete_id in tqdm(range(1, args.athletes + 1),
                               desc="Athletes", unit="ath"):
            # Каждому атлету — свой Random, посеянный из мастера.
            # Так результат воспроизводим, даже если параллелить позже.
            rng = random.Random(rng_master.random())
            rows = simulate_athlete(
                athlete_id=athlete_id,
                weeks=args.weeks,
                rng=rng,
                end_date=end_date,
            )
            writer.writerows(rows)
            n_rows += len(rows)

    # ASCII-only — иначе ломается на Windows-консолях с cp1251.
    print(
        f"Done: {args.athletes} athletes x {args.weeks} weeks -> "
        f"{n_rows:,} rows -> {args.out}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())

"""
Каталог упражнений для симулятора.

Числовые параметры откалиброваны по открытым нормативам силовых стандартов
(уровень "Intermediate" — мужчина 75 кг по strengthlevel.com / Symmetric
Strength). Для каждого упражнения задано:

  base_1rm_kg          — типичный 1ПМ для опытного мужчины 75 кг
  bw_exponent          — степень в законе масштабирования по массе тела:
                          1RM(bw) = base * (bw / 75) ** bw_exponent.
                          Для становой и приседа ~0.66, для жима ~0.55,
                          для изоляции ~0.4 (вес тела почти не помогает).
  female_factor        — множитель к base_1rm для женщин (типично 0.55–0.70
                          в зависимости от движения).
  ceiling_factor       — потолок 1ПМ (максимум, к которому может расти модель)
                          относительно начального 1ПМ опытного атлета.
                          Compound: ~2.0–2.5, изоляция: ~1.6–1.9.
  weekly_growth_max    — максимальный процент роста 1ПМ за неделю у новичка
                          (на плато → стремится к нулю по логистической кривой).
  rep_range            — типичный диапазон рабочих повторений на упражнение.
  is_compound          — базовое (multi-joint) или изоляция.
  equipment            — соответствует FitApp Exercise.EquipmentType:
                          0=Штанга, 1=Гантели, 2=Тренажёр, 3=Свой вес,
                          4=Блок, 5=Гиря, 6=Резина, 7=Прочее.
  primary_muscle       — основная мышечная группа (для расчёта усталости
                          мышечной группы между тренировками).
"""
from __future__ import annotations
from dataclasses import dataclass


@dataclass(frozen=True)
class ExerciseDef:
    id: int
    name: str            # Русское название (как в seed FitApp)
    name_en: str
    is_compound: bool
    equipment: int       # См. таблицу в шапке файла
    primary_muscle: str
    base_1rm_kg: float
    bw_exponent: float
    female_factor: float
    ceiling_factor: float
    weekly_growth_max: float  # доля от текущего 1RM
    rep_range: tuple[int, int]


EXERCISES: list[ExerciseDef] = [
    # ---- Грудь ----
    ExerciseDef(
        id=1, name="Жим лёжа со штангой", name_en="Barbell Bench Press",
        is_compound=True, equipment=0, primary_muscle="Грудь",
        base_1rm_kg=100.0, bw_exponent=0.55, female_factor=0.60,
        ceiling_factor=2.2, weekly_growth_max=0.012, rep_range=(4, 10),
    ),
    ExerciseDef(
        id=2, name="Жим гантелей лёжа", name_en="Dumbbell Bench Press",
        is_compound=True, equipment=1, primary_muscle="Грудь",
        base_1rm_kg=40.0, bw_exponent=0.50, female_factor=0.60,
        ceiling_factor=2.0, weekly_growth_max=0.010, rep_range=(6, 12),
    ),
    ExerciseDef(
        id=3, name="Жим лёжа в наклоне", name_en="Incline Bench Press",
        is_compound=True, equipment=0, primary_muscle="Грудь",
        base_1rm_kg=80.0, bw_exponent=0.55, female_factor=0.60,
        ceiling_factor=2.0, weekly_growth_max=0.011, rep_range=(5, 10),
    ),

    # ---- Спина ----
    ExerciseDef(
        id=4, name="Становая тяга", name_en="Deadlift",
        is_compound=True, equipment=0, primary_muscle="Спина",
        base_1rm_kg=160.0, bw_exponent=0.66, female_factor=0.70,
        ceiling_factor=2.0, weekly_growth_max=0.013, rep_range=(3, 8),
    ),
    ExerciseDef(
        id=5, name="Тяга штанги в наклоне", name_en="Barbell Row",
        is_compound=True, equipment=0, primary_muscle="Спина",
        base_1rm_kg=90.0, bw_exponent=0.60, female_factor=0.65,
        ceiling_factor=2.0, weekly_growth_max=0.011, rep_range=(5, 10),
    ),
    ExerciseDef(
        id=6, name="Подтягивания", name_en="Pull-up",
        is_compound=True, equipment=3, primary_muscle="Спина",
        base_1rm_kg=85.0, bw_exponent=0.90, female_factor=0.60,
        ceiling_factor=1.5, weekly_growth_max=0.009, rep_range=(5, 12),
    ),
    ExerciseDef(
        id=7, name="Тяга верхнего блока", name_en="Lat Pulldown",
        is_compound=True, equipment=4, primary_muscle="Спина",
        base_1rm_kg=80.0, bw_exponent=0.45, female_factor=0.65,
        ceiling_factor=1.9, weekly_growth_max=0.010, rep_range=(8, 12),
    ),

    # ---- Ноги ----
    ExerciseDef(
        id=8, name="Приседания со штангой", name_en="Barbell Squat",
        is_compound=True, equipment=0, primary_muscle="Ноги",
        base_1rm_kg=140.0, bw_exponent=0.66, female_factor=0.72,
        ceiling_factor=2.2, weekly_growth_max=0.013, rep_range=(4, 10),
    ),
    ExerciseDef(
        id=9, name="Жим ногами", name_en="Leg Press",
        is_compound=True, equipment=2, primary_muscle="Ноги",
        base_1rm_kg=220.0, bw_exponent=0.55, female_factor=0.75,
        ceiling_factor=2.5, weekly_growth_max=0.012, rep_range=(8, 15),
    ),
    ExerciseDef(
        id=10, name="Румынская тяга", name_en="Romanian Deadlift",
        is_compound=True, equipment=0, primary_muscle="Ноги",
        base_1rm_kg=120.0, bw_exponent=0.60, female_factor=0.70,
        ceiling_factor=2.0, weekly_growth_max=0.011, rep_range=(6, 12),
    ),

    # ---- Плечи ----
    ExerciseDef(
        id=11, name="Жим штанги стоя", name_en="Overhead Press",
        is_compound=True, equipment=0, primary_muscle="Плечи",
        base_1rm_kg=60.0, bw_exponent=0.50, female_factor=0.55,
        ceiling_factor=1.9, weekly_growth_max=0.010, rep_range=(4, 10),
    ),
    ExerciseDef(
        id=12, name="Махи гантелями в стороны", name_en="Lateral Raise",
        is_compound=False, equipment=1, primary_muscle="Плечи",
        base_1rm_kg=14.0, bw_exponent=0.30, female_factor=0.65,
        ceiling_factor=1.7, weekly_growth_max=0.007, rep_range=(10, 15),
    ),

    # ---- Руки ----
    ExerciseDef(
        id=13, name="Подъём штанги на бицепс", name_en="Barbell Curl",
        is_compound=False, equipment=0, primary_muscle="Бицепс",
        base_1rm_kg=45.0, bw_exponent=0.40, female_factor=0.55,
        ceiling_factor=1.8, weekly_growth_max=0.008, rep_range=(6, 12),
    ),
    ExerciseDef(
        id=14, name="Французский жим", name_en="Lying Triceps Extension",
        is_compound=False, equipment=0, primary_muscle="Трицепс",
        base_1rm_kg=45.0, bw_exponent=0.40, female_factor=0.60,
        ceiling_factor=1.8, weekly_growth_max=0.008, rep_range=(8, 12),
    ),
    ExerciseDef(
        id=15, name="Разгибания на трицепс на блоке", name_en="Triceps Pushdown",
        is_compound=False, equipment=4, primary_muscle="Трицепс",
        base_1rm_kg=40.0, bw_exponent=0.35, female_factor=0.65,
        ceiling_factor=1.7, weekly_growth_max=0.007, rep_range=(10, 15),
    ),
]


# Удобный индекс по id для быстрого доступа в коде симуляции.
EXERCISE_BY_ID: dict[int, ExerciseDef] = {ex.id: ex for ex in EXERCISES}

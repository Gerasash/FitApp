"""
Параметрический симулятор виртуального атлета.

Каждый VirtualAthlete:
  • имеет анкету (пол, вес, возраст, стаж);
  • имеет "характер": скорость прогресса, восстанавливаемость, дисциплину,
    уровень внутри-сессионного шума RPE;
  • держит состояние по каждому упражнению (текущий и потолочный 1ПМ);
  • держит накапливающуюся усталость по мышечным группам;
  • получает недельное расписание (split) и шагает по дням.

Модель прогресса — логистическая: прирост 1ПМ за тренировку зависит от того,
насколько далеко атлет от своего потолка. Чем ближе к потолку, тем медленнее
рост. Это даёт характерную кривую "быстрый старт у новичка → плато у
продвинутого".

Тренированный вес рабочего сета подбирается обратной формулой Эпли с поправкой
на целевой RPE:
    weight = 1RM / (1 + reps/30) * rpe_factor(target_RPE)
где rpe_factor(10)=1.0, rpe_factor(9)≈0.96, rpe_factor(8)≈0.92, и так далее.
Фактический RPE = целевой + шум + текущая усталость мышечной группы.
"""
from __future__ import annotations
from dataclasses import dataclass, field
from datetime import date, timedelta
import math
import random
from typing import Iterable

from .exercises import EXERCISES, EXERCISE_BY_ID, ExerciseDef


# ---------- Сплиты (недельные расписания) ----------

# Каждый сплит — это список "дней", где каждый день — список упражнений.
# Дни упорядочены: 0 = первый тренировочный день недели, и т.д. Конкретные
# weekday'и расставит планировщик равномерно по неделе.
PUSH = [1, 11, 14, 12]               # жим лёжа, жим стоя, фр.жим, махи
PULL = [4, 5, 7, 13]                 # становая, тяга штанги, тяга блока, бицепс
LEGS = [8, 9, 10]                    # присед, жим ногами, RDL
UPPER = [1, 5, 11, 13, 14, 12]
LOWER = [8, 10, 9]
FB_A = [1, 5, 8, 13]                 # FullBody A
FB_B = [11, 7, 10, 14]               # FullBody B

SPLITS: dict[str, list[list[int]]] = {
    "FB-2":   [FB_A, FB_B],
    "FB-3":   [FB_A, FB_B, FB_A],
    "PPL-3":  [PUSH, PULL, LEGS],
    "UL-4":   [UPPER, LOWER, UPPER, LOWER],
    "PPL-5":  [PUSH, PULL, LEGS, UPPER, LOWER],
}


# ---------- Анкета и состояние ----------

@dataclass
class AthleteProfile:
    athlete_id: int
    sex: str                  # "M" / "F"
    bodyweight_kg: float
    age: int
    training_age_months: int  # стаж в момент старта симуляции
    split_name: str           # ключ SPLITS

    # "Характер"
    progression_speed: float   # 0.5–1.5: множитель скорости роста 1ПМ
    recovery_factor: float     # 0.7–1.3: как быстро уходит усталость
    consistency: float         # 0.6–1.0: 1 - вероятность пропуска тренировки
    noise_rpe: float           # 0.3–0.9: стандартное отклонение шума RPE
    target_rpe: float          # 7.0–9.0: к чему стремится в рабочих сетах
    injury_proneness: float    # 0.0–1.0: индивидуальная склонность к травмам


@dataclass
class ExerciseState:
    current_1rm: float
    ceiling_1rm: float
    starting_1rm: float        # для статистики/отладки
    peak_1rm: float = 0.0      # макс. достигнутый 1ПМ (для "мышечной памяти")
    last_date: date | None = None  # дата последней тренировки этого упражнения


# ---------- Генерация анкеты ----------

def _sample_profile(rng: random.Random, athlete_id: int) -> AthleteProfile:
    sex = rng.choices(["M", "F"], weights=[0.7, 0.3])[0]
    if sex == "M":
        bw = max(55.0, rng.gauss(82.0, 12.0))
    else:
        bw = max(45.0, rng.gauss(65.0, 10.0))
    age = max(16, min(60, int(rng.gauss(28, 7))))
    # Стаж: смесь новичков и опытных
    training_age_months = max(0, int(rng.lognormvariate(2.8, 1.0)))  # медиана ~16 мес
    split = rng.choices(
        list(SPLITS.keys()),
        weights=[0.15, 0.20, 0.30, 0.25, 0.10],
    )[0]
    return AthleteProfile(
        athlete_id=athlete_id,
        sex=sex,
        bodyweight_kg=round(bw, 1),
        age=age,
        training_age_months=training_age_months,
        split_name=split,
        progression_speed=rng.uniform(0.6, 1.4),
        recovery_factor=rng.uniform(0.7, 1.3),
        consistency=rng.uniform(0.65, 0.98),
        noise_rpe=rng.uniform(0.3, 0.9),
        target_rpe=rng.uniform(7.0, 9.0),
        # Склонность к травмам смещена к нулю: у большинства атлетов травм
        # не будет, но у части за период симуляции случится 1+ эпизод.
        injury_proneness=rng.random() ** 1.5,
    )


def _intermediate_1rm(ex: ExerciseDef, p: AthleteProfile) -> float:
    """Опорный 1ПМ "Intermediate"-атлета этого пола и веса (анкер шкалы)."""
    base = ex.base_1rm_kg * (p.bodyweight_kg / 75.0) ** ex.bw_exponent
    if p.sex == "F":
        base *= ex.female_factor
    return base


def _genetic_ceiling_1rm(
    rng: random.Random, ex: ExerciseDef, p: AthleteProfile,
) -> float:
    """Личный генетический потолок 1ПМ — то, к чему сходится при бесконечном
    стаже. Не зависит от текущего стажа, зависит только от пола/веса и
    индивидуальных генетических факторов (шум)."""
    base = _intermediate_1rm(ex, p)
    return round(base * ex.ceiling_factor * rng.uniform(0.85, 1.15), 1)


def _experience_progress(months: int) -> float:
    """Доля пути к потолку, пройденная атлетом за `months` месяцев тренировок.
    0 мес → ~0.30, 6 мес → ~0.45, 12 мес → ~0.55, 24 мес → ~0.70,
    60 мес → ~0.85, 120 мес → ~0.92. Логистическая кривая, аналогичная
    тому, как растёт прогресс внутри симуляции."""
    return 0.30 + 0.65 * months / (months + 18)


# ---------- Виртуальный атлет ----------

# Относительная частота травм по мышечным группам. Базовые многосуставные
# движения и крупные группы травмируются чаще, чем изоляция мелких мышц.
_MUSCLE_INJURY_WEIGHTS: dict[str, float] = {
    "Спина": 3.0,
    "Ноги": 3.0,
    "Грудь": 2.0,
    "Плечи": 2.0,
    "Бицепс": 1.0,
    "Трицепс": 1.0,
}

# Базовая вероятность травмы за одну неделю при injury_proneness = 1.0.
_WEEKLY_INJURY_BASE = 0.02


class VirtualAthlete:
    def __init__(self, profile: AthleteProfile, rng: random.Random):
        self.profile = profile
        self.rng = rng
        # Состояние по каждому упражнению
        self.exercise_state: dict[int, ExerciseState] = {}
        # Сначала вытягиваем генетический потолок 1ПМ — он одинаков для всех
        # стажей; новичок и ветеран этого пола/веса имеют похожий потолок,
        # но стартуют от него на разной дистанции.
        progress = _experience_progress(profile.training_age_months)
        for ex in EXERCISES:
            ceiling = _genetic_ceiling_1rm(rng, ex, profile)
            # Стартовая позиция = доля от потолка по логистической кривой,
            # плюс индивидуальный шум.
            start = round(ceiling * progress * rng.uniform(0.85, 1.15), 1)
            # На всякий случай не даём старту превысить потолок.
            start = min(start, round(ceiling * 0.98, 1))
            self.exercise_state[ex.id] = ExerciseState(
                current_1rm=start,
                ceiling_1rm=ceiling,
                starting_1rm=start,
                peak_1rm=start,
            )
        # Усталость по мышечным группам (0 = свежий, растёт от тренировок)
        self.fatigue: dict[str, float] = {}
        # Травмы: мышечная группа -> дата, до которой группа не тренируется.
        self.injured_until: dict[str, date] = {}
        # Какие мышечные группы вообще тренирует этот атлет (по своему сплиту).
        self.trained_muscles: set[str] = set()
        for day_exercises in SPLITS[profile.split_name]:
            for eid in day_exercises:
                self.trained_muscles.add(EXERCISE_BY_ID[eid].primary_muscle)

    # ---- Внутренние ----

    @staticmethod
    def _rpe_factor(rpe: float) -> float:
        """Доля от 1ПМ, посильная на одно повторение при данном RPE.
        RPE 10 → 100%, каждый шаг ниже ≈ -4%."""
        return max(0.5, 1.0 - (10.0 - rpe) * 0.04)

    @staticmethod
    def _epley_factor(reps: int) -> float:
        """Доля от 1ПМ, посильная на N повторений при RPE 10 (формула Эпли)."""
        return 1.0 / (1.0 + reps / 30.0)

    def _weight_for(self, ex: ExerciseDef, reps: int, target_rpe: float) -> float:
        one_rm = self.exercise_state[ex.id].current_1rm
        raw = one_rm * self._epley_factor(reps) * self._rpe_factor(target_rpe)
        # Округление до 2.5 кг (штанги/гантели) или 5 кг (тренажёры/блок)
        step = 2.5 if ex.equipment in (0, 1) else 5.0
        return max(step, round(raw / step) * step)

    def _decay_fatigue(self):
        for k in list(self.fatigue.keys()):
            self.fatigue[k] *= math.exp(-0.5 / max(0.4, self.profile.recovery_factor))
            if self.fatigue[k] < 0.05:
                del self.fatigue[k]

    def _apply_session_progression(self, ex: ExerciseDef, min_actual_rpe: float):
        """Прогрессия 1ПМ после упражнения.

        Берём min RPE по рабочим сетам — это "честная" сложность рабочего веса
        в начале тренировки, до накопления усталости внутри сессии. Если она
        близка к целевой — вес подобран правильно, идёт нормальная прогрессия.
        Если ниже — был запас, прогрессия ускоряется. Если выше — был тяжёлый
        день, прогрессия замедляется.
        """
        st = self.exercise_state[ex.id]
        # Дистанция до потолка ∈ (0, 1]
        room = max(0.0, (st.ceiling_1rm - st.current_1rm) / st.ceiling_1rm)
        # Сигнал из RPE: ±20% от номинальной скорости при отклонении в 1 единицу.
        signal = 1.0 + (self.profile.target_rpe - min_actual_rpe) * 0.2
        signal = max(0.2, min(1.6, signal))
        # weekly_growth_max уже задан как максимальный недельный прирост; каждая
        # сессия даёт примерно его (две сессии в неделю → суммарно ~2x при
        # благоприятных условиях, но room быстро уменьшит это к плато).
        growth = (
            ex.weekly_growth_max
            * room
            * self.profile.progression_speed
            * signal
            * self.rng.uniform(0.6, 1.4)
        )
        # "Мышечная память": если атлет сейчас ниже ранее достигнутого пика
        # (например, после травмы или паузы), утраченную силу он возвращает
        # значительно быстрее, чем набирал впервые. Чем глубже просадка от
        # пика, тем сильнее ускорение восстановления.
        if st.current_1rm < st.peak_1rm:
            deficit = (st.peak_1rm - st.current_1rm) / st.peak_1rm
            growth *= 1.0 + 4.0 * deficit
        st.current_1rm = round(st.current_1rm * (1 + growth), 1)
        st.peak_1rm = max(st.peak_1rm, st.current_1rm)

    def _apply_detraining(self, ex: ExerciseDef, workout_date: date):
        """Потеря силы из-за длительной паузы в тренировках упражнения
        (детренинг/атрофия). До ~10 дней простоя — без потерь, дальше 1ПМ
        снижается примерно на 1.2% за каждую неделю простоя сверх этого
        порога. Скорость потери смягчается восстанавливаемостью атлета."""
        st = self.exercise_state[ex.id]
        if st.last_date is None:
            return
        gap_days = (workout_date - st.last_date).days
        if gap_days <= 10:
            return
        weeks_off = (gap_days - 10) / 7.0
        rate = 0.012 / max(0.4, self.profile.recovery_factor)
        decay = min(0.30, rate * weeks_off)
        # Не опускаемся ниже разумного дна (30% от потолка) — даже после
        # долгого перерыва базовая сила частично сохраняется.
        floor = st.ceiling_1rm * 0.3
        st.current_1rm = round(max(floor, st.current_1rm * (1 - decay)), 1)

    def maybe_injure(self, week_start: date):
        """Розыгрыш травмы на ближайшую неделю. При срабатывании наносит
        резкий спад 1ПМ всем упражнениям поражённой мышечной группы и
        выводит эту группу из тренировок на 1–3 недели."""
        p = _WEEKLY_INJURY_BASE * self.profile.injury_proneness
        if self.rng.random() >= p:
            return
        muscles = list(self.trained_muscles)
        if not muscles:
            return
        weights = [_MUSCLE_INJURY_WEIGHTS.get(m, 1.0) for m in muscles]
        muscle = self.rng.choices(muscles, weights=weights)[0]
        # Тяжесть: ~60% лёгкие (малый спад, неделя отдыха),
        # ~40% средние (заметный спад, 2–3 недели).
        if self.rng.random() < 0.6:
            drop = self.rng.uniform(0.08, 0.18)
            rest_weeks = 1
        else:
            drop = self.rng.uniform(0.18, 0.35)
            rest_weeks = self.rng.randint(2, 3)
        for ex in EXERCISES:
            if ex.primary_muscle != muscle:
                continue
            st = self.exercise_state.get(ex.id)
            if st is None:
                continue
            floor = st.ceiling_1rm * 0.3
            st.current_1rm = round(max(floor, st.current_1rm * (1 - drop)), 1)
        self.injured_until[muscle] = week_start + timedelta(weeks=rest_weeks)

    def is_muscle_blocked(self, muscle: str, day: date) -> bool:
        """True, если мышечная группа сейчас на восстановлении после травмы
        и тренировать её нельзя."""
        end = self.injured_until.get(muscle)
        return end is not None and day < end

    # ---- Публичный API: одна тренировка ----

    def simulate_workout(
        self, workout_date: date, exercise_ids: Iterable[int]
    ) -> list[dict]:
        """Симулирует одну тренировку. Возвращает список строк-сетов."""
        rows: list[dict] = []
        for order_idx, ex_id in enumerate(exercise_ids, start=1):
            ex = EXERCISE_BY_ID[ex_id]
            # Перед расчётом весов учитываем простой по этому упражнению
            # (детренинг), затем фиксируем дату текущей тренировки.
            self._apply_detraining(ex, workout_date)
            self.exercise_state[ex.id].last_date = workout_date
            fatigue_here = self.fatigue.get(ex.primary_muscle, 0.0)
            target_rpe = self.profile.target_rpe
            # Целевые повторы — случайные из диапазона упражнения
            target_reps = self.rng.randint(*ex.rep_range)

            # 1-2 разминочных сета (для упражнений со штангой/гантелями/тренажёрами)
            warmup_sets = 0
            if ex.equipment in (0, 1, 2):
                warmup_sets = self.rng.choice([1, 2])
            base_working_weight = self._weight_for(ex, target_reps, target_rpe)
            step = 2.5 if ex.equipment in (0, 1) else 5.0

            set_num = 1
            for w in range(warmup_sets):
                frac = 0.4 + 0.2 * w  # 40%, 60%
                wt = max(step, round(base_working_weight * frac / step) * step)
                reps = self.rng.choice([8, 10, 12])
                rpe = round(self.rng.uniform(4.0, 6.0), 1)
                rows.append(self._row(
                    workout_date, order_idx, ex, set_num,
                    weight=wt, reps=reps, rpe=rpe, is_warmup=True,
                ))
                set_num += 1

            # Рабочие подходы (3–5)
            n_working = self.rng.randint(3, 5)
            rpes: list[float] = []
            for i in range(n_working):
                # Лёгкий "дрейф" RPE вверх к последним подходам (накопление усталости)
                drift = i * 0.25
                actual_rpe = (
                    target_rpe
                    + drift
                    + fatigue_here
                    + self.rng.gauss(0.0, self.profile.noise_rpe)
                )
                actual_rpe = max(5.0, min(10.0, round(actual_rpe, 1)))
                # Иногда атлет не доходит до планового количества повторов
                reps_done = target_reps
                if actual_rpe >= 9.5 and self.rng.random() < 0.4:
                    reps_done = max(1, target_reps - self.rng.randint(1, 2))
                rows.append(self._row(
                    workout_date, order_idx, ex, set_num,
                    weight=base_working_weight, reps=reps_done,
                    rpe=actual_rpe, is_warmup=False,
                ))
                rpes.append(actual_rpe)
                set_num += 1

            # Прогрессия после упражнения — по минимальному RPE (см. метод)
            if rpes:
                self._apply_session_progression(ex, min(rpes))
            # Прирост усталости в мышечной группе
            self.fatigue[ex.primary_muscle] = (
                self.fatigue.get(ex.primary_muscle, 0.0) + 0.35
            )
        return rows

    def end_of_day(self):
        """Спад усталости между днями."""
        self._decay_fatigue()

    # ---- Помощник ----

    def _row(
        self, workout_date: date, order_idx: int, ex: ExerciseDef,
        set_num: int, weight: float, reps: int, rpe: float, is_warmup: bool,
    ) -> dict:
        p = self.profile
        return {
            "athlete_id": p.athlete_id,
            "sex": p.sex,
            "bodyweight_kg": p.bodyweight_kg,
            "age": p.age,
            "training_age_months": p.training_age_months,
            "split": p.split_name,
            "target_rpe_profile": round(p.target_rpe, 2),
            "date": workout_date.isoformat(),
            "exercise_id": ex.id,
            "exercise_name": ex.name,
            "exercise_name_en": ex.name_en,
            "is_compound": ex.is_compound,
            "equipment": ex.equipment,
            "primary_muscle": ex.primary_muscle,
            "exercise_order": order_idx,
            "set_number": set_num,
            "is_warmup": is_warmup,
            "weight_kg": weight,
            "reps": reps,
            "rpe": rpe,
            "true_1rm_kg": round(self.exercise_state[ex.id].current_1rm, 1),
        }


# ---------- Расписание тренировок ----------

# Какие weekday'и (0=Mon … 6=Sun) использовать в зависимости от количества дней.
_WEEKDAY_PATTERNS = {
    2: [1, 4],
    3: [0, 2, 4],
    4: [0, 1, 3, 4],
    5: [0, 1, 2, 4, 5],
}


def _weekdays_for_split(split_name: str) -> list[int]:
    n = int(split_name.rsplit("-", 1)[1])
    return _WEEKDAY_PATTERNS[n]


def simulate_athlete(
    athlete_id: int, weeks: int, rng: random.Random, end_date: date | None = None,
) -> list[dict]:
    """Полная симуляция одного виртуального атлета на `weeks` недель."""
    end_date = end_date or date.today()
    start_date = end_date - timedelta(days=weeks * 7)
    profile = _sample_profile(rng, athlete_id)
    athlete = VirtualAthlete(profile, rng)
    days_pattern = profile.split_name and SPLITS[profile.split_name]
    weekday_slots = _weekdays_for_split(profile.split_name)

    rows: list[dict] = []
    day = start_date
    week_idx = 0
    day_in_week_idx = 0
    while day < end_date:
        weekday = day.weekday()
        # В начале недели (понедельник) разыгрываем возможную травму.
        if weekday == 0:
            athlete.maybe_injure(day)
        if weekday in weekday_slots:
            # пропуск из-за лени/жизни
            if rng.random() < profile.consistency:
                slot = weekday_slots.index(weekday)
                ex_ids = days_pattern[slot % len(days_pattern)]
                # Исключаем упражнения на травмированные группы — они
                # выпадают из тренировки на время восстановления.
                ex_ids = [
                    eid for eid in ex_ids
                    if not athlete.is_muscle_blocked(
                        EXERCISE_BY_ID[eid].primary_muscle, day)
                ]
                if ex_ids:
                    rows.extend(athlete.simulate_workout(day, ex_ids))
        athlete.end_of_day()
        day += timedelta(days=1)
        # Считаем неделю прожитой при достижении воскресенья
        if weekday == 6:
            week_idx += 1
    return rows

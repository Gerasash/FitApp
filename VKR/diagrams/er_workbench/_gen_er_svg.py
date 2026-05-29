# -*- coding: utf-8 -*-
"""Рисует ER-диаграмму FitApp в стиле MySQL Workbench как SVG (без внешних программ)."""
import io

HDR = 30
ROW = 24
PAD = 8
FONT = "Tahoma, Verdana, Geneva, sans-serif"

HDR_FILL = "#C7D8EC"
HDR_STROKE = "#8FA9C7"
BORDER = "#8FA9C7"
ROW_ALT = "#F4F8FC"
TXT = "#1B2A3A"
KEY = "#E0A800"
FK = "#C0504D"
UQ = "#4F81BD"
COL = "#9DB0C2"

# (id, имя, x, y, w, [(тип, "поле", "ТИП"), ...])
TABLES = {
    "users": ("users", 1110, 40, 250, [
        ("pk", "id", "INT"), ("col", "email", "VARCHAR(60)"),
        ("col", "display_name", "VARCHAR(60)"), ("col", "bodyweight", "DOUBLE"),
        ("col", "age", "INT"), ("col", "sex", "INT"),
        ("col", "experience_start", "DATETIME"), ("col", "target_rpe", "DOUBLE"),
        ("col", "created_at", "DATETIME"), ("col", "updated_at", "DATETIME"),
        ("col", "is_deleted", "TINYINT"),
    ]),
    "workouts": ("Workouts", 590, 40, 250, [
        ("pk", "id", "INT"), ("uq", "SyncId", "VARCHAR(36)"),
        ("col", "name", "VARCHAR(255)"), ("col", "Description", "TEXT"),
        ("col", "StartTime", "DATETIME"), ("fk", "UserId", "INT"),
        ("col", "UpdatedAt", "DATETIME"), ("col", "IsDeleted", "TINYINT"),
    ]),
    "we": ("WorkoutExercises", 70, 330, 260, [
        ("pk", "id", "INT"), ("uq", "SyncId", "VARCHAR(36)"),
        ("fk", "WorkoutId", "INT"), ("col", "WorkoutSyncId", "VARCHAR(36)"),
        ("fk", "ExerciseId", "INT"), ("col", "OrderIndex", "INT"),
        ("col", "UpdatedAt", "DATETIME"), ("col", "IsDeleted", "TINYINT"),
    ]),
    "exercise": ("Exercises", 590, 350, 250, [
        ("pk", "id", "INT"), ("col", "Name", "VARCHAR(255)"),
        ("col", "NameEn", "VARCHAR(255)"), ("fk", "PrimaryMuscleGroupId", "INT"),
        ("col", "EquipmentType", "INT"), ("col", "Category", "INT"),
        ("col", "Mechanic", "INT"), ("col", "Instructions", "TEXT"),
        ("col", "IsCustom", "TINYINT"), ("col", "IsArchived", "TINYINT"),
        ("col", "IsFavorite", "TINYINT"), ("col", "CreatedAt", "DATETIME"),
    ]),
    "muscle": ("MuscleGroups", 1110, 400, 220, [
        ("pk", "id", "INT"), ("col", "name", "VARCHAR(255)"),
    ]),
    "sets": ("ExerciseSets", 70, 700, 260, [
        ("pk", "id", "INT"), ("uq", "SyncId", "VARCHAR(36)"),
        ("fk", "WorkoutExerciseId", "INT"), ("col", "WorkoutExerciseSyncId", "VARCHAR(36)"),
        ("col", "SetNumber", "INT"), ("col", "Weight", "DOUBLE"),
        ("col", "Reps", "INT"), ("col", "RPE", "DOUBLE"),
        ("col", "IsAssisted", "TINYINT"), ("col", "Kind", "INT"),
        ("col", "UpdatedAt", "DATETIME"), ("col", "IsDeleted", "TINYINT"),
    ]),
    "wmg": ("WorkoutMuscleGroups", 590, 700, 250, [
        ("pk", "id", "INT"), ("fk", "workout_id", "INT"),
        ("fk", "muscle_group_id", "INT"), ("col", "UpdatedAt", "DATETIME"),
        ("col", "IsDeleted", "TINYINT"),
    ]),
}

# (parent, p_side, p_frac, child, c_side, c_frac, manual_pts|None)
# manual_pts задаёт промежуточные ортогональные точки в обход таблиц; None -> авто-Z.
EDGES = [
    ("users", "L", 0.30, "workouts", "R", 0.30, None),
    ("workouts", "B", 0.40, "we", "T", 0.60, [(None, 296), (236, None)]),
    ("exercise", "L", 0.30, "we", "R", 0.30, None),
    ("we", "B", 0.50, "sets", "T", 0.50, None),
    ("workouts", "B", 0.62, "wmg", "T", 0.42,
     [(None, 300), (905, None), (None, 685), (695, None)]),
    ("muscle", "B", 0.50, "wmg", "R", 0.50, None),
    ("muscle", "L", 0.50, "exercise", "R", 0.30, None),
]

NORMAL = {"L": (-1, 0), "R": (1, 0), "T": (0, -1), "B": (0, 1)}
STUB = 22


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def height(fields):
    return HDR + ROW * len(fields)


def side_point(tid, side, frac):
    _, x, y, w, fields = TABLES[tid][0], TABLES[tid][1], TABLES[tid][2], TABLES[tid][3], TABLES[tid][4]
    h = height(fields)
    if side == "L":
        return (x, y + h * frac)
    if side == "R":
        return (x + w, y + h * frac)
    if side == "T":
        return (x + w * frac, y)
    return (x + w * frac, y + h)  # B


def diamond(cx, cy, color, r=5):
    pts = "{},{} {},{} {},{} {},{}".format(cx, cy - r, cx + r, cy, cx, cy + r, cx - r, cy)
    return '<polygon points="{p}" fill="{c}" stroke="#5A5A5A" stroke-width="0.6"/>'.format(p=pts, c=color)


def keyicon(cx, cy):
    # маленький ключ: кольцо слева + стержень с зубцами вправо
    s = []
    s.append('<circle cx="{}" cy="{}" r="4" fill="none" stroke="{}" stroke-width="2"/>'.format(cx - 3, cy, KEY))
    s.append('<line x1="{}" y1="{}" x2="{}" y2="{}" stroke="{}" stroke-width="2"/>'.format(cx + 1, cy, cx + 7, cy, KEY))
    s.append('<line x1="{}" y1="{}" x2="{}" y2="{}" stroke="{}" stroke-width="2"/>'.format(cx + 7, cy, cx + 7, cy + 4, KEY))
    s.append('<line x1="{}" y1="{}" x2="{}" y2="{}" stroke="{}" stroke-width="2"/>'.format(cx + 4, cy, cx + 4, cy + 3, KEY))
    return "".join(s)


def draw_table(tid):
    name, x, y, w, fields = TABLES[tid]
    h = height(fields)
    out = []
    out.append('<g>')
    # тень
    out.append('<rect x="{}" y="{}" width="{}" height="{}" rx="4" fill="#000" opacity="0.08"/>'
               .format(x + 3, y + 3, w, h))
    # контур
    out.append('<rect x="{}" y="{}" width="{}" height="{}" rx="4" fill="#FFFFFF" stroke="{}" stroke-width="1"/>'
               .format(x, y, w, h, BORDER))
    # заголовок
    out.append('<path d="M{x},{y1} L{x},{y0} Q{x},{yq} {xr},{yq} L{xe},{yq} Q{xe2},{yq} {xe2},{y0} L{xe2},{y1} Z" '
               'fill="{f}" stroke="{s}" stroke-width="1"/>'.format(
                   x=x, y0=y, y1=y + HDR, yq=y, xr=x + 4, xe=x + w - 4, xe2=x + w, f=HDR_FILL, s=HDR_STROKE))
    # иконка таблицы
    out.append('<rect x="{}" y="{}" width="12" height="9" rx="1.5" fill="#5B7DA8" stroke="#3F5E86" stroke-width="0.6"/>'
               .format(x + 8, y + HDR / 2 - 4.5))
    out.append('<text x="{}" y="{}" font-family="{}" font-size="13" font-weight="bold" fill="{}">{}</text>'
               .format(x + 26, y + HDR / 2 + 4, FONT, TXT, esc(name)))
    # строки
    for i, (kind, fname, ftype) in enumerate(fields):
        ry = y + HDR + i * ROW
        if i % 2 == 1:
            out.append('<rect x="{}" y="{}" width="{}" height="{}" fill="{}"/>'
                       .format(x + 1, ry, w - 2, ROW, ROW_ALT))
        cx = x + 16
        cy = ry + ROW / 2
        if kind == "pk":
            out.append(keyicon(cx, cy))
        elif kind == "fk":
            out.append(diamond(cx, cy, FK))
        elif kind == "uq":
            out.append(diamond(cx, cy, UQ))
        else:
            out.append(diamond(cx, cy, COL))
        weight = ' font-weight="bold"' if kind == "pk" else ""
        deco = ' text-decoration="underline"' if kind == "pk" else ""
        out.append('<text x="{}" y="{}" font-family="{}" font-size="11.5" fill="{}"{}{}>{}</text>'
                   .format(x + 30, cy + 4, FONT, TXT, weight, deco, esc(fname)))
        out.append('<text x="{}" y="{}" font-family="{}" font-size="10.5" fill="#6B7B8C" text-anchor="end">{}</text>'
                   .format(x + w - 8, cy + 4, FONT, esc(ftype)))
        out.append('<line x1="{}" y1="{}" x2="{}" y2="{}" stroke="#E3EAF1" stroke-width="0.7"/>'
                   .format(x + 1, ry + ROW, x + w - 1, ry + ROW))
    out.append('</g>')
    return "\n".join(out)


def route(parent, ps, pf, child, cs, cf, manual):
    S = side_point(parent, ps, pf)
    E = side_point(child, cs, cf)
    snx, sny = NORMAL[ps]
    enx, eny = NORMAL[cs]
    S2 = (S[0] + snx * STUB, S[1] + sny * STUB)
    E2 = (E[0] + enx * STUB, E[1] + eny * STUB)
    pts = [S, S2]
    if manual is not None:
        px, py = S2
        for wx, wy in manual:
            nx = wx if wx is not None else px
            ny = wy if wy is not None else py
            pts.append((nx, ny))
            px, py = nx, ny
    else:
        # авто-Z: ось первого хода — по нормали стартовой грани
        if ps in ("L", "R"):
            mid = (E2[0], S2[1])
        else:
            mid = (S2[0], E2[1])
        if mid != S2 and mid != E2:
            pts.append(mid)
    pts.append(E2)
    pts.append(E)
    return pts


def crowsfoot(p, prev):
    px, py = p
    ax, ay = prev
    dx, dy = px - ax, py - ay
    import math
    L = math.hypot(dx, dy) or 1
    ux, uy = dx / L, dy / L           # направление прихода (к таблице)
    bx, by = px - ux * 13, py - uy * 13
    pxp, pyp = -uy, ux                 # перпендикуляр
    sp = 6.5
    lines = []
    for s in (-1, 0, 1):
        ex, ey = px + pxp * sp * s, py + pyp * sp * s
        lines.append('<line x1="{:.1f}" y1="{:.1f}" x2="{:.1f}" y2="{:.1f}" stroke="#5A6B7B" stroke-width="1.4"/>'
                     .format(bx, by, ex, ey))
    return "".join(lines)


def onebar(p, nxt):
    px, py = p
    nx, ny = nxt
    import math
    dx, dy = nx - px, ny - py
    L = math.hypot(dx, dy) or 1
    ux, uy = dx / L, dy / L
    cx, cy = px + ux * 12, py + uy * 12
    pxp, pyp = -uy, ux
    sp = 6
    return ('<line x1="{:.1f}" y1="{:.1f}" x2="{:.1f}" y2="{:.1f}" stroke="#5A6B7B" stroke-width="1.4"/>'
            .format(cx + pxp * sp, cy + pyp * sp, cx - pxp * sp, cy - pyp * sp))


def draw_edge(e):
    parent, ps, pf, child, cs, cf, manual = e
    pts = route(parent, ps, pf, child, cs, cf, manual)
    d = "M{:.1f},{:.1f} ".format(*pts[0]) + " ".join("L{:.1f},{:.1f}".format(x, y) for x, y in pts[1:])
    out = ['<path d="{}" fill="none" stroke="#5A6B7B" stroke-width="1.4"/>'.format(d)]
    out.append(onebar(pts[0], pts[1]))         # «один» у родителя
    out.append(crowsfoot(pts[-1], pts[-2]))    # «многие» у потомка
    return "\n".join(out)


def main():
    W, H = 1420, 1040
    out = io.StringIO()
    out.write('<svg xmlns="http://www.w3.org/2000/svg" width="{}" height="{}" '
              'viewBox="0 0 {} {}" font-family="{}">\n'.format(W, H, W, H, FONT))
    out.write('<rect width="{}" height="{}" fill="#FFFFFF"/>\n'.format(W, H))
    out.write('<text x="{}" y="28" font-size="16" font-weight="bold" fill="#1B2A3A">'
              'ER-диаграмма базы данных FitApp</text>\n'.format(20))
    for e in EDGES:                 # связи под таблицами
        out.write(draw_edge(e) + "\n")
    for tid in TABLES:
        out.write(draw_table(tid) + "\n")
    out.write('</svg>\n')
    with io.open("diagram_er_workbench.svg", "w", encoding="utf-8") as f:
        f.write(out.getvalue())
    print("written diagram_er_workbench.svg")


if __name__ == "__main__":
    main()

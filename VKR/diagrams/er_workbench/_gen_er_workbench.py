# -*- coding: utf-8 -*-
"""Генератор ER-диаграммы FitApp в стиле MySQL Workbench (для draw.io)."""
import io

HEADER_H = 26
ROW_H = 24
FONT = "Helvetica"

# тип поля -> (иконка, цвет иконки)
ICON = {
    "pk":  ("\U0001F511", "#C9A227"),   # 🔑 жёлтый ключ
    "fk":  ("◆", "#B85450"),         # ◆ красный ромб
    "uq":  ("◈", "#6C8EBF"),         # ◈ синий ромб (уникальный индекс)
    "col": ("◇", "#9AA7B0"),         # ◇ серый ромб
}

# (id, имя таблицы, x, y, width, [(тип, "поле : ТИП"), ...])
TABLES = [
    ("users", "users", 1080, 40, 250, [
        ("pk",  "id : INT"),
        ("col", "email : VARCHAR(60)"),
        ("col", "display_name : VARCHAR(60)"),
        ("col", "bodyweight : REAL"),
        ("col", "age : INT"),
        ("col", "sex : INT"),
        ("col", "experience_start : DATETIME"),
        ("col", "target_rpe : REAL"),
        ("col", "created_at : DATETIME"),
        ("col", "updated_at : DATETIME"),
        ("col", "is_deleted : INT"),
    ]),
    ("workouts", "Workouts", 40, 40, 260, [
        ("pk",  "id : INT"),
        ("uq",  "SyncId : VARCHAR(36)"),
        ("col", "name : VARCHAR(255)"),
        ("col", "Description : TEXT"),
        ("col", "StartTime : DATETIME"),
        ("fk",  "UserId : INT"),
        ("col", "UpdatedAt : DATETIME"),
        ("col", "IsDeleted : INT"),
    ]),
    ("wmg", "WorkoutMuscleGroups", 430, 70, 250, [
        ("pk",  "id : INT"),
        ("fk",  "workout_id : INT"),
        ("fk",  "muscle_group_id : INT"),
        ("col", "UpdatedAt : DATETIME"),
        ("col", "IsDeleted : INT"),
    ]),
    ("muscle", "MuscleGroups", 800, 70, 220, [
        ("pk",  "id : INT"),
        ("col", "name : VARCHAR(255)"),
    ]),
    ("we", "WorkoutExercises", 430, 400, 260, [
        ("pk",  "id : INT"),
        ("uq",  "SyncId : VARCHAR(36)"),
        ("fk",  "WorkoutId : INT"),
        ("col", "WorkoutSyncId : VARCHAR(36)"),
        ("fk",  "ExerciseId : INT"),
        ("col", "OrderIndex : INT"),
        ("col", "UpdatedAt : DATETIME"),
        ("col", "IsDeleted : INT"),
    ]),
    ("exercise", "Exercises", 800, 360, 250, [
        ("pk",  "id : INT"),
        ("col", "Name : VARCHAR(255)"),
        ("col", "NameEn : VARCHAR(255)"),
        ("fk",  "PrimaryMuscleGroupId : INT"),
        ("col", "EquipmentType : INT"),
        ("col", "Category : INT"),
        ("col", "Mechanic : INT"),
        ("col", "Instructions : TEXT"),
        ("col", "IsCustom : INT"),
        ("col", "IsArchived : INT"),
        ("col", "IsFavorite : INT"),
        ("col", "CreatedAt : DATETIME"),
    ]),
    ("sets", "ExerciseSets", 40, 430, 260, [
        ("pk",  "id : INT"),
        ("uq",  "SyncId : VARCHAR(36)"),
        ("fk",  "WorkoutExerciseId : INT"),
        ("col", "WorkoutExerciseSyncId : VARCHAR(36)"),
        ("col", "SetNumber : INT"),
        ("col", "Weight : REAL"),
        ("col", "Reps : INT"),
        ("col", "RPE : REAL"),
        ("col", "IsAssisted : INT"),
        ("col", "Kind : INT"),
        ("col", "UpdatedAt : DATETIME"),
        ("col", "IsDeleted : INT"),
    ]),
]

# (src, dst, "метка", тип связи): "1:M" воронья лапка от src(один) к dst(много)
EDGES = [
    ("users", "workouts", "1 : M"),
    ("workouts", "wmg", "1 : M"),
    ("muscle", "wmg", "1 : M"),
    ("workouts", "we", "1 : M"),
    ("exercise", "we", "1 : M"),
    ("we", "sets", "1 : M"),
    ("muscle", "exercise", "1 : M"),
]


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def main():
    out = io.StringIO()
    out.write('<mxfile host="app.diagrams.net" version="24.7.17">\n')
    out.write('  <diagram id="er_fitapp_wb" name="ER FitApp (Workbench style)">\n')
    out.write('    <mxGraphModel dx="1400" dy="900" grid="1" gridSize="10" guides="1" '
              'tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" '
              'pageWidth="1400" pageHeight="1000" math="0" shadow="0">\n')
    out.write('      <root>\n')
    out.write('        <mxCell id="0" />\n')
    out.write('        <mxCell id="1" parent="0" />\n')

    for tid, name, x, y, w, fields in TABLES:
        h = HEADER_H + ROW_H * len(fields)
        table_style = (
            "shape=table;startSize={hh};container=1;collapsible=0;childLayout=tableLayout;"
            "fixedRows=1;rowLines=1;columnLines=0;fontStyle=1;align=center;"
            "fontColor=#FFFFFF;fillColor=#6C8EBF;strokeColor=#4A6785;"
            "fontFamily={f};fontSize=12;html=1;"
        ).format(hh=HEADER_H, f=FONT)
        out.write('        <mxCell id="{tid}" value="{nm}" style="{st}" vertex="1" parent="1">\n'
                  .format(tid=tid, nm=esc(name), st=table_style))
        out.write('          <mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry" />\n'
                  .format(x=x, y=y, w=w, h=h))
        out.write('        </mxCell>\n')

        for i, (kind, text) in enumerate(fields):
            icon, icolor = ICON[kind]
            rid = "{}_r{}".format(tid, i)
            row_style = (
                "shape=tableRow;horizontal=0;startSize=0;swimlaneHead=0;swimlaneBody=0;"
                "strokeColor=inherit;top=0;left=0;bottom=0;right=0;collapsible=0;"
                "dropTarget=0;fillColor=#FFFFFF;points=[[0,0.5,0],[1,0.5,0]];"
                "portConstraint=eastwest;"
            )
            out.write('        <mxCell id="{rid}" value="" style="{st}" vertex="1" parent="{tid}">\n'
                      .format(rid=rid, st=row_style, tid=tid))
            out.write('          <mxGeometry y="{yy}" width="{w}" height="{rh}" as="geometry" />\n'
                      .format(yy=HEADER_H + i * ROW_H, w=w, rh=ROW_H))
            out.write('        </mxCell>\n')

            cell_icon = (
                "shape=partialRectangle;overflow=hidden;connectable=0;fillColor=none;"
                "top=0;left=0;bottom=0;right=0;pointerEvents=1;fontStyle=0;align=center;"
                "fontColor={ic};fontSize=12;html=1;"
            ).format(ic=icolor)
            out.write('        <mxCell id="{rid}_c0" value="{ic}" style="{st}" vertex="1" parent="{rid}">\n'
                      .format(rid=rid, ic=esc(icon), st=cell_icon))
            out.write('          <mxGeometry width="28" height="{rh}" as="geometry">'
                      '<mxRectangle width="28" height="{rh}" as="alternateBounds" /></mxGeometry>\n'
                      .format(rh=ROW_H))
            out.write('        </mxCell>\n')

            fstyle = 4 if kind == "pk" else 0  # PK подчёркнут
            cell_txt = (
                "shape=partialRectangle;overflow=hidden;connectable=0;fillColor=none;"
                "top=0;left=0;bottom=0;right=0;align=left;spacingLeft=6;fontColor=#23445D;"
                "fontStyle={fs};fontFamily={f};fontSize=11;html=1;"
            ).format(fs=fstyle, f=FONT)
            out.write('        <mxCell id="{rid}_c1" value="{tx}" style="{st}" vertex="1" parent="{rid}">\n'
                      .format(rid=rid, tx=esc(text), st=cell_txt))
            out.write('          <mxGeometry x="28" width="{cw}" height="{rh}" as="geometry">'
                      '<mxRectangle width="{cw}" height="{rh}" as="alternateBounds" /></mxGeometry>\n'
                      .format(cw=w - 28, rh=ROW_H))
            out.write('        </mxCell>\n')

    for src, dst, label in EDGES:
        estyle = (
            "edgeStyle=entityRelationEdgeStyle;fontSize=10;fontFamily={f};html=1;"
            "endArrow=ERmany;startArrow=ERone;rounded=0;curved=0;"
        ).format(f=FONT)
        eid = "e_{}_{}".format(src, dst)
        out.write('        <mxCell id="{eid}" value="{lb}" style="{st}" edge="1" parent="1" '
                  'source="{s}" target="{d}">\n'
                  .format(eid=eid, lb=label, st=estyle, s=src, d=dst))
        out.write('          <mxGeometry relative="1" as="geometry" />\n')
        out.write('        </mxCell>\n')

    out.write('        <mxCell id="caption" value="Рисунок 2.2 — ER-диаграмма базы данных клиента FitApp" '
              'style="text;html=1;fontSize=12;fontStyle=2;fontFamily=Times New Roman;align=center;" '
              'vertex="1" parent="1">\n')
    out.write('          <mxGeometry x="40" y="930" width="1320" height="26" as="geometry" />\n')
    out.write('        </mxCell>\n')

    out.write('      </root>\n')
    out.write('    </mxGraphModel>\n')
    out.write('  </diagram>\n')
    out.write('</mxfile>\n')

    with io.open("diagram_er_workbench.drawio", "w", encoding="utf-8") as f:
        f.write(out.getvalue())
    print("written diagram_er_workbench.drawio")


if __name__ == "__main__":
    main()

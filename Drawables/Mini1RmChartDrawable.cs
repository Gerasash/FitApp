using Microsoft.Maui.Graphics;

namespace FitApp.Drawables;

/// <summary>
/// Спарклайн: лёгкий мини-график серии 1ПМ для карточки на главном
/// экране. Без осей, подписей и сетки — это «настроение тренда», а не
/// полноценный график. Полный анализ остаётся за страницей «Прогресс».
///
/// Реализация на Microsoft.Maui.Graphics (IDrawable) — рендер идёт
/// через GraphicsView, кросс-платформенно и без дополнительных
/// зависимостей сверх того, что уже есть в проекте (SkiaSharp
/// используется для тяжёлой страницы Progress, тут не нужен).
/// </summary>
public class Mini1RmChartDrawable : IDrawable
{
    public IReadOnlyList<double> Values { get; init; } = Array.Empty<double>();
    public Color LineColor { get; init; } = Colors.SteelBlue;
    public Color FillColor { get; init; } = Color.FromRgba(70, 130, 180, 60);
    public Color DotColor { get; init; } = Colors.White;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Values.Count < 2) return;

        const float pad = 10f;
        var innerW = dirtyRect.Width - pad * 2;
        var innerH = dirtyRect.Height - pad * 2;
        if (innerW <= 0 || innerH <= 0) return;

        double min = Values.Min();
        double max = Values.Max();
        // Защита от «плоской» серии: range == 0 → делим на ноль. Раздуваем
        // диапазон на единицу килограмм, тренд визуально лежит на средней.
        if (Math.Abs(max - min) < 0.01) { min -= 1; max += 1; }
        double range = max - min;

        var pts = new PointF[Values.Count];
        for (int i = 0; i < Values.Count; i++)
        {
            float x = pad + (float)i / (Values.Count - 1) * innerW;
            float y = pad + (float)((1 - (Values[i] - min) / range) * innerH);
            pts[i] = new PointF(x, y);
        }

        // Заливка под линией — мягкое подкрашивание тренда.
        var fill = new PathF();
        fill.MoveTo(pts[0].X, pad + innerH);
        foreach (var p in pts) fill.LineTo(p);
        fill.LineTo(pts[^1].X, pad + innerH);
        fill.Close();
        canvas.FillColor = FillColor;
        canvas.FillPath(fill);

        // Линия тренда.
        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 2.5f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        var line = new PathF();
        line.MoveTo(pts[0]);
        for (int i = 1; i < pts.Length; i++) line.LineTo(pts[i]);
        canvas.DrawPath(line);

        // Точка последней тренировки — акцент на «где мы сейчас».
        var last = pts[^1];
        canvas.FillColor = LineColor;
        canvas.FillCircle(last.X, last.Y, 4.5f);
        canvas.FillColor = DotColor;
        canvas.FillCircle(last.X, last.Y, 2.0f);
    }
}

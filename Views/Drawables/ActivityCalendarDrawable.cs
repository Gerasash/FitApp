using FitApp.ViewModels;

namespace FitApp.Views.Drawables;

// Рисует календарь активности на 12 недель:
//  - слева подписи дней недели (Пн..Вс),
//  - сверху подписи месяцев (когда меняется),
//  - сетка ячеек: цвет = интенсивность тоннажа, число дня — если тренировка была.
public class ActivityCalendarDrawable : IDrawable
{
    public IReadOnlyList<HeatmapCell> Cells { get; set; } = Array.Empty<HeatmapCell>();
    public bool IsDarkTheme { get; set; }

    // Используется кодом-бихайндом, чтобы сопоставить точку тапа с ячейкой
    public RectF[] CellRects { get; private set; } = Array.Empty<RectF>();

    private const int Weeks = 12;
    private const int Days = 7;
    private const float Gap = 4f;
    private const float LeftPad = 26f;   // место под "Пн/Ср/Пт"
    private const float TopPad = 16f;    // место под месяцы

    private static readonly string[] WeekdayLabels = { "Пн", "", "Ср", "", "Пт", "", "Вс" };
    private static readonly string[] MonthsRu =
    {
        "янв", "фев", "мар", "апр", "май", "июн",
        "июл", "авг", "сен", "окт", "ноя", "дек"
    };

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Cells.Count == 0) return;

        var gridW = dirtyRect.Width - LeftPad;
        var gridH = dirtyRect.Height - TopPad;

        var cellW = (gridW - Gap * (Weeks - 1)) / Weeks;
        var cellH = (gridH - Gap * (Days - 1)) / Days;
        var cellSize = Math.Max(8, Math.Min(cellW, cellH));

        var textColor = IsDarkTheme ? Color.FromArgb("#9E9E9E") : Color.FromArgb("#616161");
        var labelFontSize = 10f;
        canvas.FontSize = labelFontSize;
        canvas.FontColor = textColor;

        // Подписи дней недели слева
        for (int r = 0; r < Days; r++)
        {
            if (string.IsNullOrEmpty(WeekdayLabels[r])) continue;
            float y = TopPad + r * (cellSize + Gap);
            canvas.DrawString(WeekdayLabels[r], 0, y + cellSize / 2 - 6,
                LeftPad - 4, 12, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        // Сетка ячеек + подписи месяцев
        var rects = new RectF[Cells.Count];
        int prevMonth = -1;
        for (int i = 0; i < Cells.Count; i++)
        {
            int col = i / Days;
            int row = i % Days;
            float x = LeftPad + col * (cellSize + Gap);
            float y = TopPad + row * (cellSize + Gap);

            var cell = Cells[i];
            rects[i] = new RectF(x, y, cellSize, cellSize);

            // Подпись месяца, когда в верхней строке столбца меняется месяц
            if (row == 0)
            {
                int m = cell.Date.Month;
                if (m != prevMonth)
                {
                    canvas.FontColor = textColor;
                    canvas.DrawString(MonthsRu[m - 1], x, 0, cellSize + 12, TopPad - 2,
                        HorizontalAlignment.Left, VerticalAlignment.Top);
                    prevMonth = m;
                }
            }

            // Заливка
            canvas.FillColor = cell.CellColor;
            canvas.FillRoundedRectangle(x, y, cellSize, cellSize, 3);

            // Обводка сегодняшнего дня
            if (cell.Date.Date == DateTime.Now.Date)
            {
                canvas.StrokeColor = Color.FromArgb("#FF5722");
                canvas.StrokeSize = 2f;
                canvas.DrawRoundedRectangle(x, y, cellSize, cellSize, 3);
            }

            // Число дня — если была активность и ячейка достаточно крупная
            if (cell.Tonnage > 0 && cellSize >= 18)
            {
                canvas.FontColor = cell.Level >= 3 ? Colors.White : Color.FromArgb("#1B5E20");
                canvas.FontSize = Math.Min(11, cellSize * 0.55f);
                canvas.DrawString(cell.Date.Day.ToString(), x, y, cellSize, cellSize,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
                canvas.FontSize = labelFontSize;
            }
        }

        CellRects = rects;
    }
}

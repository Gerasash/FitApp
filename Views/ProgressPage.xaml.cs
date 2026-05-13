using FitApp.Data;
using FitApp.ViewModels;
using FitApp.Views.Drawables;
using System.ComponentModel;

namespace FitApp.Views;

public partial class ProgressPage : ContentPage
{
    private readonly ProgressViewModel _viewModel;
    private readonly ActivityCalendarDrawable _calendarDrawable = new();

    public ProgressPage(WorkoutDataBase database)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[ProgressPage] InitializeComponent FAILED:");
            for (var e = (Exception?)ex; e != null; e = e.InnerException)
                System.Diagnostics.Debug.WriteLine($"  -> {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            throw;
        }

        _viewModel = new ProgressViewModel(database);
        BindingContext = _viewModel;

        ActivityCalendar.Drawable = _calendarDrawable;

        // Перерисовываем календарь при изменении данных или темы
        _viewModel.PropertyChanged += OnVmPropertyChanged;
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += (_, __) =>
            {
                _calendarDrawable.IsDarkTheme = Application.Current.RequestedTheme == AppTheme.Dark;
                ActivityCalendar.Invalidate();
            };
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProgressViewModel.HeatmapCells))
        {
            _calendarDrawable.Cells = _viewModel.HeatmapCells.ToList();
            _calendarDrawable.IsDarkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;
            ActivityCalendar.Invalidate();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // По умолчанию сразу показываем Обзор (агрегаты + heatmap).
        // Список упражнений подтягивается лениво при переключении вкладки.
        if (_viewModel.IsOverviewMode)
            await _viewModel.LoadOverviewAsync();
        else
            await _viewModel.LoadExercises();
    }

    private async void OnCalendarTapped(object sender, TappedEventArgs e)
    {
        var pos = e.GetPosition(ActivityCalendar);
        if (pos == null) return;
        var rects = _calendarDrawable.CellRects;
        var cells = _calendarDrawable.Cells;
        for (int i = 0; i < rects.Length && i < cells.Count; i++)
        {
            if (rects[i].Contains((float)pos.Value.X, (float)pos.Value.Y))
            {
                var c = cells[i];
                string msg = c.Tonnage > 0
                    ? $"{c.Date:dd MMMM yyyy}\nТоннаж: {Math.Round(c.Tonnage)} кг"
                    : $"{c.Date:dd MMMM yyyy}\nТренировок не было";
                await DisplayAlert("День", msg, "OK");
                return;
            }
        }
    }
}

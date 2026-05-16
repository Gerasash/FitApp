namespace FitApp.Views;
using Microsoft.Maui.Controls;
using SQLite;
using FitApp.Models;
using FitApp.ViewModels;
using System;
using System.ComponentModel;
using static System.Net.Mime.MediaTypeNames;
using FitApp.Data;
using FitApp.Services;

public partial class WorkoutPage : ContentPage
{
    private WorkoutViewModel _viewModel;
    private readonly WorkoutDataBase _database;
    private readonly AiService _aiService;

    public WorkoutPage(WorkoutDataBase database, AiService aiService)
    {
        InitializeComponent();
        _database = database;
        _aiService = aiService;
    }

    public void Init(Workout workout)
    {
        _viewModel = new WorkoutViewModel(workout, _database, _aiService);
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnVmPropertyChanged;
        Appearing += async (s, e) => await _viewModel.LoadExercisesForWorkout(workout.Id);
    }

    private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkoutViewModel.IsSetSheetOpen)) return;
        if (_viewModel.IsSetSheetOpen) await OpenSheet();
        else await CloseSheet();
    }

    private async Task OpenSheet()
    {
        // Перед показом ставим вне экрана
        var h = SetSheet.Height > 0 ? SetSheet.Height : 480;
        SetSheet.TranslationY = h;
        SetSheet.IsVisible = true;
        Backdrop.IsVisible = true;
        Backdrop.InputTransparent = false;

        // Параллельно: фон затемняем, sheet выезжает
        var fade = Backdrop.FadeTo(0.45, 200, Easing.CubicOut);
        var slide = SetSheet.TranslateTo(0, 0, 240, Easing.CubicOut);
        await Task.WhenAll(fade, slide);
    }

    private async Task CloseSheet()
    {
        var h = SetSheet.Height > 0 ? SetSheet.Height : 480;
        Backdrop.InputTransparent = true;
        var fade = Backdrop.FadeTo(0, 180, Easing.CubicIn);
        var slide = SetSheet.TranslateTo(0, h, 200, Easing.CubicIn);
        await Task.WhenAll(fade, slide);
        SetSheet.IsVisible = false;
        Backdrop.IsVisible = false;
    }

    private async void ToModalPage(object? sender, EventArgs e)
    {
        var modalPage = new ExercisePage(_database);
        modalPage.SetCallback(async exercise =>
        {
            await _viewModel.AddExerciseToWorkout(exercise);
            if (Navigation.ModalStack.Count > 0)
                await Navigation.PopModalAsync();
        });
        await Navigation.PushModalAsync(modalPage);
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSaveAsTemplateClicked(object sender, EventArgs e)
    {
        if (_viewModel?.CurrentWorkout == null) return;
        if (_viewModel.WorkoutExercises == null || _viewModel.WorkoutExercises.Count == 0)
        {
            await DisplayAlert("Шаблон", "В тренировке нет упражнений — нечего сохранять.", "OK");
            return;
        }

        var name = await DisplayPromptAsync("Сохранить как шаблон",
            "Название шаблона:", "Сохранить", "Отмена",
            initialValue: _viewModel.CurrentWorkout.Name);
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            await _database.CreateTemplateFromWorkoutAsync(_viewModel.CurrentWorkout.Id, name);
            await DisplayAlert("Готово", $"Шаблон «{name}» создан. Найдёшь его на вкладке «Шаблоны».", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
}

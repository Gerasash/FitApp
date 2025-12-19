
using FitApp.Data;
using FitApp.Models;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;
namespace FitApp;

public partial class WorkoutPage : ContentPage
{
    private readonly WorkoutViewModel _viewModel;
    private readonly WorkoutDataBase _database;

    public WorkoutPage(Workout workout, WorkoutDataBase database)
    {
        InitializeComponent();
        _database = database;

        // Создаём ViewModel с выбранной тренировкой
        _viewModel = new WorkoutViewModel(workout, database);
        BindingContext = _viewModel;
    }

    // Обработчик сохранения тренировки
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Вызываем команду обновления из ViewModel
            if (_viewModel.UpdateWorkoutCommand.CanExecute(null))
            {
                _viewModel.UpdateWorkoutCommand.Execute(null);
            }

            // Возвращаемся на предыдущую страницу
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось сохранить: {ex.Message}", "OK");
        }
    }

    // Обработчик отмены
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // Обработчик удаления тренировки
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        bool confirmed = await DisplayAlert(
            "Подтверждение",
            "Вы уверены, что хотите удалить эту тренировку?",
            "Да",
            "Нет"
        );

        if (confirmed && _viewModel.CurrentWorkout != null)
        {
            try
            {
                if (_viewModel.DeleteWorkoutCommand.CanExecute(_viewModel.CurrentWorkout))
                {
                    _viewModel.DeleteWorkoutCommand.Execute(_viewModel.CurrentWorkout);
                }

                // Возвращаемся на список тренировок
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось удалить: {ex.Message}", "OK");
            }
        }
    }
}
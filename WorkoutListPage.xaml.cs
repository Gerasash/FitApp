namespace FitApp;

using FitApp.Models;
using FitApp.ViewModels;
public partial class WorkoutListPage : ContentPage
{
	public WorkoutListPage()
	{
        InitializeComponent();
        BindingContext = new WorkoutViewModel();
        // Привязка данных к ListView
        //WorkoutListView.ItemsSource = Workouts;
        // Добавляем обработчик события выбора тренировки
        //WorkoutListView.ItemSelected += OnWorkoutSelected;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }
    private async void OnWorkoutSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedWorkout = e.CurrentSelection.FirstOrDefault() as Workout;
        if (selectedWorkout == null)
            return;

        // Сбросим выделение, чтобы можно было повторно выбрать тот же элемент
        ((CollectionView)sender).SelectedItem = null;

        // Переход на новую страницу, передавая выбранную тренировку
        await Navigation.PushAsync(new WorkoutPage(selectedWorkout));
    }
    
}
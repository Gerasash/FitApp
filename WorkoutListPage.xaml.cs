namespace FitApp;
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
}
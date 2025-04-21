namespace FitApp;

using FitApp.Models;
using FitApp.ViewModels;
public partial class WorkoutListPage : ContentPage
{
	public WorkoutListPage()
	{
        InitializeComponent();
        BindingContext = new WorkoutViewModel();
        // �������� ������ � ListView
        //WorkoutListView.ItemsSource = Workouts;
        // ��������� ���������� ������� ������ ����������
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

        // ������� ���������, ����� ����� ���� �������� ������� ��� �� �������
        ((CollectionView)sender).SelectedItem = null;

        // ������� �� ����� ��������, ��������� ��������� ����������
        await Navigation.PushAsync(new WorkoutPage(selectedWorkout));
    }
    
}
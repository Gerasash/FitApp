namespace FitApp;
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
}
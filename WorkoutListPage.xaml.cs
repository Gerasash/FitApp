namespace FitApp;
using FitApp.Models;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;

public partial class WorkoutListPage : ContentPage
{
    public WorkoutListPage()
    {
        InitializeComponent();
        BindingContext = new WorkoutViewModel();
    }

    private async void OnWorkoutSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedWorkout = e.CurrentSelection.FirstOrDefault() as Workout;
        if (selectedWorkout == null)
            return;

        var workoutViewModel = new WorkoutViewModel(); // ������� ����� ViewModel
        // �������� ��������� ���������� �� ViewModel ��� �������������
        // (��������������, ��� � WorkoutViewModel ���� �����������, ����������� Workout)
        var workoutPage = new WorkoutPage(selectedWorkout, workoutViewModel);
        await Navigation.PushAsync(workoutPage);
        ((CollectionView)sender).SelectedItem = null;
    }
    
}
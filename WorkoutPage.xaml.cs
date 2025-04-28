namespace FitApp;
using Microsoft.Maui.Controls;
using SQLite;
using FitApp.Models;
using FitApp.ViewModels;
public partial class WorkoutPage : ContentPage
{
    public WorkoutPage(Workout workout)
    {
        InitializeComponent();
        BindingContext = new WorkoutViewModel();
        // ������������� ������ ���������� �� ��������
        WorkoutNameLabel.Text = workout.Name;
        //EditorWorkoutDescription.Text = workout.Description;

        

        WorkoutDescriptionLabel.Text = $"������: {workout.StartTime}";

        // ������ "�����"
        backButton.Clicked += async (o, e) => await Navigation.PopAsync();
        // ������ "�������� ����������"
        addExerciseButton.Clicked += ToModalPage;
    }
    private async void ToModalPage(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new AddExersciseModalPage());
    }
    void PickerSelectedIndexChanged(object sender, EventArgs e)
    {
        WorkoutDescriptionLabel.Text = $"�� �������: {WorkoutPicker.SelectedItem}";

    }

    private void EditorWorkoutDescription_TextChanged(object sender, TextChangedEventArgs e)
    {
        //SaveDescriptionAsync(sender, e.NewTextValue);
    }
}
// файл MuscleGroupsListPage.xaml.cs
namespace FitApp;
using FitApp.Data;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;
public partial class MuscleGroupsListPage : ContentPage
{
    private readonly WorkoutDataBase _database;

    public MuscleGroupsListPage(WorkoutDataBase database)
    {
        InitializeComponent();
        BindingContext = new MuscleGroupViewModel(database);
    }
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
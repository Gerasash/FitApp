// פאיכ MuscleGroupsListPage.xaml.cs
namespace FitApp;
using FitApp.Data;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;
public partial class MuscleGroupsListPage : ContentPage
{
    private readonly WorkoutDataBase _database;

    public MuscleGroupsListPage()
    {
        InitializeComponent();

        _database = new WorkoutDataBase();
        BindingContext = new MuscleGroupViewModel(_database);
    }
}
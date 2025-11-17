// פאיכ MuscleGroupsListPage.xaml.cs
namespace FitApp;
using FitApp.Data;
using FitApp.ViewModels;
using Microsoft.Maui.Controls;
public partial class MuscleGroupsListPage : ContentPage
{
	public MuscleGroupsListPage(WorkoutDataBase database)
	{
		InitializeComponent();
        BindingContext = new MuscleGroupViewModel(database);
    }
}
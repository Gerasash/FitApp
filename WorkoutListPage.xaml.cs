namespace FitApp;
using FitApp.ViewModels;
public partial class WorkoutListPage : ContentPage
{
	public WorkoutListPage()
	{
        InitializeComponent();
        BindingContext = new WorkoutViewModel();

	}

    private void Button_Clicked(object sender, EventArgs e)
    {

    }
}
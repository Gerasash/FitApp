namespace FitApp;
using FitApp.ViewModels;
public partial class TaskListPage : ContentPage
{
	public TaskListPage()
	{
		InitializeComponent();
        BindingContext = new MainViewModel();
    }

    private async void GoToBack_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
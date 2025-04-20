namespace FitApp;

using FitApp.Models;
using FitApp.ViewModels;
using System.Collections.ObjectModel;

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
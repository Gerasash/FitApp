namespace FitApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MuscleGroupsListPage), typeof(MuscleGroupsListPage));
            Routing.RegisterRoute(nameof(ExercisePage), typeof(ExercisePage));
            Routing.RegisterRoute(nameof(WorkoutListPage), typeof(WorkoutListPage));
        }
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//SettingsPage");
        }

    }
}

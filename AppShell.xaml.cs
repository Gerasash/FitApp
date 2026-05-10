using FitApp.Views;

namespace FitApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MuscleGroupsListPage), typeof(MuscleGroupsListPage));
            Routing.RegisterRoute(nameof(ExercisePage), typeof(ExercisePage));
            Routing.RegisterRoute(nameof(WorkoutPage), typeof(WorkoutPage));
        }

    }
}

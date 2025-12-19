using FitApp.Data;
using FitApp.ViewModels;

namespace FitApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Регистрируем сервисы
            var services = new ServiceCollection();
            services.AddSingleton<WorkoutDataBase>();
            services.AddSingleton<WorkoutViewModel>();
            services.AddSingleton<MuscleGroupViewModel>();

            services.AddSingleton<MainPage>();
            services.AddSingleton<WorkoutListPage>();
            services.AddSingleton<MuscleGroupsListPage>();

            MainPage = new AppShell();

            string savedTheme = Preferences.Get("AppTheme", "Light");
            AppTheme theme = savedTheme == "Dark" ? AppTheme.Dark : AppTheme.Light;
            App.Current.UserAppTheme = theme;

            
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
namespace FitApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            string savedTheme = Preferences.Get("AppTheme", "Light");
            AppTheme theme = savedTheme == "Dark" ? AppTheme.Dark : AppTheme.Light;
            App.Current.UserAppTheme = theme;

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            System.Diagnostics.Debug.WriteLine($"Unhandled: {e.ExceptionObject}");

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"Unobserved: {e.Exception}");
                e.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
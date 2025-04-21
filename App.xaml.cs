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

            
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
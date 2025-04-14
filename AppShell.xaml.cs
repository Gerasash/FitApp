namespace FitApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

        }
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//SettingsPage");
        }

    }
}

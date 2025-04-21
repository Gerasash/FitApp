namespace FitApp;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }
    void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            // ¬ключить тЄмную тему
            App.Current.UserAppTheme = AppTheme.Dark;
            Preferences.Set("AppTheme", "Dark");
        }
        else
        {
            // ¬ключить светлую тему
            App.Current.UserAppTheme = AppTheme.Light;
            Preferences.Set("AppTheme", "Light");
        }
        // Perform required operation after examining e.Value
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        string savedTheme = Preferences.Get("AppTheme", "Light");
        ThemeToggleCheckBox.IsChecked = savedTheme == "Dark";
    }
}
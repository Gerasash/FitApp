namespace FitApp.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Тема: 0=Light, 1=Dark, 2=System
        var theme = Preferences.Get("AppTheme", "System");
        ThemePicker.SelectedIndex = theme switch
        {
            "Light" => 0,
            "Dark" => 1,
            _ => 2
        };

        // Единицы: 0=кг, 1=lb
        UnitsPicker.SelectedIndex = Preferences.Get("Units", "kg") == "lb" ? 1 : 0;

        // Таймер
        var rest = Preferences.Get("RestTimer", 90);
        RestTimerPicker.SelectedIndex = rest switch
        {
            60 => 0,
            90 => 1,
            120 => 2,
            180 => 3,
            _ => 1
        };

        SoundSwitch.IsToggled = Preferences.Get("SoundEnabled", true);
        VibrationSwitch.IsToggled = Preferences.Get("VibrationEnabled", true);
    }

    void OnThemeChanged(object sender, EventArgs e)
    {
        if (Application.Current is null) return;

        switch (ThemePicker.SelectedIndex)
        {
            case 0:
                Application.Current.UserAppTheme = AppTheme.Light;
                Preferences.Set("AppTheme", "Light");
                break;
            case 1:
                Application.Current.UserAppTheme = AppTheme.Dark;
                Preferences.Set("AppTheme", "Dark");
                break;
            default:
                Application.Current.UserAppTheme = AppTheme.Unspecified;
                Preferences.Set("AppTheme", "System");
                break;
        }
    }

    void OnUnitsChanged(object sender, EventArgs e)
    {
        Preferences.Set("Units", UnitsPicker.SelectedIndex == 1 ? "lb" : "kg");
    }

    void OnRestTimerChanged(object sender, EventArgs e)
    {
        var seconds = RestTimerPicker.SelectedIndex switch
        {
            0 => 60,
            1 => 90,
            2 => 120,
            3 => 180,
            _ => 90
        };
        Preferences.Set("RestTimer", seconds);
    }

    void OnSoundToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("SoundEnabled", e.Value);
    }

    void OnVibrationToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("VibrationEnabled", e.Value);
    }

    async void OnExportCsv(object sender, EventArgs e)
    {
        await DisplayAlert("Экспорт", "Функция будет добавлена позже", "OK");
    }
}

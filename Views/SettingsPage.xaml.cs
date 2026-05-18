using FitApp.Data;
using FitApp.Models;
using System.Globalization;

namespace FitApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly WorkoutDataBase _database;
    private User? _user;
    // Чтобы загрузка значений в Picker'ы/Entry не триггерила сохранение
    private bool _loadingProfile;

    public SettingsPage(WorkoutDataBase database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // --- Профиль (из БД) ---
        await LoadProfileAsync();

        // --- Прочие настройки (Preferences) ---

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

    private async Task LoadProfileAsync()
    {
        _loadingProfile = true;
        try
        {
            _user = await _database.GetCurrentUserAsync();

            DisplayNameEntry.Text = _user.DisplayName ?? string.Empty;
            BodyweightEntry.Text = _user.Bodyweight > 0
                ? _user.Bodyweight.ToString("0.#", CultureInfo.InvariantCulture)
                : string.Empty;
            AgeEntry.Text = _user.Age > 0 ? _user.Age.ToString() : string.Empty;
            SexPicker.SelectedIndex = _user.Sex is >= 0 and <= 2 ? _user.Sex : 0;
            // DatePicker не любит null — если дата не задана, ставим сегодня и не считаем это "задано"
            ExperienceStartDatePicker.Date = _user.ExperienceStartDate ?? DateTime.Today;

            // RPE: индекс в Picker'е соответствует значениям 6.0..9.0 с шагом 0.5
            var rpeIdx = (int)Math.Round((_user.TargetRpe - 6.0) / 0.5);
            if (rpeIdx < 0) rpeIdx = 0;
            if (rpeIdx > 6) rpeIdx = 6;
            TargetRpePicker.SelectedIndex = rpeIdx;
        }
        finally
        {
            _loadingProfile = false;
        }
    }

    private async void OnProfileChanged(object? sender, EventArgs e)
    {
        if (_loadingProfile || _user == null) return;

        _user.DisplayName = string.IsNullOrWhiteSpace(DisplayNameEntry.Text)
            ? null
            : DisplayNameEntry.Text.Trim();

        _user.Bodyweight = double.TryParse(
            BodyweightEntry.Text?.Replace(',', '.'),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var bw) ? bw : 0;

        _user.Age = int.TryParse(AgeEntry.Text, out var age) ? age : 0;

        _user.Sex = SexPicker.SelectedIndex is >= 0 and <= 2 ? SexPicker.SelectedIndex : 0;

        // Если юзер не двигал DatePicker — оставляем null (мы загрузили сегодня как заглушку).
        // Здесь же сохраняем как реальное значение.
        _user.ExperienceStartDate = ExperienceStartDatePicker.Date;

        var rpeIdx = TargetRpePicker.SelectedIndex;
        if (rpeIdx >= 0)
            _user.TargetRpe = 6.0 + rpeIdx * 0.5;

        try
        {
            await _database.SaveUserAsync(_user);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsPage] SaveUserAsync failed: {ex}");
        }
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

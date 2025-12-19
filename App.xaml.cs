using FitApp.Data;
using FitApp.ViewModels;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace FitApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Устанавливаем тему приложения
        string savedTheme = Preferences.Get("AppTheme", "Light");
        AppTheme theme = savedTheme == "Dark" ? AppTheme.Dark : AppTheme.Light;
        this.UserAppTheme = theme;  // ✅ 'this' вместо 'App.Current'
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
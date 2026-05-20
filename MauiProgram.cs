using FitApp.Data;
using FitApp.Services;
using FitApp.Services.Sync;
using FitApp.ViewModels;
using FitApp.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
namespace FitApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            // LiveCharts: глобальные настройки (тема/палитра)
            LiveCharts.Configure(config =>
                config.AddSkiaSharp()
                      .AddDefaultMappers()
                      .AddLightTheme());

            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseLiveCharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            
            builder.Services.AddSingleton<WorkoutDataBase>();
            builder.Services.AddSingleton<AiService>();
            // Offline ML-предиктор (LightGBM в ONNX, inference на устройстве).
            // Singleton — модель загружается лениво на первом вызове и
            // переиспользуется до конца жизни приложения.
            builder.Services.AddSingleton<OnnxPredictionService>();
            // Модуль планирования следующей тренировки. Использует
            // OnnxPredictionService + историю из БД. Singleton — чистый,
            // не хранит состояния между вызовами.
            builder.Services.AddSingleton<WorkoutPlannerService>();

            // === Этап 6: синхронизация с FitApp.Api ===
            // BaseUrl выносим в константу/конфиг. Сейчас — локальный сервер
            // dev-машины (для эмулятора Android используется 10.0.2.2).
            // После деплоя на Render заменим на https://....onrender.com.
            // Для Windows-десктоп тестов оставляем localhost.
            builder.Services.AddSingleton(sp => new HttpClient
            {
                BaseAddress = new Uri(GetApiBaseUrl()),
                Timeout = TimeSpan.FromSeconds(30)
            });
            builder.Services.AddSingleton<AuthClient>();
            builder.Services.AddSingleton<SyncService>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<WorkoutPage>();
            builder.Services.AddTransient<MuscleGroupsListPage>();
            builder.Services.AddTransient<WorkoutListPage>();
            builder.Services.AddTransient<WorkoutViewModel>();
            builder.Services.AddTransient<MuscleGroupViewModel>();
            builder.Services.AddTransient<ExercisePage>();
            builder.Services.AddTransient<ProgressPage>();
            builder.Services.AddTransient<TemplatesPage>();
            builder.Services.AddTransient<TemplatesViewModel>();
            builder.Services.AddTransient<TemplateEditorPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<AccountPage>();
            builder.Services.AddTransient<AccountViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static string GetApiBaseUrl()
        {
            // На Android-эмуляторе localhost хоста = 10.0.2.2.
#if ANDROID
            return "http://10.0.2.2:5127/";
#else
            return "http://localhost:5127/";
#endif
        }
    }
}

using FitApp.Data;
using FitApp.Services;
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

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

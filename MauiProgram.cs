using FitApp.Data;
using FitApp.Services;
using FitApp.ViewModels;
using FitApp.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
namespace FitApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            
            builder.Services.AddSingleton<WorkoutDataBase>();
            builder.Services.AddSingleton<AiService>();

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

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

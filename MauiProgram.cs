using FitApp.Data;
using Microsoft.Extensions.Logging;
using FitApp.ViewModels;
namespace FitApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            
            builder.Services.AddSingleton<WorkoutDataBase>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<WorkoutPage>();
            builder.Services.AddTransient<MuscleGroupsListPage>();
            builder.Services.AddTransient<WorkoutListPage>();
            builder.Services.AddTransient<WorkoutViewModel>();
            builder.Services.AddTransient<MuscleGroupViewModel>();
            builder.Services.AddTransient<ExercisePage>();
            builder.Services.AddTransient<ProgressPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

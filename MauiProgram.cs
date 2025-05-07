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

            builder.Services.AddSingleton<LocalDBService>();
            builder.Services.AddSingleton<ToDoDataBase>();
            builder.Services.AddSingleton<WorkoutDataBase>();

            builder.Services.AddSingleton<WorkoutPage>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainViewModel>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

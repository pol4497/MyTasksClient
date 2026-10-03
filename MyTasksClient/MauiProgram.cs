using Microsoft.Extensions.Logging;
using MyTasksClient.Features.Tasks;

namespace MyTasksClient;

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

        builder.Services.AddTasksFeature();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
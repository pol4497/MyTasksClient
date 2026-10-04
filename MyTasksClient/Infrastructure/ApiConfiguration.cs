using Microsoft.Maui.Devices;

namespace MyTasksClient.Infrastructure;

internal static class ApiConfiguration
{
#if DEBUG
    // Port of the "http" profile in MyTasks-master/Properties/launchSettings.json.
    private const int DevelopmentPort = 5235;

    public static Uri GetBaseAddress()
    {
        var host = DeviceInfo.Platform == DevicePlatform.Android ? GetAndroidHost() : "localhost";

        // The trailing slash matters: without it, relative paths replace the last URL segment.
        return new Uri($"http://{host}:{DevelopmentPort}/");
    }

    // Emulator: 10.0.2.2 is the alias for the development PC.
    // Physical phone: "localhost" works once `adb reverse tcp:5235 tcp:5235` has been run.
    private static string GetAndroidHost() =>
        DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : "localhost";
#else
    // The address of the deployed API, for example "https://api.example.com". Set before publishing.
    private const string ProductionBaseUrl = "";

    public static Uri GetBaseAddress()
    {
        if (string.IsNullOrWhiteSpace(ProductionBaseUrl))
        {
            throw new InvalidOperationException(
                "Set ApiConfiguration.ProductionBaseUrl before running a Release build.");
        }

        return new Uri(ProductionBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }
#endif
}
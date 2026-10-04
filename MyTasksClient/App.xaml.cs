namespace MyTasksClient;

public partial class App : Application
{
    public App()
    {
        // The web client has a single light design. Pinning it keeps text and backgrounds
        // consistent when the device is set to dark mode.
        UserAppTheme = AppTheme.Light;

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new AppShell()) { Title = "MyTasks" };
}
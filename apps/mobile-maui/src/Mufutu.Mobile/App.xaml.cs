namespace Mufutu.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Created += OnWindowCreated;
        return window;
    }

    private async void OnWindowCreated(object? sender, EventArgs e)
    {
        if (sender is not Window window)
        {
            return;
        }

        window.Created -= OnWindowCreated;
        await Task.Delay(80);

        var services = window.Handler?.MauiContext?.Services;
        if (services == null || MainPage is not AppShell shell)
        {
            return;
        }

        await shell.BootstrapAsync(services);
    }
}

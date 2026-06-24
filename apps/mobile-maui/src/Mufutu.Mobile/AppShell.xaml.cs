using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Services;

namespace Mufutu.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    public async Task BootstrapAsync(IServiceProvider services)
    {
        var session = services.GetRequiredService<IAuthSessionStore>();
        var runtime = services.GetRequiredService<MauiConnectivityMonitor>();

        if (await session.HasSessionAsync())
        {
            await runtime.StartCampoRuntimeAsync();
            await GoToAsync("//campo");
        }
        else
        {
            await GoToAsync("//login");
        }
    }
}

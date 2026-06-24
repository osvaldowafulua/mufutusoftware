using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Services;
using Mufutu.Mobile.Views;

namespace Mufutu.Mobile;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();

        Items.Add(new ShellContent { Title = "Entrar", Content = services.GetRequiredService<LoginPage>(), Route = "login" });
        Items.Add(new ShellContent { Title = "Campo", Content = services.GetRequiredService<CampoHubPage>(), Route = "campo" });
        Items.Add(new ShellContent { Title = "Trabalhos", Content = services.GetRequiredService<OtsPage>(), Route = "ots" });
        Items.Add(new ShellContent { Title = "Avaria", Content = services.GetRequiredService<AvariaPage>(), Route = "avaria" });
        Items.Add(new ShellContent { Title = "Checklist", Content = services.GetRequiredService<ChecklistPage>(), Route = "checklist" });
        Items.Add(new ShellContent { Title = "Notificações", Content = services.GetRequiredService<NotificationsPage>(), Route = "notifications" });
        Items.Add(new ShellContent { Title = "Enviar", Content = services.GetRequiredService<SyncPage>(), Route = "sync" });
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

using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Services;
using Mufutu.Mobile.Views;

namespace Mufutu.Mobile;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();

        // Páginas lazy (ContentTemplate): nada é construído no arranque — cada página
        // só é resolvida na primeira navegação. Evita que um erro num construtor
        // derrube a app na splash e acelera o arranque a frio.
        Items.Add(new ShellContent { Title = "Entrar", ContentTemplate = new DataTemplate(() => services.GetRequiredService<LoginPage>()), Route = "login" });
        Items.Add(new ShellContent { Title = "Campo", ContentTemplate = new DataTemplate(() => services.GetRequiredService<CampoHubPage>()), Route = "campo" });
        Items.Add(new ShellContent { Title = "Trabalhos", ContentTemplate = new DataTemplate(() => services.GetRequiredService<OtsPage>()), Route = "ots" });
        Items.Add(new ShellContent { Title = "Avaria", ContentTemplate = new DataTemplate(() => services.GetRequiredService<AvariaPage>()), Route = "avaria" });
        Items.Add(new ShellContent { Title = "Checklist", ContentTemplate = new DataTemplate(() => services.GetRequiredService<ChecklistPage>()), Route = "checklist" });
        Items.Add(new ShellContent { Title = "Notificações", ContentTemplate = new DataTemplate(() => services.GetRequiredService<NotificationsPage>()), Route = "notifications" });
        Items.Add(new ShellContent { Title = "Enviar", ContentTemplate = new DataTemplate(() => services.GetRequiredService<SyncPage>()), Route = "sync" });
    }

    public async Task BootstrapAsync(IServiceProvider services)
    {
        try
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
        catch (Exception ex)
        {
            CrashLog.Write("Bootstrap", ex);
            System.Diagnostics.Debug.WriteLine($"Bootstrap failed: {ex}");
            try
            {
                await GoToAsync("//login");
            }
            catch
            {
                // Shell not ready — first route may already be visible
            }
        }

        await ReportPreviousCrashAsync();
    }

    private async Task ReportPreviousCrashAsync()
    {
        try
        {
            var lastCrash = CrashLog.ReadLast();
            if (string.IsNullOrWhiteSpace(lastCrash))
            {
                return;
            }

            var excerpt = lastCrash.Length > 1200 ? lastCrash[..1200] + "…" : lastCrash;
            await DisplayAlert("Erro na sessão anterior", excerpt, "OK");
            CrashLog.Clear();
        }
        catch
        {
            // diagnóstico opcional — nunca bloquear o arranque
        }
    }
}

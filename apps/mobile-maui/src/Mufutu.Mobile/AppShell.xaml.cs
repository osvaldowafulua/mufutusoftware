using Microsoft.Maui.ApplicationModel;
using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Core.Updates;
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
        Items.Add(new ShellContent { Title = "Actualizar", ContentTemplate = new DataTemplate(() => services.GetRequiredService<UpdateRequiredPage>()), Route = "update-required" });
    }

    public async Task BootstrapAsync(IServiceProvider services)
    {
        try
        {
            if (await IsBlockedByUpdateGateAsync(services))
            {
                return;
            }

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

    /// <summary>
    /// Bloqueia o arranque só quando confirma positivamente que a versão instalada
    /// está abaixo da mínima aceite. Sem rede/manifesto indisponível, devolve false
    /// e o arranque offline continua normal — nunca prende um técnico sem sinal.
    /// </summary>
    private async Task<bool> IsBlockedByUpdateGateAsync(IServiceProvider services)
    {
        try
        {
            var gate = services.GetRequiredService<IUpdateGateService>();
            var current = AppInfo.Current.VersionString;
            var result = await gate.CheckAsync(current);

            if (result.Status != UpdateGateStatus.UpdateRequired)
            {
                return false;
            }

            var query = new Dictionary<string, object>
            {
                ["current"] = current,
                ["minimum"] = result.MinimumVersion ?? "—",
                ["latest"] = result.LatestVersion ?? result.MinimumVersion ?? "—",
                ["url"] = Uri.EscapeDataString(result.DownloadUrl ?? string.Empty),
                ["notes"] = Uri.EscapeDataString(result.Notes ?? string.Empty),
            };
            await GoToAsync("//update-required", query);
            return true;
        }
        catch (Exception ex)
        {
            CrashLog.Write("UpdateGate", ex);
            return false;
        }
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

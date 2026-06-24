using System.Diagnostics;
using System.Windows;
using Mufutu.Desktop.Core.Updates;
using Microsoft.Extensions.Logging;

namespace Mufutu.Desktop.Updates;

public static class DesktopUpdateUi
{
    public static async Task CheckOnStartupAsync(
        IDesktopUpdateService updateService,
        ILogger logger,
        bool silent = true)
    {
        try
        {
            var result = await updateService.CheckForUpdatesAsync();
            await Application.Current.Dispatcher.InvokeAsync(() =>
                HandleCheckResult(updateService, logger, result, silent));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Verificação automática de actualizações falhou");
        }
    }

    public static async Task CheckManuallyAsync(IDesktopUpdateService updateService, ILogger logger)
    {
        var result = await updateService.CheckForUpdatesAsync();
        await Application.Current.Dispatcher.InvokeAsync(() =>
            HandleCheckResult(updateService, logger, result, silent: false));
    }

    private static void HandleCheckResult(
        IDesktopUpdateService updateService,
        ILogger logger,
        DesktopUpdateCheckResponse result,
        bool silent)
    {
        switch (result.Result)
        {
            case DesktopUpdateCheckResult.UpToDate:
                if (!silent)
                {
                    MessageBox.Show(
                        $"Está na versão mais recente ({updateService.CurrentVersion}).",
                        "MUFUTU",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                break;

            case DesktopUpdateCheckResult.NoReleaseFound:
                if (!silent)
                {
                    MessageBox.Show(
                        "Nenhuma release Windows encontrada no GitHub.",
                        "MUFUTU",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                break;

            case DesktopUpdateCheckResult.Error:
                if (!silent)
                {
                    MessageBox.Show(
                        result.ErrorMessage ?? "Não foi possível verificar actualizações.",
                        "Actualização",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                break;

            case DesktopUpdateCheckResult.UpdateAvailable when result.Update is not null:
                PromptInstall(updateService, logger, result.Update);
                break;
        }
    }

    private static void PromptInstall(
        IDesktopUpdateService updateService,
        ILogger logger,
        DesktopUpdateInfo update)
    {
        var answer = MessageBox.Show(
            $"Versão {update.Version} disponível.\n\nDeseja descarregar e instalar agora?",
            "Actualização disponível",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        _ = DownloadAndInstallAsync(updateService, logger, update);
    }

    private static async Task DownloadAndInstallAsync(
        IDesktopUpdateService updateService,
        ILogger logger,
        DesktopUpdateInfo update)
    {
        try
        {
            MessageBox.Show(
                "A descarregar…\n\nO instalador abrirá quando o download terminar.",
                "MUFUTU",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            var ok = await updateService.DownloadAndInstallAsync(update);
            if (!ok)
            {
                MessageBox.Show(
                    "Não foi possível iniciar o instalador.",
                    "Actualização",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                "O instalador foi iniciado. Conclua a instalação e reabra o MUFUTU.",
                "Reiniciar para instalar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Download/instalação de actualização falhou");
            MessageBox.Show(
                ex.Message,
                "Erro ao actualizar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public static void OpenReleasesPage(IDesktopUpdateService updateService)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = updateService.ReleasesPageUrl,
            UseShellExecute = true,
        });
    }
}

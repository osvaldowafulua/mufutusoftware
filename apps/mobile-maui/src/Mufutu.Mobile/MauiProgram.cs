using Mufutu.Mobile.Core;
using Mufutu.Mobile.Core.Notifications;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Services;
using Mufutu.Mobile.ViewModels;
using Mufutu.Mobile.Views;
using Plugin.LocalNotification;

namespace Mufutu.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseLocalNotification();

        builder.Services.AddSingleton<IDatabasePathProvider, MauiDatabasePathProvider>();
        builder.Services.AddSingleton<MauiConnectivityMonitor>();
        builder.Services.AddSingleton<IConnectivityMonitor>(sp => sp.GetRequiredService<MauiConnectivityMonitor>());
        builder.Services.AddSingleton<Core.Connectivity.INetworkStatusProvider>(sp => sp.GetRequiredService<MauiConnectivityMonitor>());

        builder.Services.AddSingleton<INotificationPresenter, MauiLocalNotificationPresenter>();
        builder.Services.AddSingleton<ICampoNotificationService>(sp => new CampoNotificationService(
            sp.GetRequiredService<Core.Api.MufutuApiClient>(),
            sp.GetRequiredService<ICampoOfflineStore>(),
            sp.GetRequiredService<IAuthSessionStore>(),
            sp.GetRequiredService<Core.Connectivity.INetworkStatusProvider>(),
            sp.GetRequiredService<INotificationPresenter>()));

        builder.Services.AddMufutuMobileCore(options =>
        {
#if DEBUG
            var debugApi = Environment.GetEnvironmentVariable("MUFUTU_API_URL");
            if (!string.IsNullOrWhiteSpace(debugApi))
            {
                options.ApiBaseUrl = debugApi;
            }
#endif
        });

        builder.Services.AddSingleton<ConnectivityViewModel>();
        builder.Services.AddTransient<ConnectivityPage>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<CampoHubViewModel>();
        builder.Services.AddTransient<CampoHubPage>();
        builder.Services.AddTransient<OtsViewModel>();
        builder.Services.AddTransient<OtsPage>();
        builder.Services.AddTransient<AvariaViewModel>();
        builder.Services.AddTransient<AvariaPage>();
        builder.Services.AddTransient<ChecklistViewModel>();
        builder.Services.AddTransient<ChecklistPage>();
        builder.Services.AddTransient<SyncViewModel>();
        builder.Services.AddTransient<SyncPage>();
        builder.Services.AddTransient<NotificationsViewModel>();
        builder.Services.AddTransient<NotificationsPage>();

        builder.Logging.AddDebug();

        return builder.Build();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Mufutu.Mobile.Core.Api;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Connectivity;
using Mufutu.Mobile.Core.Notifications;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Updates;

namespace Mufutu.Mobile.Core;

public static class MobileCoreServiceCollectionExtensions
{
    public static IServiceCollection AddMufutuMobileCore(
        this IServiceCollection services,
        Action<MobileClientOptions>? configure = null)
    {
        services.AddOptions<MobileClientOptions>();
        services.AddSingleton<IApiSettings>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MobileClientOptions>>().Value;
            return new ApiSettings
            {
                ApiBaseUrl = opts.ApiBaseUrl,
                SiteCode = opts.SiteCode,
            };
        });
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddHttpClient<ConnectivityProbeService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MUFUTU-Mobile/1.0");
        });

        services.AddHttpClient<MufutuApiClient>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MUFUTU-Mobile/1.0");
            client.Timeout = TimeSpan.FromSeconds(45);
        });

        services.AddSingleton<ICampoOfflineStore, CampoOfflineStore>();
        services.AddSingleton<ICampoSyncEngine, CampoSyncEngine>();
        services.AddSingleton<ICampoDataService, CampoDataService>();

        services.AddHttpClient<IUpdateGateService, UpdateGateService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MUFUTU-Mobile/1.0");
        });

        return services;
    }
}

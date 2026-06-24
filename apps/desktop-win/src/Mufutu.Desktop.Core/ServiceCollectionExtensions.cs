using Mufutu.Desktop.Core.Api;
using Mufutu.Desktop.Core.Configuration;
using Mufutu.Desktop.Core.Crypto;
using Mufutu.Desktop.Core.Security;
using Mufutu.Desktop.Core.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mufutu.Desktop.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMufutuDesktopCore(
        this IServiceCollection services,
        Action<MufutuClientOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<MufutuClientOptions>(_ => { });
        }

        services.AddSingleton<IProtectedDataStore, ProtectedKeyStore>();
        services.AddSingleton<SecureStorageService>();
        services.AddSingleton<ISecureStorageService>(sp => sp.GetRequiredService<SecureStorageService>());
        services.AddSingleton<ITokenVault>(sp =>
            new EncryptedFileTokenVault(
                () => sp.GetRequiredService<SecureStorageService>().GetOrCreateMasterKey()));

        services.AddTransient<MufutuApiLoggingHandler>();

        services.AddHttpClient<IMufutuApiClient, MufutuApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MufutuClientOptions>>().Value;
            var baseUrl = options.ApiBaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("X-Site-Id", options.SiteCode);
            client.Timeout = TimeSpan.FromSeconds(60);

            var logger = sp.GetRequiredService<ILogger<MufutuApiClient>>();
            logger.LogInformation("HttpClient → {BaseUrl} (proxy sistema)", baseUrl);
        })
        .AddHttpMessageHandler<MufutuApiLoggingHandler>()
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MufutuClientOptions>>().Value;
            return MufutuHttpClientFactory.CreateHandler(options);
        });

        services.AddHttpClient<IDesktopUpdateService, GitHubDesktopUpdateService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MUFUTU-Desktop-Windows");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        });

        return services;
    }
}

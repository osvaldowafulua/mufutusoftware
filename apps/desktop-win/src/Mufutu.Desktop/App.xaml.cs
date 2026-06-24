using System.IO;
using System.Windows;
using Mufutu.Desktop.Core;
using Mufutu.Desktop.Core.Configuration;
using Mufutu.Desktop.Core.Security;
using Mufutu.Desktop.Core.Updates;
using Mufutu.Desktop.Security;
using Mufutu.Desktop.Updates;
using Mufutu.Desktop.ViewModels;
using Mufutu.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mufutu.Desktop;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MUFUTU");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "desktop.log");

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddDebug();
                logging.AddProvider(new FileLoggerProvider(logPath));
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices(services =>
            {
                services.AddMufutuDesktopCore(options =>
                {
                    var envUrl = Environment.GetEnvironmentVariable("MUFUTU_API_URL");
                    options.ApiBaseUrl = envUrl ?? ResolveDefaultApiUrl();
                    options.SiteCode = Environment.GetEnvironmentVariable("MUFUTU_SITE_CODE") ?? "MUA";
                    options.PinnedHostSuffixes = ["mufutu.ao", "localhost", "127.0.0.1"];
                });

                if (OperatingSystem.IsWindows())
                {
                    services.AddSingleton<ITokenVault, WindowsCredentialTokenVault>();
                }

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<LoginViewModel>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<WorkOrdersViewModel>();
                services.AddSingleton<AssetsViewModel>();
                services.AddSingleton<LoginWindow>();
                services.AddSingleton<ShellWindow>();
            })
            .Build();

        await _host.StartAsync();

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(6));
            var updateService = _host.Services.GetRequiredService<IDesktopUpdateService>();
            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            await DesktopUpdateUi.CheckOnStartupAsync(updateService, logger, silent: true);
        });

        var login = _host.Services.GetRequiredService<LoginWindow>();
        login.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    public static T GetService<T>() where T : class =>
        ((App)Current)._host!.Services.GetRequiredService<T>();

    private static string ResolveDefaultApiUrl()
    {
#if DEBUG
        return "http://localhost:6000/api";
#else
        return "https://api.mufutu.ao/api";
#endif
    }
}

/// <summary>Logger simples para ficheiro %LocalAppData%\MUFUTU\desktop.log</summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly object _lock = new();

    public FileLoggerProvider(string path) => _path = path;

    public ILogger CreateLogger(string categoryName) => new FileLogger(_path, categoryName, _lock);

    public void Dispose() { }

    private sealed class FileLogger : ILogger
    {
        private readonly string _path;
        private readonly string _category;
        private readonly object _lock;

        public FileLogger(string path, string category, object lockObj)
        {
            _path = path;
            _category = category;
            _lock = lockObj;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var line = $"{DateTime.UtcNow:O} [{logLevel}] {_category}: {formatter(state, exception)}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            lock (_lock)
            {
                File.AppendAllText(_path, line + Environment.NewLine);
            }
        }
    }
}

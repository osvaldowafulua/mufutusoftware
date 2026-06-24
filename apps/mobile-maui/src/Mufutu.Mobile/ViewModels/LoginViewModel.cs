using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core;
using Mufutu.Mobile.Core.Api;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Notifications;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Services;

namespace Mufutu.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly MufutuApiClient _api;
    private readonly IApiSettings _settings;
    private readonly MauiConnectivityMonitor _runtime;

    [ObservableProperty]
    private string _email = "joao.silva@mufutu.ao";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _apiUrl = "https://api.mufutu.ao/api";

    [ObservableProperty]
    private string _siteCode = "MUA";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(MufutuApiClient api, IApiSettings settings, MauiConnectivityMonitor runtime)
    {
        _api = api;
        _settings = settings;
        _runtime = runtime;
        _apiUrl = settings.ApiBaseUrl;
        _siteCode = settings.SiteCode;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        try
        {
            _settings.ApiBaseUrl = ApiUrl.Trim().TrimEnd('/');
            if (!_settings.ApiBaseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                _settings.ApiBaseUrl += "/api";
            }
            _settings.SiteCode = SiteCode.Trim().ToUpperInvariant();

            await _api.LoginAsync(Email, Password);
            await _runtime.StartCampoRuntimeAsync();
            await Shell.Current.GoToAsync("//campo");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

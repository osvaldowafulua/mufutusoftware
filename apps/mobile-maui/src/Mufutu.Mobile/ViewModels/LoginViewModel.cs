using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core.Api;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Services;

namespace Mufutu.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly MufutuApiClient _api;
    private readonly IApiSettings _settings;
    private readonly MauiConnectivityMonitor _runtime;
    private readonly ILocalizationService _l10n;

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

    [ObservableProperty]
    private LocaleOption? _selectedLocaleItem;

    public IReadOnlyList<LocaleOption> LocaleOptions => _l10n.SupportedLocales;

    public string LoginTitle => _l10n.Get("login_title");
    public string LoginHeading => _l10n.Get("login_heading");
    public string LoginSubtitle => _l10n.Get("login_subtitle");
    public string ApiLabel => _l10n.Get("api");
    public string SiteLabel => _l10n.Get("site");
    public string EmailLabel => _l10n.Get("email");
    public string PasswordLabel => _l10n.Get("password");
    public string LanguageLabel => _l10n.Get("language");
    public string SignInLabel => IsBusy ? _l10n.Get("signing_in") : _l10n.Get("sign_in");

    public LoginViewModel(
        MufutuApiClient api,
        IApiSettings settings,
        MauiConnectivityMonitor runtime,
        ILocalizationService l10n)
    {
        _api = api;
        _settings = settings;
        _runtime = runtime;
        _l10n = l10n;
        _apiUrl = settings.ApiBaseUrl;
        _siteCode = settings.SiteCode;
        SelectedLocaleItem = l10n.SupportedLocales.FirstOrDefault(o => o.Code == l10n.CurrentLocale);
        _l10n.LanguageChanged += (_, _) => RefreshLabels();
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(SignInLabel));

    partial void OnSelectedLocaleItemChanged(LocaleOption? value)
    {
        if (value != null && value.Code != _l10n.CurrentLocale)
        {
            _l10n.SetLocale(value.Code);
        }
    }

    private void RefreshLabels()
    {
        OnPropertyChanged(nameof(LoginTitle));
        OnPropertyChanged(nameof(LoginHeading));
        OnPropertyChanged(nameof(LoginSubtitle));
        OnPropertyChanged(nameof(ApiLabel));
        OnPropertyChanged(nameof(SiteLabel));
        OnPropertyChanged(nameof(EmailLabel));
        OnPropertyChanged(nameof(PasswordLabel));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(SignInLabel));
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

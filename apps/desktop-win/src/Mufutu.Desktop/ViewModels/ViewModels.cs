using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Desktop.Core.Api;
using Mufutu.Desktop.Views;

namespace Mufutu.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IMufutuApiClient _api;

    public MainViewModel(IMufutuApiClient api) => _api = api;

    public bool IsAuthenticated => _api.IsAuthenticated;
}

public partial class LoginViewModel : ObservableObject
{
    private readonly IMufutuApiClient _api;

    [ObservableProperty]
    private string _email = "admin@mufutu.ao";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel(IMufutuApiClient api) => _api = api;

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await _api.LoginAsync(Email, Password);
            var shell = App.GetService<ShellWindow>();
            shell.Show();
            App.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
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

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentViewModel;

    public ShellViewModel(
        DashboardViewModel dashboard,
        WorkOrdersViewModel workOrders,
        AssetsViewModel assets)
    {
        _dashboard = dashboard;
        _workOrders = workOrders;
        _assets = assets;
        CurrentViewModel = _dashboard;
    }

    private readonly DashboardViewModel _dashboard;
    private readonly WorkOrdersViewModel _workOrders;
    private readonly AssetsViewModel _assets;

    [RelayCommand]
    private void ShowDashboard() => CurrentViewModel = _dashboard;

    [RelayCommand]
    private void ShowWorkOrders() => CurrentViewModel = _workOrders;

    [RelayCommand]
    private void ShowAssets() => CurrentViewModel = _assets;
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly IMufutuApiClient _api;

    [ObservableProperty]
    private int _totalAssets;

    [ObservableProperty]
    private int _openWorkOrders;

    [ObservableProperty]
    private int _pendingRequests;

    [ObservableProperty]
    private string _statusMessage = "A carregar…";

    public DashboardViewModel(IMufutuApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var summary = await _api.GetDashboardSummaryAsync();
            TotalAssets = summary.TotalAssets;
            OpenWorkOrders = summary.OpenWorkOrders;
            PendingRequests = summary.PendingRequests;
            StatusMessage = "Dados actualizados";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }
}

public partial class WorkOrdersViewModel : ObservableObject
{
    private readonly IMufutuApiClient _api;

    [ObservableProperty]
    private IReadOnlyList<Core.Api.Models.WorkOrderDto> _items = Array.Empty<Core.Api.Models.WorkOrderDto>();

    [ObservableProperty]
    private string _statusMessage = "A carregar…";

    public WorkOrdersViewModel(IMufutuApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var page = await _api.GetWorkOrdersAsync();
            Items = page.Data;
            StatusMessage = $"{page.Total} ordens";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }
}

public partial class AssetsViewModel : ObservableObject
{
    private readonly IMufutuApiClient _api;

    [ObservableProperty]
    private IReadOnlyList<Core.Api.Models.AssetDto> _items = Array.Empty<Core.Api.Models.AssetDto>();

    [ObservableProperty]
    private string _statusMessage = "A carregar…";

    public AssetsViewModel(IMufutuApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var page = await _api.GetAssetsAsync();
            Items = page.Data;
            StatusMessage = $"{page.Total} activos";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
    }
}

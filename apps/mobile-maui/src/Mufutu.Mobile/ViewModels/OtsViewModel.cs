using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core;
using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.ViewModels;

public partial class OtsViewModel : ObservableObject
{
    private readonly ICampoDataService _data;
    private readonly IAuthSessionStore _session;
    private readonly ILocalizationService _l10n;

    [ObservableProperty]
    private string _siteLabel = "MUA";

    [ObservableProperty]
    private string _userLabel = "Técnico";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _fromCache;

    public ObservableCollection<WorkOrderItem> Items { get; } = [];

    public OtsViewModel(ICampoDataService data, IAuthSessionStore session, ILocalizationService l10n)
    {
        _data = data;
        _session = session;
        _l10n = l10n;
    }

    public async Task InitializeAsync()
    {
        SiteLabel = await _session.GetSiteCodeAsync();
        UserLabel = (await _session.GetUserNameAsync()) ?? _l10n.Get("technician");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        FromCache = !_data.IsOnline;
        try
        {
            var list = await _data.GetMyWorkOrdersAsync();
            Items.Clear();
            foreach (var wo in list)
            {
                Items.Add(WorkOrderItem.From(wo, _l10n));
            }
            if (!_data.IsOnline && Items.Count == 0)
            {
                ErrorMessage = "Sem rede e sem dados guardados.";
            }
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

    [RelayCommand]
    private async Task ActAsync(WorkOrderItem item)
    {
        if (item.StatusKey == "approved")
        {
            await StartAsync(item);
        }
        else if (item.StatusKey == "in_progress")
        {
            await FinishAsync(item);
        }
    }

    private async Task StartAsync(WorkOrderItem item)
    {
        if (item.StatusKey != "approved")
        {
            return;
        }

        try
        {
            await _data.ChangeWorkOrderStatusAsync(item.Id, "in_progress");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task FinishAsync(WorkOrderItem item)
    {
        if (item.StatusKey != "in_progress")
        {
            return;
        }

        try
        {
            await _data.ChangeWorkOrderStatusAsync(item.Id, "completed");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}

public sealed partial class WorkOrderItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string AssetName { get; init; } = string.Empty;
    public string StatusKey { get; init; } = string.Empty;

    [ObservableProperty]
    private string _statusLabel = string.Empty;

    public string ActionLabel { get; init; } = string.Empty;

    public bool CanAct => StatusKey is "approved" or "in_progress";

    public static WorkOrderItem From(WorkOrderDto dto, ILocalizationService l10n) => new()
    {
        Id = dto.Id ?? string.Empty,
        Number = dto.Number ?? "—",
        Title = dto.Title ?? "Trabalho",
        AssetName = dto.Asset?.Name ?? dto.Asset?.Code ?? "Equipamento",
        StatusKey = dto.Status ?? "approved",
        StatusLabel = l10n.WoStatus(dto.Status),
        ActionLabel = dto.Status switch
        {
            "approved" => l10n.Get("start"),
            "in_progress" => l10n.Get("finish"),
            _ => string.Empty,
        },
    };
}

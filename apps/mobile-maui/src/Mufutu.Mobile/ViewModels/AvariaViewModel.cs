using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core;
using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.ViewModels;

public partial class AvariaViewModel : ObservableObject
{
    private readonly ICampoDataService _data;
    private readonly IAuthSessionStore _session;
    private readonly ILocalizationService _l10n;

    [ObservableProperty]
    private string _siteLabel = "MUA";

    [ObservableProperty]
    private string _userLabel = "Técnico";

    [ObservableProperty]
    private string? _photoDataUrl;

    [ObservableProperty]
    private FaultReason? _selectedReason;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _sent;

    [ObservableProperty]
    private string _sentMessage = string.Empty;

    public IReadOnlyList<FaultReason> Reasons => _l10n.FaultReasons;

    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoDataUrl);

    public AvariaViewModel(ICampoDataService data, IAuthSessionStore session, ILocalizationService l10n)
    {
        _data = data;
        _session = session;
        _l10n = l10n;
        _sentMessage = l10n.Get("sent");
    }

    public async Task InitializeAsync()
    {
        SiteLabel = await _session.GetSiteCodeAsync();
        UserLabel = (await _session.GetUserNameAsync()) ?? _l10n.Get("technician");
    }

    partial void OnPhotoDataUrlChanged(string? value) => OnPropertyChanged(nameof(HasPhoto));

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ErrorMessage = "Câmara não disponível neste dispositivo.";
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions { Title = "Foto da avaria" });
            if (photo == null)
            {
                return;
            }

            await using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var b64 = Convert.ToBase64String(ms.ToArray());
            PhotoDataUrl = $"data:image/jpeg;base64,{b64}";
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void SelectReason(FaultReason reason) => SelectedReason = reason;

    [RelayCommand]
    private async Task SendAsync()
    {
        if (!HasPhoto)
        {
            ErrorMessage = _l10n.Get("photo_required");
            return;
        }
        if (SelectedReason == null)
        {
            ErrorMessage = _l10n.Get("choose_reason");
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var result = await _data.CreateMaintenanceRequestAsync(new CreateMaintenanceRequestPayload
            {
                Title = $"Avaria: {SelectedReason.Label}",
                Symptom = SelectedReason.Label,
                Description = $"Reportado via MUFUTU Campo — {SelectedReason.Label}",
                Priority = SelectedReason.Priority,
                Photos =
                [
                    new MaintenancePhotoDto
                    {
                        DataUrl = PhotoDataUrl!,
                        MimeType = "image/jpeg",
                        Name = $"avaria-{DateTime.UtcNow:yyyyMMdd-HHmmss}.jpg",
                    },
                ],
            });
            SentMessage = result.Message;
            Sent = true;
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
    private void Reset()
    {
        Sent = false;
        PhotoDataUrl = null;
        SelectedReason = null;
        ErrorMessage = null;
        SentMessage = _l10n.Get("sent");
    }
}

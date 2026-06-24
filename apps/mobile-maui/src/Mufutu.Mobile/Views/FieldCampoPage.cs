namespace Mufutu.Mobile.Views;

/// <summary>
/// Base para ecrãs Campo — respeita notch / Dynamic Island no iPhone (MAUI 8).
/// </summary>
public class FieldCampoPage : ContentPage
{
    public FieldCampoPage()
    {
        BackgroundColor = Color.FromArgb("#0F1419");
#if IOS
        Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(this, true);
#endif
    }
}

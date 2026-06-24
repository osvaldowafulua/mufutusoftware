namespace Mufutu.Mobile.Views;

/// <summary>
/// Base para ecrãs Campo — respeita notch / Dynamic Island no iPhone.
/// </summary>
public class FieldCampoPage : ContentPage
{
    public FieldCampoPage()
    {
        BackgroundColor = Color.FromArgb("#0F1419");
        SafeAreaEdges = SafeAreaEdges.All;
    }
}

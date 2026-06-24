using Mufutu.Mobile.ViewModels;

namespace Mufutu.Mobile.Views;

public partial class CampoHubPage : FieldCampoPage
{
    private readonly CampoHubViewModel _vm;

    public CampoHubPage(CampoHubViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }
}

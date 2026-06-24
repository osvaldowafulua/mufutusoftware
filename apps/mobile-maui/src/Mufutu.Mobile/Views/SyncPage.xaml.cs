using Mufutu.Mobile.ViewModels;

namespace Mufutu.Mobile.Views;

public partial class SyncPage : FieldCampoPage
{
    private readonly SyncViewModel _vm;

    public SyncPage(SyncViewModel vm)
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

using Mufutu.Mobile.ViewModels;

namespace Mufutu.Mobile.Views;

public partial class OtsPage : FieldCampoPage
{
    private readonly OtsViewModel _vm;

    public OtsPage(OtsViewModel vm)
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

using Mufutu.Mobile.ViewModels;

namespace Mufutu.Mobile.Views;

public partial class AvariaPage : FieldCampoPage
{
    private readonly AvariaViewModel _vm;

    public AvariaPage(AvariaViewModel vm)
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

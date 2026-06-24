using Mufutu.Mobile.ViewModels;

namespace Mufutu.Mobile.Views;

public partial class NotificationsPage : FieldCampoPage
{
    private readonly NotificationsViewModel _vm;

    public NotificationsPage(NotificationsViewModel vm)
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

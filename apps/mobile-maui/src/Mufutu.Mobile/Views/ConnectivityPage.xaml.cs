using Mufutu.Mobile.ViewModels;

namespace Mufutu.Mobile.Views;

public partial class ConnectivityPage : ContentPage
{
    public ConnectivityPage(ConnectivityViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.RunProbeCommand.ExecuteAsync(null);
    }
}

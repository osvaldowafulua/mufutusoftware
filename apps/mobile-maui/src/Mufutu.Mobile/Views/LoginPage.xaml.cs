using Mufutu.Mobile.ViewModels;

namespace Mufutu.Mobile.Views;

public partial class LoginPage : FieldCampoPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}

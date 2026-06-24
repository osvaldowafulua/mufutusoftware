using System.Windows;
using System.Windows.Controls;
using Mufutu.Desktop.ViewModels;

namespace Mufutu.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        Title = "MUFUTU — Entrar";
        Width = 420;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Background = System.Windows.Media.Brushes.White;

        var root = new StackPanel { Margin = new Thickness(32) };

        var title = new TextBlock
        {
            Text = "MUFUTU",
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xE8, 0x61, 0x2D)),
            Margin = new Thickness(0, 0, 0, 8),
        };
        root.Children.Add(title);

        root.Children.Add(new TextBlock
        {
            Text = "Gestão mineira · Windows",
            Margin = new Thickness(0, 0, 0, 24),
        });

        var emailBox = new TextBox();
        emailBox.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding(nameof(LoginViewModel.Email)));
        root.Children.Add(new TextBlock { Text = "Email" });
        root.Children.Add(emailBox);

        var passBox = new PasswordBox { Margin = new Thickness(0, 4, 0, 0) };
        passBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = passBox.Password;
            }
        };
        root.Children.Add(new TextBlock { Text = "Palavra-passe", Margin = new Thickness(0, 12, 0, 0) });
        root.Children.Add(passBox);

        var error = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.Firebrick,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        };
        error.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(LoginViewModel.ErrorMessage)));
        root.Children.Add(error);

        var loginBtn = new Button { Content = "Entrar", Margin = new Thickness(0, 16, 0, 0) };
        loginBtn.SetBinding(Button.CommandProperty, new System.Windows.Data.Binding(nameof(LoginViewModel.LoginCommand)));
        loginBtn.SetBinding(UIElement.IsEnabledProperty, new System.Windows.Data.Binding(nameof(LoginViewModel.IsBusy))
        {
            Converter = new InverseBoolConverter(),
        });
        root.Children.Add(loginBtn);

        Content = root;
    }
}

internal sealed class InverseBoolConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        value is bool b ? !b : false;
}

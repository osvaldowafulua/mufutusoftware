using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mufutu.Desktop.ViewModels;

namespace Mufutu.Desktop.Views;

public partial class LoginWindow : Window
{
    private static readonly SolidColorBrush BrandOrange = new(Color.FromRgb(0xE8, 0x61, 0x2D));

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        Title = "MUFUTU — Entrar";
        Width = 420;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Background = Brushes.White;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(180) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Background = BrandOrange };
        var headerStack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var logo = new Image
        {
            Width = 88,
            Height = 88,
            Stretch = Stretch.Uniform,
            Source = LoadBrandImage("logo-white.png"),
            Margin = new Thickness(0, 0, 0, 8),
        };
        headerStack.Children.Add(logo);
        headerStack.Children.Add(new TextBlock
        {
            Text = "Gestão mineira · Windows",
            Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        header.Children.Add(headerStack);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var form = new StackPanel { Margin = new Thickness(32, 28, 32, 32) };

        var emailBox = new TextBox();
        emailBox.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding(nameof(LoginViewModel.Email)));
        form.Children.Add(new TextBlock { Text = "Email" });
        form.Children.Add(emailBox);

        var passBox = new PasswordBox { Margin = new Thickness(0, 4, 0, 0) };
        passBox.PasswordChanged += (_, _) =>
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = passBox.Password;
            }
        };
        form.Children.Add(new TextBlock { Text = "Palavra-passe", Margin = new Thickness(0, 12, 0, 0) });
        form.Children.Add(passBox);

        var error = new TextBlock
        {
            Foreground = Brushes.Firebrick,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        };
        error.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(LoginViewModel.ErrorMessage)));
        form.Children.Add(error);

        var loginBtn = new Button
        {
            Content = "Entrar",
            Margin = new Thickness(0, 16, 0, 0),
            Background = BrandOrange,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(0, 10, 0, 10),
        };
        loginBtn.SetBinding(Button.CommandProperty, new System.Windows.Data.Binding(nameof(LoginViewModel.LoginCommand)));
        loginBtn.SetBinding(UIElement.IsEnabledProperty, new System.Windows.Data.Binding(nameof(LoginViewModel.IsBusy))
        {
            Converter = new InverseBoolConverter(),
        });
        form.Children.Add(loginBtn);

        Grid.SetRow(form, 1);
        root.Children.Add(form);

        Content = root;
    }

    private static BitmapImage? LoadBrandImage(string fileName)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Assets/brand/{fileName}", UriKind.Absolute);
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = uri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class InverseBoolConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        value is bool b ? !b : false;
}

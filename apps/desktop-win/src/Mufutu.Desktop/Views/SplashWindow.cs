using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mufutu.Desktop.Views;

public sealed class SplashWindow : Window
{
    private static readonly SolidColorBrush BrandOrange = new(Color.FromRgb(0xE8, 0x61, 0x2D));

    public SplashWindow()
    {
        Title = "MUFUTU";
        Width = 420;
        Height = 420;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = BrandOrange;
        ShowInTaskbar = false;
        Topmost = true;

        var root = new Grid { Background = BrandOrange };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var logo = new Image
        {
            Width = 128,
            Height = 128,
            Stretch = Stretch.Uniform,
            Source = LoadBrandImage("logo-white.png"),
            Margin = new Thickness(0, 0, 0, 20),
        };
        stack.Children.Add(logo);

        stack.Children.Add(new TextBlock
        {
            Text = "Gestão mineira · A carregar…",
            Foreground = new SolidColorBrush(Color.FromArgb(0xB8, 0xFF, 0xFF, 0xFF)),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
        });

        root.Children.Add(stack);
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

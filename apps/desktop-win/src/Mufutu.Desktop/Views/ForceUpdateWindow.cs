using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mufutu.Desktop.Core.Updates;

namespace Mufutu.Desktop.Views;

/// <summary>
/// Ecrã de bloqueio obrigatório: mostrado quando <see cref="IVersionGateService"/>
/// confirma que a versão instalada está abaixo da mínima aceite. Não tem botão
/// "fechar" nem forma de contornar — só "Actualizar agora" ou "Sair".
/// </summary>
public sealed class ForceUpdateWindow : Window
{
    private static readonly SolidColorBrush BrandOrange = new(Color.FromRgb(0xEB, 0x5E, 0x28));

    public ForceUpdateWindow(VersionGateResult result)
    {
        Title = "Actualização obrigatória — MUFUTU";
        Width = 460;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.SingleBorderWindow;
        Background = Brushes.White;
        Topmost = true;
        ShowInTaskbar = true;

        // Bloqueia Alt+F4 e o "X" — só se sai pelo botão "Sair"
        Closing += (_, e) => e.Cancel = !_allowClose;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(140) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Background = BrandOrange };
        var logo = new Image
        {
            Width = 64,
            Height = 64,
            Stretch = Stretch.Uniform,
            Source = LoadBrandImage("logo-white.png"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        header.Children.Add(logo);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var body = new StackPanel { Margin = new Thickness(32, 24, 32, 24) };

        body.Children.Add(new TextBlock
        {
            Text = "É necessário actualizar o MUFUTU",
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
        });

        var minimum = result.MinimumVersion ?? "—";
        var latest = result.LatestVersion ?? minimum;
        body.Children.Add(new TextBlock
        {
            Text = $"A sua versão é anterior à mínima suportada ({minimum}). " +
                   $"Descarregue a versão {latest} para continuar a usar o MUFUTU.",
            FontSize = 13,
            Foreground = Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        });

        if (!string.IsNullOrWhiteSpace(result.Notes))
        {
            body.Children.Add(new TextBlock
            {
                Text = result.Notes,
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16),
            });
        }

        var updateBtn = new Button
        {
            Content = "Descarregar actualização",
            Background = BrandOrange,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(0, 12, 0, 12),
            Margin = new Thickness(0, 12, 0, 8),
        };
        updateBtn.Click += (_, _) => OpenDownload(result.DownloadUrl);
        body.Children.Add(updateBtn);

        var quitBtn = new Button
        {
            Content = "Sair",
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            Padding = new Thickness(0, 10, 0, 10),
        };
        quitBtn.Click += (_, _) => Quit();
        body.Children.Add(quitBtn);

        Grid.SetRow(body, 1);
        root.Children.Add(body);

        Content = root;
    }

    private bool _allowClose;

    private void Quit()
    {
        _allowClose = true;
        Close();
        Application.Current.Shutdown();
    }

    private static void OpenDownload(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // ignorar — utilizador pode copiar o link das notas de release
        }
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

using Microsoft.Maui.ApplicationModel;

namespace Mufutu.Mobile.Views;

/// <summary>
/// Ecrã de bloqueio obrigatório: mostrado quando o <c>IUpdateGateService</c>
/// confirma que a versão instalada está abaixo da mínima aceite. Sem navegação
/// de volta e sem botão de saída — apps móveis não devem auto-terminar (guia da
/// Apple); o técnico fecha pela gesture/botão do sistema se não quiser actualizar já.
/// </summary>
public sealed class UpdateRequiredPage : ContentPage, IQueryAttributable
{
    private static readonly Color BrandOrange = Color.FromArgb("#EB5E28");

    private string? _downloadUrl;
    private readonly Label _message;
    private readonly Label _notes;

    public UpdateRequiredPage()
    {
        BackgroundColor = Colors.White;
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);

        var header = new Grid
        {
            BackgroundColor = BrandOrange,
            HeightRequest = 140,
        };
        header.Children.Add(new Label
        {
            Text = "MUFUTU",
            TextColor = Colors.White,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        });

        var title = new Label
        {
            Text = "É necessário actualizar o MUFUTU",
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 24, 0, 12),
        };

        _message = new Label
        {
            TextColor = Colors.DimGray,
            FontSize = 14,
        };

        _notes = new Label
        {
            TextColor = Colors.Gray,
            FontSize = 12,
            FontAttributes = FontAttributes.Italic,
            Margin = new Thickness(0, 8, 0, 0),
            IsVisible = false,
        };

        var updateBtn = new Button
        {
            Text = "Descarregar actualização",
            BackgroundColor = BrandOrange,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 24, 0, 8),
        };
        updateBtn.Clicked += async (_, _) => await OpenDownloadAsync();

        var hint = new Label
        {
            Text = "Sem esta actualização não é possível continuar a usar o Modo Campo.",
            TextColor = Colors.Gray,
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(32, 0, 32, 32),
            Children = { title, _message, _notes, updateBtn, hint },
        };

        Content = new VerticalStackLayout { Children = { header, body } };
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var current = query.TryGetValue("current", out var c) ? c?.ToString() : null;
        var minimum = query.TryGetValue("minimum", out var m) ? m?.ToString() : null;
        var latest = query.TryGetValue("latest", out var l) ? l?.ToString() : null;
        var notes = query.TryGetValue("notes", out var n) ? n?.ToString() : null;
        _downloadUrl = query.TryGetValue("url", out var u) ? Uri.UnescapeDataString(u?.ToString() ?? string.Empty) : null;

        _message.Text = $"A sua versão ({current}) é anterior à mínima suportada ({minimum}). " +
                         $"Descarregue a versão {latest ?? minimum} para continuar.";

        if (!string.IsNullOrWhiteSpace(notes))
        {
            _notes.Text = Uri.UnescapeDataString(notes);
            _notes.IsVisible = true;
        }
    }

    protected override bool OnBackButtonPressed() => true; // bloqueia o botão físico/gesto de voltar (Android)

    private async Task OpenDownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(_downloadUrl))
        {
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(new Uri(_downloadUrl));
        }
        catch
        {
            // sem browser/handler disponível — nada a fazer aqui
        }
    }
}

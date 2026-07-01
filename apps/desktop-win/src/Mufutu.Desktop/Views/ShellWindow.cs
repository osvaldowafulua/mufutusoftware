using System.Windows;
using System.Windows.Controls;
using Mufutu.Desktop.Localization;
using Mufutu.Desktop.Core.Updates;
using Mufutu.Desktop.Updates;
using Mufutu.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace Mufutu.Desktop.Views;

public partial class ShellWindow : Window
{
    private readonly IDesktopUpdateService _updateService;
    private readonly ILogger<ShellWindow> _logger;

    public ShellWindow(ShellViewModel viewModel, IDesktopUpdateService updateService, ILogger<ShellWindow> logger)
    {
        _updateService = updateService;
        _logger = logger;
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        Title = DesktopLanguage.T("shell_title");
        Width = 1100;
        Height = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nav = new StackPanel
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xF5, 0xF7, 0xFA)),
            Margin = new Thickness(0),
        };

        nav.Children.Add(MakeNavButton(DesktopLanguage.T("nav_dashboard"), nameof(ShellViewModel.ShowDashboardCommand)));
        nav.Children.Add(MakeNavButton(DesktopLanguage.T("nav_work_orders"), nameof(ShellViewModel.ShowWorkOrdersCommand)));
        nav.Children.Add(MakeNavButton(DesktopLanguage.T("nav_assets"), nameof(ShellViewModel.ShowAssetsCommand)));
        nav.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 12) });
        nav.Children.Add(MakeActionButton(DesktopLanguage.T("check_updates"), OnCheckUpdates));
        nav.Children.Add(MakeActionButton(DesktopLanguage.T("download_latest"), OnOpenReleases));

        Grid.SetColumn(nav, 0);
        grid.Children.Add(nav);

        var contentHost = new ContentControl();
        contentHost.SetBinding(ContentControl.ContentProperty,
            new System.Windows.Data.Binding(nameof(ShellViewModel.CurrentViewModel))
            {
                Converter = new ViewModelTemplateSelector(),
            });
        Grid.SetColumn(contentHost, 1);
        grid.Children.Add(contentHost);

        Content = grid;
    }

    private static Button MakeNavButton(string label, string commandName)
    {
        var btn = new Button
        {
            Content = label,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x15, 0x65, 0xC0)),
        };
        btn.SetBinding(Button.CommandProperty, new System.Windows.Data.Binding(commandName));
        return btn;
    }

    private static Button MakeActionButton(string label, RoutedEventHandler onClick)
    {
        var btn = new Button
        {
            Content = label,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66)),
            FontSize = 12,
        };
        btn.Click += onClick;
        return btn;
    }

    private async void OnCheckUpdates(object sender, RoutedEventArgs e)
    {
        await DesktopUpdateUi.CheckManuallyAsync(_updateService, _logger);
    }

    private void OnOpenReleases(object sender, RoutedEventArgs e)
    {
        DesktopUpdateUi.OpenReleasesPage(_updateService);
    }
}

internal sealed class ViewModelTemplateSelector : System.Windows.Data.IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            DashboardViewModel vm => new DashboardView(vm),
            WorkOrdersViewModel vm => new WorkOrdersView(vm),
            AssetsViewModel vm => new AssetsView(vm),
            _ => new TextBlock { Text = "Selecione um módulo" },
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
        throw new NotSupportedException();
}

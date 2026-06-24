using System.Windows.Controls;
using Mufutu.Desktop.ViewModels;

namespace Mufutu.Desktop.Views;

public sealed class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel vm)
    {
        var stack = new StackPanel { Margin = new System.Windows.Thickness(24) };
        stack.Children.Add(new TextBlock { Text = "Dashboard", FontSize = 22, FontWeight = System.Windows.FontWeights.SemiBold });
        stack.Children.Add(Bind("Activos", nameof(vm.TotalAssets)));
        stack.Children.Add(Bind("OTs abertas", nameof(vm.OpenWorkOrders)));
        stack.Children.Add(Bind("Pedidos pendentes", nameof(vm.PendingRequests)));
        var status = new TextBlock { Margin = new System.Windows.Thickness(0, 16, 0, 0) };
        status.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(vm.StatusMessage)));
        stack.Children.Add(status);
        Content = stack;
        DataContext = vm;
    }

    private static StackPanel Bind(string label, string prop)
    {
        var panel = new StackPanel { Margin = new System.Windows.Thickness(0, 12, 0, 0) };
        panel.Children.Add(new TextBlock { Text = label, FontWeight = System.Windows.FontWeights.SemiBold });
        var value = new TextBlock { FontSize = 20 };
        value.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(prop));
        panel.Children.Add(value);
        return panel;
    }
}

public sealed class WorkOrdersView : UserControl
{
    public WorkOrdersView(WorkOrdersViewModel vm)
    {
        var grid = new Grid { Margin = new System.Windows.Thickness(24) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new TextBlock { Text = "Ordens de trabalho", FontSize = 22, FontWeight = System.Windows.FontWeights.SemiBold };
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        var list = new ListView();
        list.SetBinding(ListView.ItemsSourceProperty, new System.Windows.Data.Binding(nameof(vm.Items)));
        list.View = new GridView
        {
            Columns =
            {
                new GridViewColumn { Header = "Número", DisplayMemberBinding = new System.Windows.Data.Binding("Number"), Width = 120 },
                new GridViewColumn { Header = "Título", DisplayMemberBinding = new System.Windows.Data.Binding("Title"), Width = 280 },
                new GridViewColumn { Header = "Estado", DisplayMemberBinding = new System.Windows.Data.Binding("Status"), Width = 120 },
                new GridViewColumn { Header = "Prioridade", DisplayMemberBinding = new System.Windows.Data.Binding("Priority"), Width = 100 },
            },
        };
        Grid.SetRow(list, 1);
        grid.Children.Add(list);
        Content = grid;
        DataContext = vm;
    }
}

public sealed class AssetsView : UserControl
{
    public AssetsView(AssetsViewModel vm)
    {
        var grid = new Grid { Margin = new System.Windows.Thickness(24) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = "Activos",
            FontSize = 22,
            FontWeight = System.Windows.FontWeights.SemiBold,
        });

        var list = new ListView();
        list.SetBinding(ListView.ItemsSourceProperty, new System.Windows.Data.Binding(nameof(vm.Items)));
        list.View = new GridView
        {
            Columns =
            {
                new GridViewColumn { Header = "ID Global", DisplayMemberBinding = new System.Windows.Data.Binding("GlobalId"), Width = 180 },
                new GridViewColumn { Header = "Nome", DisplayMemberBinding = new System.Windows.Data.Binding("Name"), Width = 260 },
                new GridViewColumn { Header = "Estado", DisplayMemberBinding = new System.Windows.Data.Binding("Status"), Width = 120 },
                new GridViewColumn { Header = "Site", DisplayMemberBinding = new System.Windows.Data.Binding("SiteCode"), Width = 80 },
            },
        };
        Grid.SetRow(list, 1);
        grid.Children.Add(list);
        Content = grid;
        DataContext = vm;
    }
}

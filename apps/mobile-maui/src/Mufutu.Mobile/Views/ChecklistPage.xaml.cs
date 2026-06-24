using Mufutu.Mobile.ViewModels;

namespace Mufutu.Mobile.Views;

public partial class ChecklistPage : FieldCampoPage
{
    private readonly ChecklistViewModel _vm;

    public ChecklistPage(ChecklistViewModel vm)
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

    private void OnRowChecked(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox { BindingContext: ChecklistRow row })
        {
            _vm.RowChangedCommand.Execute(row);
        }
    }
}

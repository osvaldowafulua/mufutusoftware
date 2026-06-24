using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core;
using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.ViewModels;

public partial class ChecklistViewModel : ObservableObject
{
    private const string PrefsKey = "mufutu_campo_checklist";

    private readonly IAuthSessionStore _session;

    [ObservableProperty]
    private string _siteLabel = "MUA";

    [ObservableProperty]
    private string _userLabel = "Técnico";

    [ObservableProperty]
    private ChecklistTemplate? _selectedTemplate;

    public IReadOnlyList<ChecklistTemplate> Templates => FieldCopy.ChecklistTemplates;

    public List<ChecklistRow> Rows { get; } = [];

    public ChecklistViewModel(IAuthSessionStore session)
    {
        _session = session;
    }

    public async Task InitializeAsync()
    {
        SiteLabel = await _session.GetSiteCodeAsync();
        UserLabel = (await _session.GetUserNameAsync()) ?? "Técnico";
        SelectedTemplate = Templates.FirstOrDefault();
        LoadRows();
    }

    partial void OnSelectedTemplateChanged(ChecklistTemplate? value)
    {
        LoadRows();
    }

    private void LoadRows()
    {
        Rows.Clear();
        if (SelectedTemplate == null)
        {
            return;
        }

        var saved = LoadSaved(SelectedTemplate.Id);
        foreach (var item in SelectedTemplate.Items)
        {
            Rows.Add(new ChecklistRow
            {
                Label = item,
                IsChecked = saved.GetValueOrDefault(item, false),
            });
        }
        OnPropertyChanged(nameof(Rows));
    }

    [RelayCommand]
    private void RowChanged(ChecklistRow row)
    {
        Save();
    }

    [RelayCommand]
    private void SelectTemplate(ChecklistTemplate template)
    {
        SelectedTemplate = template;
    }

    private void Save()
    {
        if (SelectedTemplate == null)
        {
            return;
        }

        var dict = Rows.ToDictionary(r => r.Label, r => r.IsChecked);
        var all = LoadAll();
        all[SelectedTemplate.Id] = dict;
        Preferences.Default.Set(PrefsKey, JsonSerializer.Serialize(all));
    }

    private Dictionary<string, bool> LoadSaved(string templateId)
    {
        var all = LoadAll();
        return all.GetValueOrDefault(templateId) ?? new Dictionary<string, bool>();
    }

    private static Dictionary<string, Dictionary<string, bool>> LoadAll()
    {
        var json = Preferences.Default.Get(PrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, Dictionary<string, bool>>();
        }
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(json)
            ?? new Dictionary<string, Dictionary<string, bool>>();
    }
}

public partial class ChecklistRow : ObservableObject
{
    public string Label { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isChecked;
}

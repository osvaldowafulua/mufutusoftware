using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.Views.Controls;

public partial class FieldShellView : ContentView
{
    public static readonly BindableProperty BodyProperty =
        BindableProperty.Create(nameof(Body), typeof(View), typeof(FieldShellView));

    public static readonly BindableProperty SiteLabelProperty =
        BindableProperty.Create(nameof(SiteLabel), typeof(string), typeof(FieldShellView), "MUA");

    public static readonly BindableProperty UserLabelProperty =
        BindableProperty.Create(nameof(UserLabel), typeof(string), typeof(FieldShellView), "Técnico");

    public static readonly BindableProperty HeaderRightTextProperty =
        BindableProperty.Create(nameof(HeaderRightText), typeof(string), typeof(FieldShellView), string.Empty);

    public static readonly BindableProperty ActiveRouteProperty =
        BindableProperty.Create(nameof(ActiveRoute), typeof(string), typeof(FieldShellView), "campo",
            propertyChanged: OnActiveRouteChanged);

    private static readonly Color Active = Color.FromArgb("#5BA8E8");
    private static readonly Color Inactive = Color.FromArgb("#8B9CB3");

    private ILocalizationService? _l10n;

    public View? Body
    {
        get => (View?)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public string SiteLabel
    {
        get => (string)GetValue(SiteLabelProperty);
        set => SetValue(SiteLabelProperty, value);
    }

    public string UserLabel
    {
        get => (string)GetValue(UserLabelProperty);
        set => SetValue(UserLabelProperty, value);
    }

    public string HeaderRightText
    {
        get => (string)GetValue(HeaderRightTextProperty);
        set => SetValue(HeaderRightTextProperty, value);
    }

    public string ActiveRoute
    {
        get => (string)GetValue(ActiveRouteProperty);
        set => SetValue(ActiveRouteProperty, value);
    }

    public Color NavHomeColor { get; private set; } = Active;
    public Color NavOtsColor { get; private set; } = Inactive;
    public Color NavAvariaColor { get; private set; } = Inactive;
    public Color NavChecklistColor { get; private set; } = Inactive;
    public Color NavSyncColor { get; private set; } = Inactive;

    public FieldShellView()
    {
        InitializeComponent();
        BindingContext = this;
        UpdateNavColors();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.MauiContext?.Services.GetService(typeof(ILocalizationService)) is ILocalizationService l10n)
        {
            if (_l10n != null)
            {
                _l10n.LanguageChanged -= OnLanguageChanged;
            }

            _l10n = l10n;
            _l10n.LanguageChanged += OnLanguageChanged;
            ApplyNavLabels();
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => ApplyNavLabels();

    private void ApplyNavLabels()
    {
        if (_l10n == null)
        {
            return;
        }

        NavHomeBtn.Text = _l10n.Get("nav_home");
        NavOtsBtn.Text = _l10n.Get("nav_work");
        NavAvariaBtn.Text = _l10n.Get("nav_fault");
        NavChecklistBtn.Text = _l10n.Get("nav_checklist");
        NavSyncBtn.Text = _l10n.Get("nav_sync");
    }

    private static void OnActiveRouteChanged(BindableObject bindable, object _, object __)
    {
        if (bindable is FieldShellView shell)
        {
            shell.UpdateNavColors();
        }
    }

    private void UpdateNavColors()
    {
        NavHomeColor = ActiveRoute == "campo" ? Active : Inactive;
        NavOtsColor = ActiveRoute == "ots" ? Active : Inactive;
        NavAvariaColor = ActiveRoute == "avaria" ? Active : Inactive;
        NavChecklistColor = ActiveRoute == "checklist" ? Active : Inactive;
        NavSyncColor = ActiveRoute == "sync" ? Active : Inactive;
        OnPropertyChanged(nameof(NavHomeColor));
        OnPropertyChanged(nameof(NavOtsColor));
        OnPropertyChanged(nameof(NavAvariaColor));
        OnPropertyChanged(nameof(NavChecklistColor));
        OnPropertyChanged(nameof(NavSyncColor));
    }

    private async void OnNavHome(object sender, EventArgs e) => await GoAsync("//campo");
    private async void OnNavOts(object sender, EventArgs e) => await GoAsync("//ots");
    private async void OnNavAvaria(object sender, EventArgs e) => await GoAsync("//avaria");
    private async void OnNavChecklist(object sender, EventArgs e) => await GoAsync("//checklist");
    private async void OnNavSync(object sender, EventArgs e) => await GoAsync("//sync");

    private static async Task GoAsync(string route)
    {
        if (Shell.Current.CurrentState.Location.OriginalString.Contains(route.TrimStart('/')))
        {
            return;
        }
        await Shell.Current.GoToAsync(route);
    }
}

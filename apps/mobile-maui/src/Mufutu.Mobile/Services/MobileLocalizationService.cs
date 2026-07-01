using System.Globalization;
using Mufutu.Mobile.Core;
using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.Services;

public sealed class MobileLocalizationService : ILocalizationService
{
    private const string PrefKey = "mufutu_mobile_locale";

    private static readonly LocaleOption[] AllLocales =
    [
        new("pt-AO", "Português (Angola)"),
        new("en", "English"),
        new("de", "Deutsch"),
    ];

    private string _locale = "pt-AO";

    public event EventHandler? LanguageChanged;

    public string CurrentLocale => _locale;

    public IReadOnlyList<LocaleOption> SupportedLocales => AllLocales;

    public MobileLocalizationService()
    {
        var saved = Preferences.Default.Get(PrefKey, "pt-AO");
        ApplyLocale(saved, persist: false);
    }

    public void SetLocale(string locale)
    {
        ApplyLocale(locale, persist: true);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key)
    {
        if (LocalizationCatalog.Tables.TryGetValue(_locale, out var table) &&
            table.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_locale != "pt-AO" &&
            LocalizationCatalog.Tables["pt-AO"].TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    public string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, Get(key), args);

    public IReadOnlyList<FaultReason> FaultReasons =>
    [
        new("oil_leak", Get("fault_oil_leak"), "high"),
        new("overheat", Get("fault_overheat"), "critical"),
        new("flat_tire", Get("fault_flat_tire"), "high"),
        new("noise", Get("fault_noise"), "medium"),
        new("hydraulic", Get("fault_hydraulic"), "high"),
        new("electrical", Get("fault_electrical"), "critical"),
    ];

    public IReadOnlyList<ChecklistTemplate> ChecklistTemplates =>
    [
        new("dozer", Get("cl_dozer"), ["Óleo motor", "Água radiador", "Fugas no solo", "Rastos tensos"]),
        new("excavator", Get("cl_excavator"), ["Óleo hidráulico", "Pinos e buchas", "Cabine limpa", "Pneus / rodas"]),
        new("truck", Get("cl_truck"), ["Pneus OTR", "Freios", "Luzes", "Nível diesel"]),
        new("generator", Get("cl_generator"), ["Nível diesel", "Bateria", "Ruído motor", "Tensão saída"]),
    ];

    public string WoStatus(string? status) => status switch
    {
        "approved" => Get("wo_approved"),
        "in_progress" => Get("wo_in_progress"),
        "completed" => Get("wo_completed"),
        "pending_approval" => Get("wo_pending_approval"),
        "draft" => Get("wo_draft"),
        "cancelled" => Get("wo_cancelled"),
        _ => status ?? string.Empty,
    };

    private void ApplyLocale(string locale, bool persist)
    {
        _locale = locale switch
        {
            "en" => "en",
            "de" => "de",
            _ => "pt-AO",
        };

        var culture = _locale switch
        {
            "en" => "en",
            "de" => "de",
            _ => "pt",
        };

        var ci = new CultureInfo(culture);
        CultureInfo.CurrentCulture = ci;
        CultureInfo.CurrentUICulture = ci;
        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;

        if (persist)
        {
            Preferences.Default.Set(PrefKey, _locale);
        }
    }
}

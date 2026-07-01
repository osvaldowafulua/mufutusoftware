using Mufutu.Mobile.Core;

namespace Mufutu.Mobile.Core.Services;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;

    string CurrentLocale { get; }

    IReadOnlyList<LocaleOption> SupportedLocales { get; }

    void SetLocale(string locale);

    string Get(string key);

    string Format(string key, params object[] args);

    IReadOnlyList<FaultReason> FaultReasons { get; }

    IReadOnlyList<ChecklistTemplate> ChecklistTemplates { get; }

    string WoStatus(string? status);
}

public sealed record LocaleOption(string Code, string Label);

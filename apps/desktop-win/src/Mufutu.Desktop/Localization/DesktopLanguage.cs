namespace Mufutu.Desktop.Localization;

using System.IO;

public static class DesktopLanguage
{
    private const string PrefPath = "desktop-language.txt";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Tables =
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["pt"] = Pt,
            ["en"] = En,
        };

    private static readonly IReadOnlyDictionary<string, string> Pt = new Dictionary<string, string>
    {
        ["login_title"] = "MUFUTU — Entrar",
        ["login_tagline"] = "Gestão mineira · Windows",
        ["email"] = "Email",
        ["password"] = "Palavra-passe",
        ["sign_in"] = "Entrar",
        ["language"] = "Idioma",
        ["shell_title"] = "MUFUTU",
        ["nav_dashboard"] = "Dashboard",
        ["nav_work_orders"] = "Ordens de trabalho",
        ["nav_assets"] = "Activos",
        ["check_updates"] = "Verificar actualizações",
        ["download_latest"] = "Descarregar última versão",
        ["locale_pt"] = "Português",
        ["locale_en"] = "English",
    };

    private static readonly IReadOnlyDictionary<string, string> En = new Dictionary<string, string>
    {
        ["login_title"] = "MUFUTU — Sign in",
        ["login_tagline"] = "Mining operations · Windows",
        ["email"] = "Email",
        ["password"] = "Password",
        ["sign_in"] = "Sign in",
        ["language"] = "Language",
        ["shell_title"] = "MUFUTU",
        ["nav_dashboard"] = "Dashboard",
        ["nav_work_orders"] = "Work orders",
        ["nav_assets"] = "Assets",
        ["check_updates"] = "Check for updates",
        ["download_latest"] = "Download latest version",
        ["locale_pt"] = "Português",
        ["locale_en"] = "English",
    };

    public static string Current { get; private set; } = "pt";

    public static IReadOnlyList<(string Code, string Label)> SupportedLocales =>
    [
        ("pt", T("locale_pt")),
        ("en", T("locale_en", "en")),
    ];

    public static void Load()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MUFUTU",
                PrefPath);
            if (File.Exists(path))
            {
                var saved = File.ReadAllText(path).Trim();
                if (saved is "pt" or "en")
                {
                    Current = saved;
                }
            }
        }
        catch
        {
            // keep default
        }

        ApplyCulture();
    }

    public static void Set(string code)
    {
        Current = code is "en" ? "en" : "pt";
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MUFUTU");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, PrefPath), Current);
        }
        catch
        {
            // ignore
        }

        ApplyCulture();
    }

    public static string T(string key, string? localeOverride = null)
    {
        var locale = localeOverride ?? Current;
        if (Tables.TryGetValue(locale, out var table) && table.TryGetValue(key, out var value))
        {
            return value;
        }

        return Tables["pt"].TryGetValue(key, out var fallback) ? fallback : key;
    }

    private static void ApplyCulture()
    {
        var culture = Current == "en"
            ? new System.Globalization.CultureInfo("en")
            : new System.Globalization.CultureInfo("pt");
        System.Globalization.CultureInfo.CurrentCulture = culture;
        System.Globalization.CultureInfo.CurrentUICulture = culture;
    }
}

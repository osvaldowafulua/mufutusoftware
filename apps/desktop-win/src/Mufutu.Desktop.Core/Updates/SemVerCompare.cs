namespace Mufutu.Desktop.Core.Updates;

/// <summary>Comparação simples de versões semver (ignora sufixos -beta/-rc).</summary>
public static class SemVerCompare
{
    public static bool IsNewer(string candidate, string baseline)
    {
        if (Version.TryParse(Normalize(candidate), out var candidateVersion)
            && Version.TryParse(Normalize(baseline), out var baselineVersion))
        {
            return candidateVersion > baselineVersion;
        }

        return string.Compare(candidate, baseline, StringComparison.OrdinalIgnoreCase) > 0;
    }

    public static bool IsOlder(string candidate, string baseline) => IsNewer(baseline, candidate);

    public static string Normalize(string value) => value.Split('-')[0];
}

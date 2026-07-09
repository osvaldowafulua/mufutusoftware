namespace Mufutu.Mobile;

/// <summary>
/// Registo de crashes em ficheiro local — permite diagnosticar falhas de arranque
/// em builds Release no terreno (sem adb/logcat). Nunca lança excepções.
/// </summary>
public static class CrashLog
{
    public static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "mufutu-crash.log");

    public static void Write(string source, Exception? ex)
    {
        try
        {
            File.AppendAllText(FilePath, $"[{DateTimeOffset.UtcNow:O}] {source}\n{ex}\n----\n");
        }
        catch
        {
            // último recurso — o logger nunca pode falhar
        }
    }

    public static string? ReadLast()
    {
        try
        {
            return File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;
        }
        catch
        {
            return null;
        }
    }

    public static void Clear()
    {
        try
        {
            File.Delete(FilePath);
        }
        catch
        {
            // ignorar
        }
    }
}

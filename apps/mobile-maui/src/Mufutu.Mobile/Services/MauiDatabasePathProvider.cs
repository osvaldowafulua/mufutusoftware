using Mufutu.Mobile.Core.Offline;

namespace Mufutu.Mobile.Services;

public sealed class MauiDatabasePathProvider : IDatabasePathProvider
{
    public string GetDatabasePath() =>
        Path.Combine(FileSystem.AppDataDirectory, "mufutu_campo.db");
}

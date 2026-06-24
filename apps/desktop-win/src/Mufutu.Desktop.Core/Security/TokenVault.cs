using Mufutu.Desktop.Core.Crypto;

namespace Mufutu.Desktop.Core.Security;

public interface ITokenVault
{
    void Store(string key, string value);
    string? Retrieve(string key);
    void Remove(string key);
}

/// <summary>
/// Armazena tokens encriptados com AES-256-GCM (fallback multiplataforma / CI).
/// No Windows, a app WPF regista <see cref="WindowsCredentialTokenVault"/>.
/// </summary>
public sealed class EncryptedFileTokenVault : ITokenVault
{
    private readonly string _vaultDir;
    private readonly Func<byte[]> _masterKeyProvider;
    private readonly AesGcmCipher _cipher = new();

    public EncryptedFileTokenVault(Func<byte[]> masterKeyProvider, string? vaultDir = null)
    {
        _masterKeyProvider = masterKeyProvider;
        _vaultDir = vaultDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mufutu", "vault");
        Directory.CreateDirectory(_vaultDir);
    }

    public void Store(string key, string value)
    {
        var encrypted = _cipher.EncryptToBase64(value, _masterKeyProvider());
        File.WriteAllText(GetPath(key), encrypted);
    }

    public string? Retrieve(string key)
    {
        var path = GetPath(key);
        if (!File.Exists(path))
        {
            return null;
        }

        return _cipher.DecryptFromBase64(File.ReadAllText(path), _masterKeyProvider());
    }

    public void Remove(string key)
    {
        var path = GetPath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string GetPath(string key) => Path.Combine(_vaultDir, $"{key}.vault");
}

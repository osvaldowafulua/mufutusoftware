using System.Security.Cryptography;
using Mufutu.Desktop.Core.Crypto;

namespace Mufutu.Desktop.Core.Security;

public interface ISecureStorageService
{
    void SaveSecret(string key, string value);
    string? LoadSecret(string key);
    void DeleteSecret(string key);
    byte[] GetOrCreateMasterKey();
}

public sealed class SecureStorageService : ISecureStorageService
{
    private const string MasterKeyFile = "master.key";
    private readonly string _storageDir;
    private readonly AesGcmCipher _cipher = new();
    private readonly IProtectedDataStore _protectedDataStore;
    private readonly ITokenVault _tokenVault;

    public SecureStorageService(
        IProtectedDataStore protectedDataStore,
        ITokenVault tokenVault,
        string? storageDir = null)
    {
        _protectedDataStore = protectedDataStore;
        _tokenVault = tokenVault;
        _storageDir = storageDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mufutu");
        Directory.CreateDirectory(_storageDir);
    }

    public void SaveSecret(string key, string value)
    {
        if (IsTokenKey(key))
        {
            _tokenVault.Store(key, value);
            return;
        }

        var masterKey = GetOrCreateMasterKey();
        var encrypted = _cipher.EncryptToBase64(value, masterKey);
        File.WriteAllText(GetSecretPath(key), encrypted);
    }

    public string? LoadSecret(string key)
    {
        if (IsTokenKey(key))
        {
            return _tokenVault.Retrieve(key);
        }

        var path = GetSecretPath(key);
        if (!File.Exists(path))
        {
            return null;
        }

        var masterKey = GetOrCreateMasterKey();
        return _cipher.DecryptFromBase64(File.ReadAllText(path), masterKey);
    }

    public void DeleteSecret(string key)
    {
        if (IsTokenKey(key))
        {
            _tokenVault.Remove(key);
            return;
        }

        var path = GetSecretPath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public byte[] GetOrCreateMasterKey()
    {
        var path = Path.Combine(_storageDir, MasterKeyFile);
        if (File.Exists(path))
        {
            var protectedBytes = File.ReadAllBytes(path);
            return _protectedDataStore.Unprotect(protectedBytes);
        }

        var key = RandomNumberGenerator.GetBytes(32);
        var protectedKey = _protectedDataStore.Protect(key);
        File.WriteAllBytes(path, protectedKey);
        return key;
    }

    private static bool IsTokenKey(string key) =>
        key is "access_token" or "refresh_token";

    private string GetSecretPath(string key) =>
        Path.Combine(_storageDir, $"{Sanitize(key)}.enc");

    private static string Sanitize(string key) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
}

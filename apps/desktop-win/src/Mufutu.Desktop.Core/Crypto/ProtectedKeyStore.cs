using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Mufutu.Desktop.Core.Crypto;

public interface IProtectedDataStore
{
    byte[] Protect(byte[] data);
    byte[] Unprotect(byte[] data);
}

/// <summary>
/// DPAPI CurrentUser no Windows; AES local com chave de máquina em outros SO (dev/CI).
/// </summary>
public sealed class ProtectedKeyStore : IProtectedDataStore
{
    private readonly byte[] _fallbackKey;

    public ProtectedKeyStore()
    {
        _fallbackKey = SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(
                $"{Environment.MachineName}:{Environment.UserName}:mufutu-desktop"));
    }

    public byte[] Protect(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (OperatingSystem.IsWindows())
        {
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        }

        using var aes = Aes.Create();
        aes.Key = _fallbackKey;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(data, 0, data.Length);
        var result = new byte[aes.IV.Length + cipher.Length];
        aes.IV.CopyTo(result, 0);
        cipher.CopyTo(result, aes.IV.Length);
        return result;
    }

    public byte[] Unprotect(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (OperatingSystem.IsWindows())
        {
            return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
        }

        using var aes = Aes.Create();
        aes.Key = _fallbackKey;
        var ivLength = aes.BlockSize / 8;
        var iv = data.AsSpan(0, ivLength);
        var cipher = data.AsSpan(ivLength);
        aes.IV = iv.ToArray();
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipher.ToArray(), 0, cipher.Length);
    }
}

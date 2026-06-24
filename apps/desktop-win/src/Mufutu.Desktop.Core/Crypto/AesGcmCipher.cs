using System.Security.Cryptography;

namespace Mufutu.Desktop.Core.Crypto;

/// <summary>
/// AES-256-GCM — layout compatível com packages/licensing crypto.ts: IV(12) || TAG(16) || ciphertext.
/// </summary>
public sealed class AesGcmCipher
{
    private const int IvLength = 12;
    private const int TagLength = 16;
    private const int KeyLength = 32;

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key)
    {
        ValidateKey(key);
        var iv = RandomNumberGenerator.GetBytes(IvLength);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagLength];

        using var aes = new AesGcm(key, TagLength);
        aes.Encrypt(iv, plaintext, ciphertext, tag);

        var result = new byte[IvLength + TagLength + ciphertext.Length];
        iv.CopyTo(result.AsSpan(0, IvLength));
        tag.CopyTo(result.AsSpan(IvLength, TagLength));
        ciphertext.CopyTo(result.AsSpan(IvLength + TagLength));
        return result;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> key)
    {
        ValidateKey(key);
        if (payload.Length < IvLength + TagLength)
        {
            throw new CryptographicException("Payload encriptado inválido");
        }

        var iv = payload[..IvLength];
        var tag = payload.Slice(IvLength, TagLength);
        var ciphertext = payload[(IvLength + TagLength)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagLength);
        aes.Decrypt(iv, ciphertext, tag, plaintext);
        return plaintext;
    }

    public string EncryptToBase64(string plaintext, ReadOnlySpan<byte> key)
    {
        var bytes = Encrypt(System.Text.Encoding.UTF8.GetBytes(plaintext), key);
        return Convert.ToBase64String(bytes);
    }

    public string DecryptFromBase64(string encryptedBase64, ReadOnlySpan<byte> key)
    {
        var payload = Convert.FromBase64String(encryptedBase64);
        var plain = Decrypt(payload, key);
        return System.Text.Encoding.UTF8.GetString(plain);
    }

    public static byte[] DeriveKey(string passphrase, ReadOnlySpan<byte> salt, int iterations = 100_000)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            passphrase,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeyLength);
    }

    private static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeyLength)
        {
            throw new ArgumentException("Chave AES-256 deve ter 32 bytes", nameof(key));
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;

namespace Mufutu.Desktop.Licensing;

public sealed class LicenseVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public LicenseClaims Verify(string token, IReadOnlyList<PublicKeyRecord> publicKeys)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentNullException.ThrowIfNull(publicKeys);

        if (publicKeys.Count == 0)
        {
            throw new InvalidOperationException("Nenhuma chave pública configurada");
        }

        var compact = StripPrefix(token.Trim());
        var parts = compact.Split('.');
        if (parts.Length != 3)
        {
            throw new FormatException("Formato de licença inválido");
        }

        var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
        using var headerDoc = JsonDocument.Parse(headerJson);
        var alg = headerDoc.RootElement.GetProperty("alg").GetString();
        if (!string.Equals(alg, "EdDSA", StringComparison.Ordinal))
        {
            throw new CryptographicException("Algoritmo de licença não suportado");
        }

        var kid = headerDoc.RootElement.TryGetProperty("kid", out var kidEl)
            ? kidEl.GetString()
            : null;

        var record = publicKeys.FirstOrDefault(k => k.Kid == kid)
            ?? (publicKeys.Count == 1 ? publicKeys[0] : null);

        if (record is null)
        {
            throw new CryptographicException($"Chave pública não encontrada (kid: {kid ?? "?"})");
        }

        var signingInput = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
        var signature = Base64UrlDecode(parts[2]);
        var publicKey = ReadEd25519PublicKey(record.PublicKey);

        var verifier = new Ed25519Signer();
        verifier.Init(false, publicKey);
        verifier.BlockUpdate(signingInput, 0, signingInput.Length);
        if (!verifier.VerifySignature(signature))
        {
            throw new CryptographicException("Assinatura de licença inválida");
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        var claims = JsonSerializer.Deserialize<LicenseClaims>(payloadJson, JsonOptions)
            ?? throw new FormatException("Payload de licença inválido");

        if (claims.ValidUntil is { } until
            && DateTimeOffset.TryParse(until, out var expiry)
            && expiry < DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Licença expirada");
        }

        return claims;
    }

    public static string StripPrefix(string token)
    {
        return token.StartsWith(LicenseConstants.Prefix, StringComparison.Ordinal)
            ? token[LicenseConstants.Prefix.Length..]
            : token;
    }

    private static Ed25519PublicKeyParameters ReadEd25519PublicKey(string pem)
    {
        using var reader = new StringReader(pem);
        var keyObject = new PemReader(reader).ReadObject()
            ?? throw new CryptographicException("Chave pública inválida");

        return keyObject switch
        {
            Ed25519PublicKeyParameters ed => ed,
            AsymmetricKeyParameter asymmetric when asymmetric is Ed25519PublicKeyParameters edKey => edKey,
            _ => throw new CryptographicException("Chave pública Ed25519 esperada"),
        };
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var pad = 4 - (input.Length % 4);
        if (pad == 4)
        {
            pad = 0;
        }

        var b64 = input.Replace('-', '+').Replace('_', '/');
        if (pad > 0)
        {
            b64 += new string('=', pad);
        }

        return Convert.FromBase64String(b64);
    }
}

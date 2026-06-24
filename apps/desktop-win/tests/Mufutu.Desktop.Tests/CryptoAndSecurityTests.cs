using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Mufutu.Desktop.Core.Crypto;
using Mufutu.Desktop.Core.Security;
using Mufutu.Desktop.Licensing;

namespace Mufutu.Desktop.Tests;

public class AesGcmCipherTests
{
  private readonly AesGcmCipher _cipher = new();
  private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

  [Fact]
  public void EncryptDecrypt_roundtrip()
  {
    var plain = "MUFUTU-secret-payload-2026";
    var encrypted = _cipher.EncryptToBase64(plain, _key);
    var decrypted = _cipher.DecryptFromBase64(encrypted, _key);
    decrypted.Should().Be(plain);
  }

  [Fact]
  public void Payload_layout_matches_iv_tag_cipher()
  {
    var bytes = _cipher.Encrypt(Encoding.UTF8.GetBytes("x"), _key);
    bytes.Length.Should().BeGreaterThan(28);
  }

  [Fact]
  public void DeriveKey_is_deterministic()
  {
    var salt = RandomNumberGenerator.GetBytes(16);
    var a = AesGcmCipher.DeriveKey("pass", salt);
    var b = AesGcmCipher.DeriveKey("pass", salt);
    a.Should().Equal(b);
    a.Length.Should().Be(32);
  }
}

public class SecureStorageServiceTests
{
  [Fact]
  public void SaveLoadDelete_secret_roundtrip()
  {
    var dir = Path.Combine(Path.GetTempPath(), "mufutu-test-" + Guid.NewGuid());
    var store = new SecureStorageService(
      new ProtectedKeyStore(),
      new EncryptedFileTokenVault(() => RandomNumberGenerator.GetBytes(32), Path.Combine(dir, "vault")),
      dir);

    store.SaveSecret("license_key", "MUFUTU-LIC-test");
    store.LoadSecret("license_key").Should().Be("MUFUTU-LIC-test");
    store.DeleteSecret("license_key");
    store.LoadSecret("license_key").Should().BeNull();
  }

  [Fact]
  public void Token_vault_encrypts_values()
  {
    var dir = Path.Combine(Path.GetTempPath(), "mufutu-vault-" + Guid.NewGuid());
    var key = RandomNumberGenerator.GetBytes(32);
    var vault = new EncryptedFileTokenVault(() => key, dir);
    vault.Store("access_token", "jwt-test");
    vault.Retrieve("access_token").Should().Be("jwt-test");
    File.ReadAllText(Path.Combine(dir, "access_token.vault")).Should().NotContain("jwt-test");
  }
}

public class LicenseVerifierTests
{
  [Fact]
  public void StripPrefix_removes_mufutu_prefix()
  {
    LicenseVerifier.StripPrefix("MUFUTU-LIC-abc.def.ghi").Should().Be("abc.def.ghi");
  }

  [Fact]
  public void Verify_throws_on_invalid_token()
  {
    var verifier = new LicenseVerifier();
    var act = () => verifier.Verify("invalid", new[]
    {
      new PublicKeyRecord { Kid = "k1", PublicKey = "not-a-pem" },
    });
    act.Should().Throw<Exception>();
  }
}

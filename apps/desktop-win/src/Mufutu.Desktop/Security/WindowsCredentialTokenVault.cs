namespace Mufutu.Desktop.Core.Security;

/// <summary>
/// Windows Credential Locker — só compilado no projecto WPF (net8.0-windows).
/// </summary>
public sealed class WindowsCredentialTokenVault : ITokenVault
{
    private const string VaultPrefix = "Mufutu.Desktop";

    public void Store(string key, string value)
    {
        var resource = $"{VaultPrefix}:{key}";
        var vault = new Windows.Security.Credentials.PasswordVault();
        try
        {
            var existing = vault.Retrieve(resource, Environment.UserName);
            vault.Remove(existing);
        }
        catch (Exception)
        {
            // Credencial ainda não existe.
        }

        vault.Add(new Windows.Security.Credentials.PasswordCredential(
            resource,
            Environment.UserName,
            value));
    }

    public string? Retrieve(string key)
    {
        var resource = $"{VaultPrefix}:{key}";
        try
        {
            var cred = new Windows.Security.Credentials.PasswordVault().Retrieve(
                resource,
                Environment.UserName);
            cred.RetrievePassword();
            return cred.Password;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Remove(string key)
    {
        var resource = $"{VaultPrefix}:{key}";
        try
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            var cred = vault.Retrieve(resource, Environment.UserName);
            vault.Remove(cred);
        }
        catch (Exception)
        {
            // Ignorar se não existir.
        }
    }
}

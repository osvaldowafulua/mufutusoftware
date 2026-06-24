using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.Services;

public sealed class MauiAuthSessionStore : IAuthSessionStore
{
    private const string AccessKey = "mufutu_mobile_at";
    private const string RefreshKey = "mufutu_mobile_rt";
    private const string UserIdKey = "mufutu_mobile_uid";
    private const string UserNameKey = "mufutu_mobile_uname";
    private const string SiteKey = "mufutu_mobile_site";

    public async Task<string?> GetRefreshTokenAsync() =>
        await SecureStorage.Default.GetAsync(RefreshKey);

    public async Task UpdateAccessTokenAsync(string accessToken, string? refreshToken = null)
    {
        await SecureStorage.Default.SetAsync(AccessKey, accessToken);
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await SecureStorage.Default.SetAsync(RefreshKey, refreshToken);
        }
    }

    public async Task SaveAsync(LoginResponse response, string siteCode = "MUA")
    {
        if (!string.IsNullOrWhiteSpace(response.AccessToken))
        {
            await SecureStorage.Default.SetAsync(AccessKey, response.AccessToken);
        }
        if (!string.IsNullOrWhiteSpace(response.RefreshToken))
        {
            await SecureStorage.Default.SetAsync(RefreshKey, response.RefreshToken);
        }
        var user = response.User;
        if (user?.Id != null)
        {
            await SecureStorage.Default.SetAsync(UserIdKey, user.Id);
        }
        var name = string.Join(' ', new[] { user?.FirstName, user?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(name))
        {
            await SecureStorage.Default.SetAsync(UserNameKey, name);
        }
        await SecureStorage.Default.SetAsync(SiteKey, siteCode);
    }

    public async Task<string?> GetAccessTokenAsync() =>
        await SecureStorage.Default.GetAsync(AccessKey);

    public async Task<string?> GetUserIdAsync() =>
        await SecureStorage.Default.GetAsync(UserIdKey);

    public async Task<string?> GetUserNameAsync() =>
        await SecureStorage.Default.GetAsync(UserNameKey);

    public async Task<string> GetSiteCodeAsync() =>
        (await SecureStorage.Default.GetAsync(SiteKey)) ?? "MUA";

    public async Task<bool> HasSessionAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }

    public void Clear()
    {
        SecureStorage.Default.Remove(AccessKey);
        SecureStorage.Default.Remove(RefreshKey);
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(UserNameKey);
        SecureStorage.Default.Remove(SiteKey);
    }
}

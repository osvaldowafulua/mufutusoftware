using Mufutu.Mobile.Core.Models;

namespace Mufutu.Mobile.Core.Services;

public interface IAuthSessionStore
{
    Task<string?> GetRefreshTokenAsync();
    Task UpdateAccessTokenAsync(string accessToken, string? refreshToken = null);
    Task SaveAsync(LoginResponse response, string siteCode = "MUA");
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetUserIdAsync();
    Task<string?> GetUserNameAsync();
    Task<string> GetSiteCodeAsync();
    Task<bool> HasSessionAsync();
    void Clear();
}

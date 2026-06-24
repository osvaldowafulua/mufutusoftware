namespace Mufutu.Mobile.Core.Configuration;

public interface IApiSettings
{
    string ApiBaseUrl { get; set; }
    string SiteCode { get; set; }
}

public sealed class ApiSettings : IApiSettings
{
    public string ApiBaseUrl { get; set; } = "https://api.mufutu.ao/api";
    public string SiteCode { get; set; } = "MUA";
}

using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Connectivity;
using Xunit;

namespace Mufutu.Mobile.Tests;

internal sealed class FakeNetworkStatusProvider : INetworkStatusProvider
{
    public bool IsInternetAvailable { get; init; } = true;
    public bool IsWifi { get; init; } = true;
    public bool IsCellular { get; init; }
    public string NetworkDescription { get; init; } = "Internet (Wi‑Fi)";
}

internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_handler(request));
}

public class ConnectivityProbeServiceTests
{
    [Fact]
    public async Task ProbeHealth_returns_NoNetwork_when_offline()
    {
        var network = new FakeNetworkStatusProvider { IsInternetAvailable = false };
        var http = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var svc = new ConnectivityProbeService(
            http,
            network,
            Options.Create(new MobileClientOptions()));

        var result = await svc.ProbeHealthAsync();

        Assert.Equal(ConnectivityProbeStatus.NoNetwork, result.Status);
    }

    [Fact]
    public async Task ProbeHealth_success_on_200()
    {
        var network = new FakeNetworkStatusProvider();
        var http = new HttpClient(new StubHttpHandler(req =>
        {
            Assert.EndsWith("/health", req.RequestUri?.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ok\"}"),
            };
        }));

        var svc = new ConnectivityProbeService(
            http,
            network,
            Options.Create(new MobileClientOptions { ApiBaseUrl = "https://api.mufutu.ao/api" }));

        var result = await svc.ProbeHealthAsync();

        Assert.Equal(ConnectivityProbeStatus.Success, result.Status);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.True(result.IsWifi);
    }

    [Fact]
    public async Task ProbeHealth_reports_cellular_profile()
    {
        var network = new FakeNetworkStatusProvider
        {
            IsWifi = false,
            IsCellular = true,
            NetworkDescription = "Internet (dados móveis)",
        };
        var http = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok"),
        }));

        var svc = new ConnectivityProbeService(http, network, Options.Create(new MobileClientOptions()));
        var result = await svc.ProbeHealthAsync();

        Assert.Equal(ConnectivityProbeStatus.Success, result.Status);
        Assert.False(result.IsWifi);
        Assert.Contains("móveis", result.NetworkDescription);
    }
}

using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Updates;
using Xunit;

namespace Mufutu.Mobile.Tests;

public sealed class GateStubHttpHandler : HttpMessageHandler
{
    public string ManifestJson { get; set; } = "{}";
    public bool ThrowOnSend { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (ThrowOnSend)
        {
            throw new HttpRequestException("sem rede");
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ManifestJson, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}

public class UpdateGateServiceTests
{
    private static string Manifest(string latest, string minimum) =>
        $$"""
        {
          "platforms": {
            "android": {
              "latestVersion": "{{latest}}",
              "minimumVersion": "{{minimum}}",
              "downloadUrl": "https://example.test/mufutu.apk",
              "note": "teste"
            }
          }
        }
        """;

    private static UpdateGateService Create(string manifestJson, bool throwOnSend = false)
    {
        var handler = new GateStubHttpHandler { ManifestJson = manifestJson, ThrowOnSend = throwOnSend };
        var http = new HttpClient(handler);
        var options = Options.Create(new MobileClientOptions());
        return new UpdateGateService(http, options);
    }

    [Fact]
    public async Task Versao_abaixo_do_minimo_e_bloqueada()
    {
        var service = Create(Manifest(latest: "1.0.14", minimum: "1.0.13"));

        var result = await service.CheckAsync("1.0.10");

        Assert.Equal(UpdateGateStatus.UpdateRequired, result.Status);
        Assert.Equal("1.0.13", result.MinimumVersion);
        Assert.Equal("https://example.test/mufutu.apk", result.DownloadUrl);
    }

    [Fact]
    public async Task Versao_entre_minimo_e_ultima_apenas_sugere_actualizar()
    {
        var service = Create(Manifest(latest: "1.0.14", minimum: "1.0.12"));

        var result = await service.CheckAsync("1.0.13");

        Assert.Equal(UpdateGateStatus.UpdateAvailable, result.Status);
    }

    [Fact]
    public async Task Versao_atual_nao_bloqueia()
    {
        var service = Create(Manifest(latest: "1.0.14", minimum: "1.0.12"));

        var result = await service.CheckAsync("1.0.14");

        Assert.Equal(UpdateGateStatus.UpToDate, result.Status);
    }

    [Fact]
    public async Task Sem_rede_nunca_bloqueia_o_tecnico_no_terreno()
    {
        var service = Create(Manifest(latest: "1.0.14", minimum: "1.0.14"), throwOnSend: true);

        var result = await service.CheckAsync("1.0.0");

        Assert.Equal(UpdateGateStatus.CheckFailed, result.Status);
    }

    [Fact]
    public async Task Manifesto_sem_a_plataforma_nunca_bloqueia()
    {
        var service = Create("""{"platforms":{"windows":{"latestVersion":"1.0.20","minimumVersion":"1.0.20"}}}""");

        var result = await service.CheckAsync("1.0.0");

        Assert.Equal(UpdateGateStatus.CheckFailed, result.Status);
    }
}

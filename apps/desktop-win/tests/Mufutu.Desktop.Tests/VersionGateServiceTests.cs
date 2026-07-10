using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mufutu.Desktop.Core.Configuration;
using Mufutu.Desktop.Core.Updates;
using Xunit;

namespace Mufutu.Desktop.Tests;

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

public class VersionGateServiceTests
{
    private static string Manifest(string latest, string minimum) =>
        $$"""
        {
          "platforms": {
            "windows": {
              "latestVersion": "{{latest}}",
              "minimumVersion": "{{minimum}}",
              "downloadUrl": "https://example.test/setup.exe",
              "note": "teste"
            }
          }
        }
        """;

    private static VersionGateService Create(string manifestJson, string currentVersion, bool throwOnSend = false)
    {
        var handler = new GateStubHttpHandler { ManifestJson = manifestJson, ThrowOnSend = throwOnSend };
        var http = new HttpClient(handler);
        var options = Options.Create(new MufutuClientOptions());
        return new VersionGateService(http, options, NullLogger<VersionGateService>.Instance, () => currentVersion);
    }

    [Fact]
    public async Task Versao_abaixo_do_minimo_e_bloqueada()
    {
        var service = Create(Manifest(latest: "1.0.20", minimum: "1.0.16"), currentVersion: "1.0.10");

        var result = await service.CheckAsync();

        result.Status.Should().Be(VersionGateStatus.UpdateRequired);
        result.MinimumVersion.Should().Be("1.0.16");
        result.DownloadUrl.Should().Be("https://example.test/setup.exe");
    }

    [Fact]
    public async Task Versao_entre_minimo_e_ultima_apenas_sugere_actualizar()
    {
        var service = Create(Manifest(latest: "1.0.20", minimum: "1.0.16"), currentVersion: "1.0.18");

        var result = await service.CheckAsync();

        result.Status.Should().Be(VersionGateStatus.UpdateAvailable);
    }

    [Fact]
    public async Task Versao_atual_nao_bloqueia()
    {
        var service = Create(Manifest(latest: "1.0.20", minimum: "1.0.16"), currentVersion: "1.0.20");

        var result = await service.CheckAsync();

        result.Status.Should().Be(VersionGateStatus.UpToDate);
    }

    [Fact]
    public async Task Sem_rede_nunca_bloqueia_arranque_offline()
    {
        var service = Create(Manifest(latest: "1.0.20", minimum: "1.0.20"), currentVersion: "1.0.0", throwOnSend: true);

        var result = await service.CheckAsync();

        result.Status.Should().Be(VersionGateStatus.CheckFailed);
    }

    [Fact]
    public async Task Manifesto_sem_a_plataforma_nunca_bloqueia()
    {
        var service = Create("""{"platforms":{"macos":{"latestVersion":"1.0.20","minimumVersion":"1.0.20"}}}""", currentVersion: "1.0.0");

        var result = await service.CheckAsync();

        result.Status.Should().Be(VersionGateStatus.CheckFailed);
    }
}

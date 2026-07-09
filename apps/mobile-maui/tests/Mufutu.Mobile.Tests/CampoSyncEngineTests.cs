using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Mufutu.Mobile.Core.Api;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;
using Xunit;

namespace Mufutu.Mobile.Tests;

public sealed class FakeAuthSessionStore : IAuthSessionStore
{
    public Task<string?> GetRefreshTokenAsync() => Task.FromResult<string?>("refresh");
    public Task UpdateAccessTokenAsync(string accessToken, string? refreshToken = null) => Task.CompletedTask;
    public Task SaveAsync(LoginResponse response, string siteCode = "MUA") => Task.CompletedTask;
    public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>("token");
    public Task<string?> GetUserIdAsync() => Task.FromResult<string?>("user-1");
    public Task<string?> GetUserNameAsync() => Task.FromResult<string?>("Técnico");
    public Task<string> GetSiteCodeAsync() => Task.FromResult("MUA");
    public Task<bool> HasSessionAsync() => Task.FromResult(true);
    public void Clear() { }
}

public sealed class SyncStubHttpHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, string> Responder { get; set; } = _ => """{"workOrders":[],"total":0}""";
    public List<Uri> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request.RequestUri!);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Responder(request), Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}

public class CampoSyncEngineTests
{
    private static string WorkOrdersJson(params (string Id, string Status)[] orders)
    {
        var payload = new
        {
            workOrders = orders.Select(o => new
            {
                id = o.Id,
                number = $"OT-{o.Id}",
                title = $"Trabalho {o.Id}",
                status = o.Status,
                priority = "high",
                asset = new { name = "CAT 777" },
            }),
            total = orders.Length,
        };
        return System.Text.Json.JsonSerializer.Serialize(payload);
    }

    private static (CampoSyncEngine Engine, CampoOfflineStore Store, SyncStubHttpHandler Http) CreateEngine(
        bool serverSupportsDelta = false)
    {
        var store = new CampoOfflineStore(new TestDatabasePathProvider());
        var handler = new SyncStubHttpHandler();
        var api = new MufutuApiClient(
            new HttpClient(handler),
            new FakeAuthSessionStore(),
            new ApiSettings { ApiBaseUrl = "https://api.test/api" });
        var engine = new CampoSyncEngine(
            store,
            api,
            Options.Create(new MobileClientOptions { ServerSupportsDeltaSync = serverSupportsDelta }));
        return (engine, store, handler);
    }

    [Fact]
    public async Task SyncAll_descarrega_para_cache_e_nao_reescreve_o_que_nao_mudou()
    {
        var (engine, store, http) = CreateEngine();
        http.Responder = _ => WorkOrdersJson(("wo-1", "approved"), ("wo-2", "in_progress"));

        var first = await engine.SyncAllAsync();
        Assert.True(first.PullOk);
        Assert.Equal(2, first.Pulled);
        Assert.Equal(0, first.Unchanged);

        // Segundo sync com os mesmos dados: nada é reescrito
        var second = await engine.SyncAllAsync();
        Assert.Equal(0, second.Pulled);
        Assert.Equal(2, second.Unchanged);

        var cached = await store.GetWorkOrdersAsync();
        Assert.Equal(2, cached.Count);
    }

    [Fact]
    public async Task SyncAll_preserva_alteracoes_locais_pendentes()
    {
        var (engine, store, http) = CreateEngine();
        http.Responder = _ => WorkOrdersJson(("wo-1", "approved"));
        await engine.SyncAllAsync();

        // Técnico muda estado offline — fica pendente de envio
        await store.UpdateWorkOrderStatusLocalAsync("wo-1", "in_progress");

        // Servidor ainda diz "approved" — o pull não pode esmagar o estado local
        var result = await engine.SyncAllAsync();
        var cached = await store.GetWorkOrdersAsync();
        Assert.Equal("in_progress", cached.Single(c => c.Id == "wo-1").Status);
        Assert.Equal(1, result.Unchanged); // contado como preservado
    }

    [Fact]
    public async Task SyncAll_pull_completo_remove_o_que_desapareceu_do_servidor()
    {
        var (engine, store, http) = CreateEngine();
        http.Responder = _ => WorkOrdersJson(("wo-1", "approved"), ("wo-2", "approved"));
        await engine.SyncAllAsync();

        http.Responder = _ => WorkOrdersJson(("wo-1", "approved"));
        var result = await engine.SyncAllAsync();

        Assert.Equal(1, result.Removed);
        var cached = await store.GetWorkOrdersAsync();
        Assert.Single(cached);
        Assert.Equal("wo-1", cached[0].Id);
    }

    [Fact]
    public async Task SyncAll_avanca_cursor_e_usa_updatedSince_em_modo_delta()
    {
        var (engine, store, http) = CreateEngine(serverSupportsDelta: true);
        http.Responder = _ => WorkOrdersJson(("wo-1", "approved"));

        // 1.º sync: sem cursor — pull completo, sem updatedSince
        await engine.SyncAllAsync();
        Assert.DoesNotContain("updatedSince", http.Requests[^1].Query);

        var cursor = await store.GetSyncCursorAsync(OfflineEntityTypes.WorkOrder);
        Assert.True(DateTimeOffset.TryParse(cursor, out _));

        // 2.º sync: com cursor — pede só o que mudou e não varre a cache
        http.Responder = _ => WorkOrdersJson(); // delta vazio
        var second = await engine.SyncAllAsync();
        Assert.Contains("updatedSince", http.Requests[^1].Query);
        Assert.Equal(0, second.Removed);
        Assert.Single(await store.GetWorkOrdersAsync());
    }

    [Fact]
    public async Task SyncAll_sem_rede_mantem_cache_e_sinaliza_pull_falhado()
    {
        var (engine, store, http) = CreateEngine();
        http.Responder = _ => WorkOrdersJson(("wo-1", "approved"));
        await engine.SyncAllAsync();

        http.Responder = _ => throw new HttpRequestException("sem rede");
        var result = await engine.SyncAllAsync();

        Assert.False(result.PullOk);
        Assert.Single(await store.GetWorkOrdersAsync());
    }
}

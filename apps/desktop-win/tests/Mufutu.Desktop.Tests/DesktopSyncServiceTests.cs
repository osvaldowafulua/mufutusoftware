using FluentAssertions;
using Mufutu.Desktop.Core.Api;
using Mufutu.Desktop.Core.Api.Models;
using Mufutu.Desktop.Core.Offline;
using Xunit;

namespace Mufutu.Desktop.Tests;

public sealed class FakeApiClient : IMufutuApiClient
{
    public List<WorkOrderDto> WorkOrders { get; set; } = [];
    public List<AssetDto> Assets { get; set; } = [];
    public DashboardSummaryDto Dashboard { get; set; } = new();
    public bool Offline { get; set; }

    public bool IsAuthenticated => true;

    public Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default) =>
        Task.FromResult(new LoginResponse());

    public Task<LoginResponse> RefreshAsync(CancellationToken ct = default) =>
        Task.FromResult(new LoginResponse());

    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default) =>
        Offline ? throw new HttpRequestException("sem rede") : Task.FromResult(Dashboard);

    public Task<PagedResponse<WorkOrderDto>> GetWorkOrdersAsync(int page = 1, int limit = 20, CancellationToken ct = default) =>
        Offline
            ? throw new HttpRequestException("sem rede")
            : Task.FromResult(new PagedResponse<WorkOrderDto> { Data = WorkOrders, Total = WorkOrders.Count });

    public Task<PagedResponse<AssetDto>> GetAssetsAsync(int page = 1, int limit = 20, CancellationToken ct = default) =>
        Offline
            ? throw new HttpRequestException("sem rede")
            : Task.FromResult(new PagedResponse<AssetDto> { Data = Assets, Total = Assets.Count });
}

public class DesktopSyncServiceTests
{
    private static (DesktopSyncService Service, FakeApiClient Api, OfflineStore Store) Create()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        var store = new OfflineStore(
            Path.Combine(Path.GetTempPath(), $"mufutu_desktop_test_{Guid.NewGuid():N}.db"),
            key);
        var api = new FakeApiClient
        {
            WorkOrders =
            [
                new WorkOrderDto { Id = "wo-1", Number = "OT-1", Title = "Filtro", Status = "approved", Priority = "high" },
                new WorkOrderDto { Id = "wo-2", Number = "OT-2", Title = "Óleo", Status = "in_progress", Priority = "medium" },
            ],
            Assets = [new AssetDto { Id = "a-1", GlobalId = "CAT-777-01", Name = "CAT 777", Status = "operational" }],
            Dashboard = new DashboardSummaryDto { TotalAssets = 1, OpenWorkOrders = 2 },
        };
        return (new DesktopSyncService(api, store), api, store);
    }

    [Fact]
    public async Task SyncAll_descarrega_tudo_e_dedupe_no_segundo_ciclo()
    {
        var (service, _, _) = Create();

        var first = await service.SyncAllAsync();
        first.PullOk.Should().BeTrue();
        first.Pulled.Should().Be(4); // 2 OTs + 1 activo + dashboard

        var second = await service.SyncAllAsync();
        second.Pulled.Should().Be(0);
        second.Unchanged.Should().Be(4);
    }

    [Fact]
    public async Task Offline_serve_da_cache_local()
    {
        var (service, api, _) = Create();
        await service.SyncAllAsync();

        api.Offline = true;

        var workOrders = await service.GetWorkOrdersAsync();
        workOrders.Should().HaveCount(2);
        workOrders.Select(w => w.Id).Should().Contain(["wo-1", "wo-2"]);

        var assets = await service.GetAssetsAsync();
        assets.Should().ContainSingle(a => a.Name == "CAT 777");

        var dashboard = await service.GetDashboardSummaryAsync();
        dashboard.Should().NotBeNull();
        dashboard!.OpenWorkOrders.Should().Be(2);
    }

    [Fact]
    public async Task Pull_remove_o_que_desapareceu_do_servidor()
    {
        var (service, api, _) = Create();
        await service.SyncAllAsync();

        api.WorkOrders.RemoveAll(w => w.Id == "wo-2");
        var result = await service.SyncAllAsync();

        result.Removed.Should().Be(1);
        (await service.GetWorkOrdersAsync()).Should().ContainSingle(w => w.Id == "wo-1");
    }

    [Fact]
    public async Task Dados_em_repouso_ficam_encriptados()
    {
        var key = new byte[32];
        Random.Shared.NextBytes(key);
        var dbPath = Path.Combine(Path.GetTempPath(), $"mufutu_enc_test_{Guid.NewGuid():N}.db");
        var store = new OfflineStore(dbPath, key);

        await store.UpsertEntitiesAsync("workOrder", [("wo-1", """{"title":"SEGREDO-VISIVEL"}""")]);

        var raw = await File.ReadAllTextAsync(dbPath);
        raw.Should().NotContain("SEGREDO-VISIVEL");

        var roundTrip = await store.GetEntitiesAsync("workOrder");
        roundTrip.Should().ContainSingle(j => j.Contains("SEGREDO-VISIVEL"));
    }
}

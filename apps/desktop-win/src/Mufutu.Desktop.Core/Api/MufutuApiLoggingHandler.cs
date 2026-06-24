using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Mufutu.Desktop.Core.Api;

/// <summary>Regista pedidos API e erros de rede para diagnóstico em produção.</summary>
public sealed class MufutuApiLoggingHandler : DelegatingHandler
{
    private readonly ILogger<MufutuApiLoggingHandler> _logger;

    public MufutuApiLoggingHandler(ILogger<MufutuApiLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var method = request.Method;
        var uri = request.RequestUri?.ToString() ?? "(null)";

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var snippet = body.Length > 400 ? body[..400] + "…" : body;
                _logger.LogWarning(
                    "API {Method} {Uri} → {StatusCode}: {Body}",
                    method,
                    uri,
                    (int)response.StatusCode,
                    snippet);
            }

            return response;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or SocketException)
        {
            _logger.LogError(ex, "Erro de rede API {Method} {Uri}", method, uri);
            throw;
        }
    }
}

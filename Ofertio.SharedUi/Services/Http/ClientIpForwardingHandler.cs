using Microsoft.AspNetCore.Http;
using Ofertio.Business.Service.Legal;

namespace Ofertio.SharedUi.Services.Http;

/// <summary>
/// Repassa o IP de quem está no navegador para a API.
/// O Blazor Server chama a API a partir do próprio servidor; sem este cabeçalho o aceite gravaria o IP da máquina.
/// </summary>
public sealed class ClientIpForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientIpForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var cloudflare = http.Request.Headers["CF-Connecting-IP"].ToString();
            var forwarded = http.Request.Headers["X-Forwarded-For"].ToString();
            var remote = http.Connection.RemoteIpAddress?.ToString();
            var ip = TermsDocumentRules.ResolveClientIp(cloudflare, forwarded, remote);
            if (!string.IsNullOrWhiteSpace(ip))
            {
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
            }

            if (!string.IsNullOrWhiteSpace(cloudflare))
            {
                request.Headers.TryAddWithoutValidation("CF-Connecting-IP", cloudflare);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
